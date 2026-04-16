public class ResourceRequirement
{
    public ResourceType resourceType;
    public int          amount;

    public ResourceRequirement(ResourceType type, int amount)
    {
        resourceType = type;
        this.amount  = amount;
    }

    public override string ToString() => $"{amount}x {resourceType}";
}