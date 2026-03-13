namespace Whycespace.ProjectionRuntime.Storage;

public interface IIdempotentProjectionStore
{
    Task<bool> HasProcessedAsync(Guid eventId);

    Task MarkProcessedAsync(Guid eventId, string aggregateId, long sequenceNumber);

    Task SetAsync(string key, string value);

    Task<string?> GetAsync(string key);

    Task DeleteAsync(string key);
}
