// ============================================================
//  ISelectable.cs
//  Place in: Assets/_Delivery/Scripts/
//  Implemented by: ContractSO, LocationSO
// ============================================================

public interface ISelectable
{
    void Select();
    void Deselect();
    bool IsSelected();
}
