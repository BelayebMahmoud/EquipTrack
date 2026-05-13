namespace EquipTrack.Domain.Entities;

public enum AssetStatus { Available, InUse, UnderMaintenance, Retired }

public class Asset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public AssetStatus Status { get; set; } = AssetStatus.Available;

    public void Assign()
    {
        if (Status != AssetStatus.Available)
            throw new InvalidOperationException("Asset is not available for assignment.");
        Status = AssetStatus.InUse;
    }

    public void Return()
    {
        if (Status != AssetStatus.InUse)
            throw new InvalidOperationException("Asset is not currently in use and cannot be returned.");
        Status = AssetStatus.Available;
    }
}