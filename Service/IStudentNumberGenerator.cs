namespace NavGuru.Services;

public interface IStudentNumberGenerator
{
    /// <summary>
    /// Generates a unique 8-digit student number, retrying on collision.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken ct = default);
}