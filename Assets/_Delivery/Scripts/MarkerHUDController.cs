using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all edge-of-screen navigational markers for WorldMarker objects.
///
/// Architecture notes (following Tanks conventions):
///  • This is a pure UI controller — it reads game state, never writes it.
///  • One controller per Camera/Canvas pair; assign both in the inspector.
///  • MarkerWidget (a separate prefab) handles per-marker rendering.
///  • The controller owns the widget pool so widgets are never destroyed
///    mid-frame.
///
/// Clustering:
///  LateUpdate runs in three passes:
///    1. Compute raw edge position and camera distance for every marker.
///    2. Group nearby off-screen markers into clusters; assign each a spread
///       slot so icons fan out perpendicular to their shared edge.
///    3. Write final positions to widgets.
/// </summary>
public class MarkerHUDController : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("The gameplay camera used for frustum and viewport tests.")]
    public Camera gameplayCamera;

    [Tooltip("The Canvas that hosts the marker widgets " +
             "(must be Screen Space – Overlay or Screen Space – Camera).")]
    public Canvas hudCanvas;

    [Tooltip("Prefab that renders a single marker. " +
             "Must have a MarkerWidget component at its root.")]
    public MarkerWidget markerWidgetPrefab;

    [Header("Layout")]
    [Tooltip("Inset (px) from each screen edge where edge icons are clamped.")]
    public float edgeOffset = 40f;

    [Tooltip("Size of each edge icon in pixels.")]
    public Vector2 iconSize = new Vector2(48f, 48f);

    [Header("Clustering")]
    [Tooltip("Canvas-space radius (px) within which two edge icons are " +
             "considered overlapping and will be spread into a stack.")]
    public float clusterRadius = 60f;

    [Tooltip("Gap (px) between icon edges inside a spread stack.")]
    public float stackSpacing = 8f;

    // ── Private types ────────────────────────────────────────────────────

    /// <summary>Which screen edge an off-screen marker has been clamped to.</summary>
    private enum EdgeSide { Left, Right, Bottom, Top }

    /// <summary>
    /// All data computed in Pass 1 for a single marker.
    /// Struct keeps the GC cost zero per frame.
    /// </summary>
    private struct MarkerFrameData
    {
        public int      Index;          // parallel index into m_Markers / m_Widgets
        public bool     InFrustum;
        public Vector2  RawEdgePos;     // unmodified clamped canvas position
        public float    Angle;          // arrow rotation in degrees
        public float    CamDistance;    // world distance to camera (for sort order)
        public EdgeSide Edge;
        public Vector2  FinalEdgePos;   // written in Pass 2, read in Pass 3
        public bool     Processed;      // cluster pass has assigned a slot
    }

    // ── Private ──────────────────────────────────────────────────────────

    private readonly List<WorldMarker>     m_Markers   = new();
    private readonly List<MarkerWidget>    m_Widgets   = new();
    private readonly List<MarkerFrameData> m_FrameData = new();  // reused every frame

    private RectTransform m_CanvasRect;

    // ── Unity Lifecycle ──────────────────────────────────────────────────

    private void Awake()
    {
        if (hudCanvas != null)
            m_CanvasRect = hudCanvas.GetComponent<RectTransform>();
    }

    private void Start()
    {
        // Auto-discover all WorldMarkers already in the scene.
        // Markers spawned at runtime should call RegisterMarker() manually.
        foreach (var marker in FindObjectsByType<WorldMarker>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            RegisterMarker(marker);
        }
    }

    private void LateUpdate()
    {
        if (gameplayCamera == null || hudCanvas == null) return;

        Pass1_ComputeRawPositions();
        Pass2_SpreadClusters();
        Pass3_WriteToWidgets();
    }

    // ── Pass 1 — raw positions ────────────────────────────────────────────

    /// <summary>
    /// Tests every marker against the camera frustum, updates discovery state,
    /// and computes the unclustered edge position + metadata.
    /// </summary>
    private void Pass1_ComputeRawPositions()
    {
        m_FrameData.Clear();

        for (int i = 0; i < m_Markers.Count; i++)
        {
            WorldMarker marker = m_Markers[i];

            if (marker == null || !marker.gameObject.activeInHierarchy)
            {
                // Placeholder keeps indices aligned with m_Widgets.
                m_FrameData.Add(new MarkerFrameData { Index = i, InFrustum = true });
                m_Widgets[i].gameObject.SetActive(false);
                continue;
            }

            Vector3 worldPos    = marker.transform.position;
            Vector3 viewportPos = gameplayCamera.WorldToViewportPoint(worldPos);

            bool inFrustum = viewportPos.z > 0f
                             && viewportPos.x is > 0f and < 1f
                             && viewportPos.y is > 0f and < 1f;

            // Inform the marker of its new visibility (may trigger discovery).
            marker.SetVisibility(inFrustum);

            // Icon is stateless per frame — safe to set here.
            m_Widgets[i].gameObject.SetActive(true);
            m_Widgets[i].SetIcon(marker.CurrentIcon);

            if (inFrustum)
            {
                m_FrameData.Add(new MarkerFrameData { Index = i, InFrustum = true });
                continue;
            }

            Vector2 rawEdgePos = ComputeEdgePosition(viewportPos,
                out float angle, out EdgeSide edge);

            float camDist = Vector3.Distance(worldPos, gameplayCamera.transform.position);

            m_FrameData.Add(new MarkerFrameData
            {
                Index        = i,
                InFrustum    = false,
                RawEdgePos   = rawEdgePos,
                FinalEdgePos = rawEdgePos,  // default; overwritten by Pass 2 for clusters
                Angle        = angle,
                CamDistance  = camDist,
                Edge         = edge,
                Processed    = false,
            });
        }
    }

    // ── Pass 2 — cluster spreading ────────────────────────────────────────

    /// <summary>
    /// Groups off-screen markers whose raw edge positions fall within
    /// <see cref="clusterRadius"/> of each other, then fans them out
    /// perpendicular to their shared edge so icons never overlap.
    ///
    /// Spread axis per edge (top-down orthographic game):
    ///   Left / Right  →  Y axis  (icons stack vertically)
    ///   Top  / Bottom →  X axis  (icons stack horizontally)
    ///
    /// Ordering: closest marker at the centre of the stack, alternating outward
    /// (e.g. 4 markers → slots 0, +1, -1, +2).
    /// </summary>
    private void Pass2_SpreadClusters()
    {
        // Step between icon centres = icon footprint + gap.
        float largerSide = Mathf.Max(iconSize.x, iconSize.y);
        float step       = largerSide + stackSpacing;

        for (int i = 0; i < m_FrameData.Count; i++)
        {
            MarkerFrameData root = m_FrameData[i];
            if (root.InFrustum || root.Processed) continue;

            // Gather all markers that belong to this cluster (root included).
            List<int> cluster = new() { i };

            for (int j = i + 1; j < m_FrameData.Count; j++)
            {
                MarkerFrameData candidate = m_FrameData[j];
                if (candidate.InFrustum || candidate.Processed) continue;
                if (candidate.Edge != root.Edge) continue;

                if (Vector2.Distance(root.RawEdgePos, candidate.RawEdgePos) <= clusterRadius)
                    cluster.Add(j);
            }

            // Single marker — no spread needed.
            if (cluster.Count == 1)
            {
                MarkProcessed(i, root.RawEdgePos);
                continue;
            }

            // Sort by camera distance: closest first (will land at centre slot 0).
            cluster.Sort((a, b) =>
                m_FrameData[a].CamDistance.CompareTo(m_FrameData[b].CamDistance));

            // Build signed slot offsets centred on 0.
            int[] slots = BuildSlotOffsets(cluster.Count);

            // Stack pivots around the centroid of all raw positions so the
            // group stays visually anchored to the direction they share.
            Vector2 centroid  = ComputeCentroid(cluster);
            bool    spreadOnY = root.Edge == EdgeSide.Left || root.Edge == EdgeSide.Right;

            for (int k = 0; k < cluster.Count; k++)
            {
                float   offset   = slots[k] * step;
                Vector2 finalPos = centroid + (spreadOnY
                    ? new Vector2(0f, offset)
                    : new Vector2(offset, 0f));

                finalPos = ClampToSafeArea(finalPos);
                MarkProcessed(cluster[k], finalPos);
            }
        }
    }

    // ── Pass 3 — write to widgets ─────────────────────────────────────────

    /// <summary>
    /// Applies resolved final positions from Pass 2 to each MarkerWidget.
    /// All layout decisions have already been made — this pass only renders.
    /// </summary>
    private void Pass3_WriteToWidgets()
    {
        foreach (MarkerFrameData data in m_FrameData)
        {
            MarkerWidget widget = m_Widgets[data.Index];

            if (data.InFrustum)
                widget.SetEdgeMode(false, Vector2.zero, 0f);
            else
                widget.SetEdgeMode(true, data.FinalEdgePos, data.Angle);
        }
    }

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Registers a WorldMarker and creates its HUD widget.
    /// Call this for markers spawned at runtime.
    /// </summary>
    public void RegisterMarker(WorldMarker marker)
    {
        if (m_Markers.Contains(marker)) return;

        m_Markers.Add(marker);

        MarkerWidget widget = Instantiate(markerWidgetPrefab, hudCanvas.transform);
        widget.Initialise(iconSize);
        m_Widgets.Add(widget);
    }

    /// <summary>
    /// Removes a marker and its widget (e.g. when a package is delivered).
    /// </summary>
    public void UnregisterMarker(WorldMarker marker)
    {
        int idx = m_Markers.IndexOf(marker);
        if (idx < 0) return;

        Destroy(m_Widgets[idx].gameObject);
        m_Markers.RemoveAt(idx);
        m_Widgets.RemoveAt(idx);
    }

    // ── Private helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Maps a viewport position (possibly outside [0,1]) to a canvas-space
    /// edge position clamped to the safe inset area. Also returns the edge
    /// the icon landed on and the angle the arrow should point.
    /// </summary>
    private Vector2 ComputeEdgePosition(Vector3 viewportPos,
        out float angle, out EdgeSide edge)
    {
        // Remap viewport [0,1] → centred direction [-0.5, 0.5].
        Vector2 dir = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);

        // Objects behind the camera need their direction flipped.
        if (viewportPos.z < 0f) dir = -dir;

        angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Vector2 canvasSize = m_CanvasRect.sizeDelta;
        float halfW = canvasSize.x * 0.5f - edgeOffset;
        float halfH = canvasSize.y * 0.5f - edgeOffset;

        // The smaller scale tells us which axis hit the edge first.
        float scaleX = (dir.x == 0f) ? float.MaxValue : halfW / Mathf.Abs(dir.x);
        float scaleY = (dir.y == 0f) ? float.MaxValue : halfH / Mathf.Abs(dir.y);
        float scale  = Mathf.Min(scaleX, scaleY);

        // Determine edge: whichever axis was the limiting constraint.
        if (scaleX <= scaleY)
            edge = dir.x > 0f ? EdgeSide.Right : EdgeSide.Left;
        else
            edge = dir.y > 0f ? EdgeSide.Top : EdgeSide.Bottom;

        return dir * scale;
    }

    /// <summary>
    /// Produces signed slot indices with 0 at centre, alternating outward.
    /// count=1 → [0]
    /// count=2 → [0, 1]
    /// count=3 → [0, 1, -1]
    /// count=4 → [0, 1, -1, 2]
    /// </summary>
    private static int[] BuildSlotOffsets(int count)
    {
        int[] slots = new int[count];
        for (int i = 1; i < count; i++)
        {
            // i=1→+1, i=2→-1, i=3→+2, i=4→-2 …
            int half  = (i + 1) / 2;
            slots[i]  = (i % 2 == 1) ? half : -half;
        }
        return slots;
    }

    /// <summary>Geometric centroid of raw edge positions in a cluster.</summary>
    private Vector2 ComputeCentroid(List<int> clusterIndices)
    {
        Vector2 sum = Vector2.zero;
        foreach (int idx in clusterIndices)
            sum += m_FrameData[idx].RawEdgePos;
        return sum / clusterIndices.Count;
    }

    /// <summary>
    /// Clamps a canvas-space position to the safe inset area so spread icons
    /// never slide past the screen corners.
    /// </summary>
    private Vector2 ClampToSafeArea(Vector2 pos)
    {
        Vector2 half  = m_CanvasRect.sizeDelta * 0.5f;
        float   inset = edgeOffset + Mathf.Max(iconSize.x, iconSize.y) * 0.5f;

        return new Vector2(
            Mathf.Clamp(pos.x, -half.x + inset, half.x - inset),
            Mathf.Clamp(pos.y, -half.y + inset, half.y - inset)
        );
    }

    /// <summary>Writes a resolved final position back into the frame data list.</summary>
    private void MarkProcessed(int dataIdx, Vector2 finalPos)
    {
        MarkerFrameData d = m_FrameData[dataIdx];
        d.FinalEdgePos = finalPos;
        d.Processed    = true;
        m_FrameData[dataIdx] = d;
    }
}