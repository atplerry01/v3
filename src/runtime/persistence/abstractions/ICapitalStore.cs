namespace Whycespace.Runtime.Persistence.Abstractions;

public interface ICapitalStore
{
    Task InitializeAsync();
    Task AppendAsync(Guid entryId, Guid capitalId, decimal amount, string currency, DateTimeOffset timestamp);
    Task<IReadOnlyList<object>> GetByCapitalIdAsync(Guid capitalId);
    Task<IReadOnlyList<object>> GetByDateRangeAsync(DateTimeOffset start, DateTimeOffset end);
}
