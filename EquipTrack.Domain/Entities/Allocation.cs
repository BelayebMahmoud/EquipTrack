namespace EquipTrack.Domain.Entities;

public class Allocation
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? ReturnDate { get; set; }

    // Navigation properties
    public Asset? Asset { get; set; }
    public Employee? Employee { get; set; }
}