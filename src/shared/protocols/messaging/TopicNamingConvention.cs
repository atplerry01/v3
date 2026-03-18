namespace Whycespace.Shared.Protocols.Messaging;

public static class TopicNamingConvention
{
    public const string Prefix = "whyce";
    public const string Separator = ".";

    public static string ForDomain(string domain, string category)
        => $"{Prefix}{Separator}{domain}{Separator}{category}";

    public static string ForCluster(string clusterId, string category)
        => $"{Prefix}{Separator}cluster{Separator}{clusterId}{Separator}{category}";
}
