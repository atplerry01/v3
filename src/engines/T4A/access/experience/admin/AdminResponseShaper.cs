namespace Whycespace.Engines.T4A.Access.Experience.Admin;

public sealed class AdminResponseShaper : IResponseShaper<IReadOnlyDictionary<string, object>>
{
    public string ClientType => "admin";

    public object Shape(IReadOnlyDictionary<string, object> data)
    {
        // Admin sees full data including internal metadata
        return new Dictionary<string, object>(data)
        {
            ["_view"] = "admin",
            ["_timestamp"] = DateTimeOffset.UtcNow
        };
    }
}
