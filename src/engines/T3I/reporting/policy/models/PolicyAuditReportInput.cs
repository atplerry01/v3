namespace Whycespace.Engines.T3I.Reporting.Policy.Models;

using Whycespace.Systems.Upstream.WhycePolicy.Models;
using SystemEvidenceRecord = Whycespace.Systems.Upstream.WhycePolicy.Models.PolicyEvidenceRecord;

public sealed record PolicyAuditReportInput(
    PolicyAuditQuery Query,
    IReadOnlyList<PolicyAuditRecord> Records,
    IReadOnlyList<SystemEvidenceRecord>? Evidence);
