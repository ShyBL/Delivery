using UnityEngine;
using UnityEngine.Events;

public class Teleporter : MonoBehaviour
{
    [SerializeField] public UnityEvent OnTrigger;
    [SerializeField] private float deliveryCooldown = 0.5f;

    public float _cooldownTimer;

    private void Update()
    {
        if (_cooldownTimer > 0)
            _cooldownTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Player player))
            TryDeliver(player);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out Player player))
            TryDeliver(player);
    }

    private void TryDeliver(Player player)
    {
        if (_cooldownTimer > 0) return;
        if (player.GetPackagesStoredCount() <= 0) return;

        GameManager.instance.RegisterDelivery();
        OnTrigger.Invoke();
        _cooldownTimer = deliveryCooldown;
    }
}