namespace Whycespace.Domain.Core.Spv;

public interface ISpvRegistry
{
    void Register(Guid spvId, string subCluster);
    string? GetSubCluster(Guid spvId);
    IReadOnlyDictionary<Guid, string> GetAll();
}
