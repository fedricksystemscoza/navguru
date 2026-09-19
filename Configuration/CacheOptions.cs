namespace NavGuru.Configuration;

public class CacheOptions
{
    public const string SectionName = "Cache";

    public string Provider { get; set; } = "Memory";
    public string RedisConnectionString { get; set; } = string.Empty;
    public int FaqCacheMinutes { get; set; } = 60;
    public int EventsCacheMinutes { get; set; } = 15;
    public int OfflineSyncMinutes { get; set; } = 30;

    public bool UseRedis =>
        Provider.Equals("Redis", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(RedisConnectionString);
}