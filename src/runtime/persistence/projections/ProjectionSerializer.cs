namespace Whycespace.Runtime.Persistence.Projections;

using System.Text.Json;

public static class ProjectionSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize<T>(T state) => JsonSerializer.Serialize(state, Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
}
