namespace Whycespace.Shared.Projections;

public sealed record ProjectionVersion(
    string ProjectionName,
    int Major,
    int Minor,
    bool RequiresRebuild
)
{
    public bool IsCompatibleWith(ProjectionVersion other)
        => ProjectionName == other.ProjectionName && Major == other.Major;
}
