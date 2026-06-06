using System.ComponentModel.DataAnnotations;

namespace HospitalApi.DTOs;

// Odpowiada formatowi z pliku POST.json
public class AssignBedRequestDto
{
    [Required]
    public DateTime From { get; set; }

    // Opcjonalne - puste oznacza przypisanie bezterminowe (Example 2 w POST.json)
    public DateTime? To { get; set; }

    [Required]
    public string BedType { get; set; } = null!;

    [Required]
    public string Ward { get; set; } = null!;
}

// Zwracane po pomyślnym przypisaniu łóżka
public class AssignBedResponseDto
{
    public int BedAssignmentId { get; set; }
    public string PatientPesel { get; set; } = null!;
    public int BedId { get; set; }
    public string RoomId { get; set; } = null!;
    public string BedType { get; set; } = null!;
    public string Ward { get; set; } = null!;
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
}
