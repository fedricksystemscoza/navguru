namespace NavGuru.Configuration;

public class MapsOptions
{
    public const string SectionName = "Maps";

    public string Provider { get; set; } = "AzureMaps";
    public string ApiKey { get; set; } = string.Empty;
    public MapCenter DefaultCenter { get; set; } = new();
    public int DefaultZoom { get; set; } = 15;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}

public class MapCenter
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}