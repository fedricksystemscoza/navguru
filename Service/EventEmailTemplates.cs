using NavGuru.Models;

namespace NavGuru.Services;

public static class EventEmailTemplates
{
    public static string NewEventHtml(OrientationEvent ev, string recipientName)
    {
        var firstName = string.IsNullOrWhiteSpace(recipientName)
            ? "there"
            : recipientName.Split(' ', StringSplitOptions.RemoveEmptyEntries).First();

        var mandatoryBadge = ev.IsMandatory
            ? "<span style=\"display:inline-block;background:#0A1F44;color:#FFC72C;font-size:10px;font-weight:800;padding:3px 8px;border-radius:4px;letter-spacing:0.05em;text-transform:uppercase;\">Mandatory</span>"
            : "<span style=\"display:inline-block;background:#e2e8f0;color:#475569;font-size:10px;font-weight:800;padding:3px 8px;border-radius:4px;letter-spacing:0.05em;text-transform:uppercase;\">Optional</span>";

        var dateStr = ev.StartTime.ToString("dddd, dd MMMM yyyy");
        var timeStr = $"{ev.StartTime:HH:mm} – {ev.EndTime:HH:mm}";

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
</head>
<body style=""margin:0;padding:0;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:#f1f5f9;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f1f5f9;padding:32px 16px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(10,31,68,.08);"">

                    <!-- Header -->
                    <tr>
                        <td style=""background:#0A1F44;padding:32px;text-align:center;"">
                            <div style=""display:inline-block;background:#FFC72C;color:#0A1F44;width:48px;height:48px;border-radius:12px;font-size:26px;line-height:48px;text-align:center;"">★</div>
                            <h1 style=""color:#ffffff;margin:16px 0 0;font-size:22px;font-weight:700;"">NavGuru</h1>
                            <p style=""color:rgba(255,255,255,.7);margin:4px 0 0;font-size:13px;"">New Student Orientation &amp; Support</p>
                        </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                        <td style=""padding:32px;"">
                            <p style=""color:#0A1F44;font-size:16px;margin:0 0 8px;"">Hi {firstName},</p>
                            <p style=""color:#475569;font-size:14px;line-height:1.6;margin:0 0 24px;"">
                                A new orientation event has just been added to the NavGuru calendar. Here are the details:
                            </p>

                            <!-- Event card -->
                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f8fafc;border-left:4px solid #FFC72C;border-radius:8px;padding:0;"">
                                <tr>
                                    <td style=""padding:20px;"">
                                        <div style=""margin-bottom:12px;"">{mandatoryBadge}</div>
                                        <h2 style=""color:#0A1F44;font-size:18px;margin:0 0 12px;line-height:1.3;"">{ev.Title}</h2>
                                        <p style=""color:#475569;font-size:14px;line-height:1.6;margin:0 0 16px;"">{ev.Description}</p>

                                        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                                            <tr>
                                                <td style=""padding:4px 0;color:#0A1F44;font-size:13px;"">
                                                    <strong style=""color:#0A1F44;"">📅</strong>&nbsp;&nbsp;{dateStr}
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style=""padding:4px 0;color:#0A1F44;font-size:13px;"">
                                                    <strong style=""color:#0A1F44;"">🕐</strong>&nbsp;&nbsp;{timeStr}
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style=""padding:4px 0;color:#0A1F44;font-size:13px;"">
                                                    <strong style=""color:#0A1F44;"">📍</strong>&nbsp;&nbsp;{ev.Location}
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <p style=""color:#475569;font-size:13px;line-height:1.6;margin:24px 0 0;"">
                                Log in to NavGuru to view this event on your dashboard, add it to your calendar, or check in via QR code.
                            </p>

                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin-top:24px;"">
                                <tr>
                                    <td style=""background:#FFC72C;border-radius:10px;padding:0;"">
                                        <a href=""https://navguru.example.com/Calendar"" style=""display:inline-block;color:#0A1F44;font-weight:700;font-size:14px;padding:12px 24px;text-decoration:none;"">
                                            View in NavGuru →
                                        </a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""background:#f8fafc;padding:20px 32px;border-top:1px solid #e2e8f0;text-align:center;"">
                            <p style=""color:#94a3b8;font-size:11px;margin:0;line-height:1.6;"">
                                You're receiving this because you're enrolled at Nelson Mandela University.<br>
                                NavGuru · Student Orientation Portal
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    public static string NewEventPlainText(OrientationEvent ev, string recipientName)
    {
        var firstName = string.IsNullOrWhiteSpace(recipientName)
            ? "there"
            : recipientName.Split(' ', StringSplitOptions.RemoveEmptyEntries).First();

        return $@"Hi {firstName},

A new orientation event has just been added to the NavGuru calendar:

{ev.Title}
{ev.Description}

Date: {ev.StartTime:dddd, dd MMMM yyyy}
Time: {ev.StartTime:HH:mm} – {ev.EndTime:HH:mm}
Location: {ev.Location}
{(ev.IsMandatory ? "MANDATORY — attendance required" : "Optional event")}

Log in to NavGuru to view this on your dashboard.

— NavGuru
Nelson Mandela University";
    }
}