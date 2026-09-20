using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NavGuru.Configuration;

namespace NavGuru.Services;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning("Email not configured — skipping send to {Email}", toEmail);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = 30_000;   // 30 seconds — fail fast

            // Choose the right security mode based on config
            var secureOptions = _options.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect    // implicit SSL (port 465)
                : _options.UseStartTls
                    ? SecureSocketOptions.StartTls    // STARTTLS (port 587)
                    : SecureSocketOptions.Auto;

            _logger.LogInformation(
                "Connecting to {Host}:{Port} using {Mode}",
                _options.SmtpHost, _options.SmtpPort, secureOptions);

            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, secureOptions, ct);
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Email send cancelled for {Email}", toEmail);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex,
                "Email send timed out for {Email} — likely SMTP port {Port} blocked on this host. Try port 465 with SSL.",
                toEmail, _options.SmtpPort);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(ex,
                "Email authentication failed for {Email} — check Email:Username and Email:Password.",
                toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }

    public async Task SendToManyAsync(IEnumerable<(string Email, string Name)> recipients, string subject, string htmlBody, CancellationToken ct = default)
    {
        foreach (var (email, name) in recipients)
        {
            await SendAsync(email, name, subject, htmlBody, ct);
            await Task.Delay(100, ct);
        }
    }

    public async Task SendBulkAsync(IEnumerable<(string Email, string Name)> recipients, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!_options.IsConfigured) return;

        using var client = new SmtpClient();
        client.Timeout = 30_000;

        var secureOptions = _options.SmtpPort == 465
            ? SecureSocketOptions.SslOnConnect
            : _options.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

        await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, secureOptions, ct);
        await client.AuthenticateAsync(_options.Username, _options.Password, ct);

        foreach (var (email, name) in recipients)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            message.To.Add(new MailboxAddress(name, email));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            await client.SendAsync(message, ct);
            await Task.Delay(100, ct);
        }

        await client.DisconnectAsync(true, ct);
    }
}