namespace NavGuru.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default);
    Task SendToManyAsync(IEnumerable<(string Email, string Name)> recipients, string subject, string htmlBody, CancellationToken ct = default);
}