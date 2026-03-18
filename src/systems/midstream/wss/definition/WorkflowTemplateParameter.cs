namespace Whycespace.Systems.Midstream.WSS.Definition;

public sealed record WorkflowTemplateParameter(
    string ParameterName,
    string ParameterType,
    bool Required
);
