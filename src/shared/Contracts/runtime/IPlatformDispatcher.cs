namespace Whycespace.Contracts.Runtime;

public interface IPlatformDispatcher
{
    Task<DispatchResult> DispatchAsync(string command, Dictionary<string, object> payload);
}
