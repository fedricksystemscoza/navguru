using NavGuru.Data;
using Microsoft.EntityFrameworkCore;

namespace NavGuru.Services;

public class StudentNumberGenerator : IStudentNumberGenerator
{
    private readonly ApplicationDbContext _db;
    private readonly Random _random = new();

    public StudentNumberGenerator(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        // Try up to 20 times to avoid collisions (astronomically unlikely with 8 digits)
        for (var attempt = 0; attempt < 20; attempt++)
        {
            // 8-digit number: first digit is 2-9 (no leading zero), rest 0-9
            // e.g. 20240001, 73451298, etc.
            var firstDigit = _random.Next(2, 10);          // 2-9
            var rest = _random.Next(0, 10_000_000);        // 0000000 - 9999999
            var candidate = $"{firstDigit}{rest:D7}";      // "2" + "0000000" = "20000000"

            var exists = await _db.Users
                .AnyAsync(u => u.StudentNumber == candidate, ct);

            if (!exists) return candidate;
        }

        // Extreme fallback — timestamp-based
        return DateTime.UtcNow.ToString("yyMMddHHmm");
    }
}