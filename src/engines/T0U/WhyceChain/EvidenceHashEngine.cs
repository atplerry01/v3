namespace Whycespace.Engines.T0U.WhyceChain;

using global::System.Security.Cryptography;
using global::System.Text;
using global::System.Text.Json;
using Whycespace.System.Upstream.WhyceChain.Ledger;
using Whycespace.System.Upstream.WhyceChain.Models;

public sealed class EvidenceHashEngine
{
    private const string Algorithm = "SHA256";

    private static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = global::System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public EvidenceHashResult Execute(EvidenceHashCommand command)
    {
        var payloadHash = ComputePayloadHash(command.EvidencePayload);
        var metadataHash = ComputeMetadataHash(command.EvidenceType, command.TraceId, command.CorrelationId, command.Timestamp);
        var evidenceHash = ComputeHash(payloadHash + metadataHash);

        return new EvidenceHashResult(
            EvidenceHash: evidenceHash,
            HashAlgorithm: Algorithm,
            PayloadCanonicalHash: payloadHash,
            MetadataHash: metadataHash,
            GeneratedAt: command.Timestamp,
            TraceId: command.TraceId);
    }

    public EvidenceHash HashPayload(string payload)
    {
        var hash = ComputeHash(payload);
        return new EvidenceHash(hash, Algorithm, DateTimeOffset.UtcNow);
    }

    public EvidenceHash HashObject(object obj)
    {
        var json = Canonicalize(obj);
        return HashPayload(json);
    }

    public bool VerifyHash(string payload, string expectedHash)
    {
        var actual = ComputeHash(payload);
        return actual == expectedHash;
    }

    private static string ComputePayloadHash(object payload)
    {
        var canonical = Canonicalize(payload);
        return ComputeHash(canonical);
    }

    private static string ComputeMetadataHash(string evidenceType, string traceId, string correlationId, DateTime timestamp)
    {
        var metadata = new SortedDictionary<string, object>(StringComparer.Ordinal)
        {
            ["correlationId"] = correlationId,
            ["evidenceType"] = evidenceType,
            ["timestamp"] = timestamp.ToString("O"),
            ["traceId"] = traceId
        };

        var json = JsonSerializer.Serialize(metadata, CanonicalJsonOptions);
        return ComputeHash(json);
    }

    private static string Canonicalize(object obj)
    {
        var json = JsonSerializer.Serialize(obj, CanonicalJsonOptions);
        using var doc = JsonDocument.Parse(json);
        var sorted = SortElement(doc.RootElement);
        return JsonSerializer.Serialize(sorted, CanonicalJsonOptions);
    }

    private static object? SortElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => SortObject(element),
            JsonValueKind.Array => element.EnumerateArray().Select(SortElement).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    private static SortedDictionary<string, object?> SortObject(JsonElement element)
    {
        var sorted = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            sorted[property.Name] = SortElement(property.Value);
        }
        return sorted;
    }

    private static string ComputeHash(string input)
    {
        return Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }
}
