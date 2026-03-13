using Whycespace.EventSchema.Models;

namespace Whycespace.EventSchema.Versioning;

public sealed class EventVersionManager
{
    public bool ValidateCompatibility(Models.EventSchema previous, Models.EventSchema next)
    {
        foreach (var field in previous.PayloadStructure)
        {
            if (!next.PayloadStructure.TryGetValue(field.Key, out var nextType))
                return false;

            if (nextType != field.Value)
                return false;
        }

        return true;
    }

    public int IncrementVersion(Models.EventSchema current) => current.SchemaVersion + 1;
}
