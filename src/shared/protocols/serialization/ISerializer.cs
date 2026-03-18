namespace Whycespace.Shared.Protocols.Serialization;

public interface ISerializer
{
    byte[] Serialize<T>(T value);
    T Deserialize<T>(byte[] data);
    string ContentType { get; }
}
