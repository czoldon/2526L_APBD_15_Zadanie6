namespace HospitalApi.Models;

public partial class Room
{
    public string Id { get; set; } = null!;

    public int WardId { get; set; }

    public bool HasTv { get; set; }

    public virtual Ward Ward { get; set; } = null!;

    public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();
}
