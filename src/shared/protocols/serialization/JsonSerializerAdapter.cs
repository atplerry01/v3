using System.Text.Json;

namespace Whycespace.Shared.Protocols.Serialization;

public sealed class JsonSerializerAdapter : ISerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonSerializerAdapter(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public string ContentType => "application/json";

    public byte[] Serialize<T>(T value)
        => JsonSerializer.SerializeToUtf8Bytes(value, _options);

    public T Deserialize<T>(byte[] data)
        => JsonSerializer.Deserialize<T>(data, _options)
           ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
}
