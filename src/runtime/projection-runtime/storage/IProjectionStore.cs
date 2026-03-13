namespace Whycespace.ProjectionRuntime.Storage;

public interface IProjectionStore
{
    Task SetAsync(string key, string value);

    Task<string?> GetAsync(string key);

    Task DeleteAsync(string key);
}
