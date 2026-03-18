using Whycespace.Shared.Primitives.Common;
using Version = Whycespace.Shared.Primitives.Common.Version;

namespace Whycespace.Shared.Protocols.Versioning;

public sealed record VersionConstraint(
    Version MinVersion,
    Version? MaxVersion = null,
    CompatibilityMode Mode = CompatibilityMode.Backward
)
{
    public bool IsSatisfiedBy(Version version)
    {
        if (version.Major < MinVersion.Major) return false;
        if (MaxVersion is { } max && version.Major > max.Major) return false;
        return true;
    }
}
