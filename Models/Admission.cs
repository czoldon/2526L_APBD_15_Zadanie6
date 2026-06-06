namespace HospitalApi.Models;

public partial class Admission
{
    public int Id { get; set; }

    public DateTime AdmissionDate { get; set; }

    public DateTime? DischargeDate { get; set; }

    public string PatientPesel { get; set; } = null!;

    public int WardId { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Ward Ward { get; set; } = null!;
}
