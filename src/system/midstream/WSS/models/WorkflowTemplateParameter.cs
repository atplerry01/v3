namespace Whycespace.System.Midstream.WSS.Models;

public sealed record WorkflowTemplateParameter(
    string ParameterName,
    string ParameterType,
    bool Required
);
