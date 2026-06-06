namespace HospitalApi.Models;

public partial class Bed
{
    public int Id { get; set; }

    public string RoomId { get; set; } = null!;

    public int BedTypeId { get; set; }

    public virtual BedType BedType { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;

    public virtual ICollection<BedAssignment> BedAssignments { get; set; } = new List<BedAssignment>();
}
