using HospitalApi.DTOs;
using HospitalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalApi.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly HospitalContext _context;

    public PatientsController(HospitalContext context)
    {
        _context = context;
    }

    // =====================================================================
    // ZAD 2:  GET /api/patients?search=...
    // Zwraca wszystkich pacjentów; opcjonalny parametr "search" filtruje
    // po FirstName ORAZ LastName z użyciem LIKE %...% (EF.Functions.Like).
    // =====================================================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientGetDto>>> GetPatients([FromQuery] string? search)
    {
        var query = _context.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(p =>
                EF.Functions.Like(p.FirstName, pattern) ||
                EF.Functions.Like(p.LastName, pattern));
        }

        var patients = await query
            .Select(p => new PatientGetDto
            {
                Pesel = p.Pesel,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Age = p.Age,
                Sex = p.Sex ? "Male" : "Female",
                Admissions = p.Admissions.Select(a => new AdmissionDto
                {
                    Id = a.Id,
                    AdmissionDate = a.AdmissionDate,
                    DischargeDate = a.DischargeDate,
                    Ward = new WardDto
                    {
                        Id = a.Ward.Id,
                        Name = a.Ward.Name,
                        Description = a.Ward.Description
                    }
                }).ToList(),
                BedAssignments = p.BedAssignments.Select(ba => new BedAssignmentDto
                {
                    Id = ba.Id,
                    From = ba.From,
                    To = ba.To,
                    Bed = new BedDto
                    {
                        Id = ba.Bed.Id,
                        BedType = new BedTypeDto
                        {
                            Id = ba.Bed.BedType.Id,
                            Name = ba.Bed.BedType.Name,
                            Description = ba.Bed.BedType.Description
                        },
                        Room = new RoomDto
                        {
                            Id = ba.Bed.Room.Id,
                            HasTv = ba.Bed.Room.HasTv,
                            Ward = new WardDto
                            {
                                Id = ba.Bed.Room.Ward.Id,
                                Name = ba.Bed.Room.Ward.Name,
                                Description = ba.Bed.Room.Ward.Description
                            }
                        }
                    }
                }).ToList()
            })
            .ToListAsync();

        return Ok(patients);
    }

    // =====================================================================
    // ZAD 3:  POST /api/patients/{pesel}/bedassignments
    // Znajduje wolne łóżko danego typu, w danym oddziale, niezajęte
    // we wskazanym okresie i przypisuje je pacjentowi.
    // Każdy przypadek braku zasobu zwraca osobny, czytelny komunikat 404.
    // =====================================================================
    [HttpPost("{pesel}/bedassignments")]
    public async Task<ActionResult<AssignBedResponseDto>> AssignBed(
        string pesel,
        [FromBody] AssignBedRequestDto request)
    {
        // Walidacja zakresu czasu
        if (request.To.HasValue && request.To.Value <= request.From)
        {
            return BadRequest("Data 'to' musi być późniejsza niż data 'from'.");
        }

        // 1. Pacjent musi istnieć
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Pesel == pesel);
        if (patient is null)
        {
            return NotFound($"Nie znaleziono pacjenta o numerze PESEL '{pesel}'.");
        }

        // 2. Oddział musi istnieć
        var ward = await _context.Wards
            .FirstOrDefaultAsync(w => w.Name == request.Ward);
        if (ward is null)
        {
            return NotFound($"Nie znaleziono oddziału o nazwie '{request.Ward}'.");
        }

        // 3. Typ łóżka musi istnieć
        var bedType = await _context.BedTypes
            .FirstOrDefaultAsync(bt => bt.Name == request.BedType);
        if (bedType is null)
        {
            return NotFound($"Nie znaleziono typu łóżka o nazwie '{request.BedType}'.");
        }

        // 4. Czy w tym oddziale w ogóle istnieją łóżka tego typu?
        var matchingBedsExist = await _context.Beds.AnyAsync(b =>
            b.BedTypeId == bedType.Id &&
            b.Room.WardId == ward.Id);
        if (!matchingBedsExist)
        {
            return NotFound(
                $"W oddziale '{request.Ward}' nie ma żadnego łóżka typu '{request.BedType}'.");
        }

        // 5. Spośród pasujących łóżek wybierz pierwsze WOLNE w zadanym okresie.
        //    Łóżko jest zajęte, gdy istnieje przypisanie nakładające się na okres:
        //      istniejące [From, To)  nakłada się na  żądane [from, to)
        //      gdy:  (To == null || To > from)  AND  (to == null || From < to)
        DateTime from = request.From;
        DateTime? to = request.To;

        var freeBed = await _context.Beds
            .Where(b =>
                b.BedTypeId == bedType.Id &&
                b.Room.WardId == ward.Id &&
                !b.BedAssignments.Any(ba =>
                    (ba.To == null || ba.To > from) &&
                    (to == null || ba.From < to)))
            .OrderBy(b => b.Id)
            .FirstOrDefaultAsync();

        if (freeBed is null)
        {
            return NotFound(
                $"W oddziale '{request.Ward}' wszystkie łóżka typu '{request.BedType}' " +
                $"są zajęte we wskazanym okresie.");
        }

        // 6. Utwórz przypisanie
        var assignment = new BedAssignment
        {
            PatientPesel = pesel,
            BedId = freeBed.Id,
            From = from,
            To = to
        };

        _context.BedAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        var response = new AssignBedResponseDto
        {
            BedAssignmentId = assignment.Id,
            PatientPesel = pesel,
            BedId = freeBed.Id,
            RoomId = freeBed.RoomId,
            BedType = bedType.Name,
            Ward = ward.Name,
            From = from,
            To = to
        };

        return CreatedAtAction(nameof(GetPatients), new { search = pesel }, response);
    }
}
