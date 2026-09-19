namespace NavGuru.Configuration;

public class QrOptions
{
    public const string SectionName = "Qr";
    public string SigningSecret { get; set; } = string.Empty;
}