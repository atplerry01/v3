namespace Whycespace.ProjectionRuntime.Registry;

/// <summary>
/// Marks a class as a projection processor for auto-discovery.
/// The class must implement IProjectionProcessor&lt;TEvent&gt;.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ProjectionProcessorAttribute : Attribute;
