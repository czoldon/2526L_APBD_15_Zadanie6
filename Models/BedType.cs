namespace HospitalApi.Models;

public partial class BedType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();
}
