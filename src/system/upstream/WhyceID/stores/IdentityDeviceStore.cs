namespace Whycespace.System.WhyceID.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.WhyceID.Models;

public sealed class IdentityDeviceStore
{
    private readonly ConcurrentDictionary<Guid, List<IdentityDevice>> _devices = new();

    public void Register(Guid identityId, IdentityDevice device)
    {
        var list = _devices.GetOrAdd(identityId, _ => new List<IdentityDevice>());

        lock (list)
        {
            list.Add(device);
        }
    }

    public IReadOnlyCollection<IdentityDevice> Get(Guid identityId)
    {
        if (_devices.TryGetValue(identityId, out var list))
        {
            lock (list)
            {
                return list.ToList();
            }
        }

        return Array.Empty<IdentityDevice>();
    }

    public void TrustDevice(Guid identityId, Guid deviceId)
    {
        if (_devices.TryGetValue(identityId, out var list))
        {
            lock (list)
            {
                var device = list.FirstOrDefault(d => d.DeviceId == deviceId);

                if (device is not null)
                {
                    list.Remove(device);
                    list.Add(device with { Trusted = true });
                }
            }
        }
    }

    public bool IsTrusted(Guid identityId, Guid deviceId)
    {
        if (_devices.TryGetValue(identityId, out var list))
        {
            lock (list)
            {
                return list.Any(d => d.DeviceId == deviceId && d.Trusted);
            }
        }

        return false;
    }
}
