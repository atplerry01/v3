namespace Whycespace.Runtime.Persistence.Contracts;

public interface IProjectionStore
{
    Task InitializeAsync();
    Task UpsertAsync(string projectionName, string key, object state);
    Task<string?> GetAsync(string projectionName, string key);
    Task<IReadOnlyList<(string Key, string State)>> GetAllAsync(string projectionName);
}
