using Whycespace.EventFabric.Schema;

namespace Whycespace.EventFabric.Validation;

public sealed class CompatibilityValidator
{
    public bool ValidateUpgrade(EventSchemaDefinition previous, EventSchemaDefinition next)
    {
        if (next.Version <= previous.Version)
            return false;

        foreach (var field in previous.Fields)
        {
            if (!next.Fields.TryGetValue(field.Key, out var nextType))
                return false;

            if (nextType != field.Value)
                return false;
        }

        return true;
    }
}
