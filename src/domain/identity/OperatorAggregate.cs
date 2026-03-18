namespace Whycespace.Domain.Identity;

public sealed class OperatorAggregate
{
    private readonly List<string> _authorizedScopes = new();
    private readonly List<object> _domainEvents = new();

    public OperatorId OperatorId { get; }
    public string Name { get; }
    public OperatorStatus Status { get; private set; }
    public IReadOnlyList<string> AuthorizedScopes => _authorizedScopes.AsReadOnly();
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private OperatorAggregate(
        OperatorId operatorId,
        string name,
        IEnumerable<string> authorizedScopes)
    {
        OperatorId = operatorId;
        Name = name;
        Status = OperatorStatus.Active;
        _authorizedScopes.AddRange(authorizedScopes);
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static OperatorAggregate Register(
        OperatorId operatorId,
        string name,
        IEnumerable<string> authorizedScopes)
    {
        if (operatorId == OperatorId.Empty)
            throw new InvalidOperationException("OperatorId must exist.");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Operator name is required.");

        return new OperatorAggregate(operatorId, name, authorizedScopes);
    }

    public bool HasAuthorityForScope(string scope)
        => _authorizedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase);

    public bool IsActive() => Status == OperatorStatus.Active;

    public void GrantScope(string scope)
    {
        if (_authorizedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Operator already has scope '{scope}'.");

        _authorizedScopes.Add(scope);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RevokeScope(string scope)
    {
        if (!_authorizedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Operator does not have scope '{scope}'.");

        _authorizedScopes.Remove(scope);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Suspend()
    {
        if (Status != OperatorStatus.Active)
            throw new InvalidOperationException("Only active operators can be suspended.");

        Status = OperatorStatus.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (Status == OperatorStatus.Active)
            throw new InvalidOperationException("Operator is already active.");

        Status = OperatorStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
