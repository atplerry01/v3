
using System.Text.Json;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.EventFabric.Schema;

namespace Whycespace.EventFabric.Validation;

public sealed class SchemaValidator
{
    private readonly EventSchemaRegistry _schemaRegistry;

    public SchemaValidator(EventSchemaRegistry schemaRegistry)
    {
        _schemaRegistry = schemaRegistry;
    }

    public bool Validate(EventEnvelope envelope)
    {
        var schema = _schemaRegistry.GetLatest(envelope.EventType);

        if (schema is null)
            return false;

        var payloadFields = ExtractFields(envelope.Payload);

        foreach (var field in schema.Fields)
        {
            if (!payloadFields.ContainsKey(field.Key))
                return false;
        }

        return true;
    }

    private static Dictionary<string, string> ExtractFields(object payload)
    {
        if (payload is JsonElement jsonElement)
            return ExtractFromJsonElement(jsonElement);

        var properties = payload.GetType().GetProperties();
        var fields = new Dictionary<string, string>();

        foreach (var prop in properties)
        {
            fields[prop.Name] = prop.PropertyType.Name;
        }

        return fields;
    }

    private static Dictionary<string, string> ExtractFromJsonElement(JsonElement element)
    {
        var fields = new Dictionary<string, string>();

        if (element.ValueKind != JsonValueKind.Object)
            return fields;

        foreach (var property in element.EnumerateObject())
        {
            fields[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => "String",
                JsonValueKind.Number => "Number",
                JsonValueKind.True or JsonValueKind.False => "Boolean",
                _ => "Object"
            };
        }

        return fields;
    }
}
