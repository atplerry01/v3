namespace Whycespace.Engines.T4A.Access.Experience;

public interface IResponseShaper<T>
{
    string ClientType { get; }
    object Shape(T data);
}
