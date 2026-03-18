namespace Whycespace.Engines.T4A.Access.Contracts.Mappings;

using Whycespace.Engines.T4A.Access.Contracts.Requests;

public static class RequestToCommandMapper
{
    public static (string Command, Dictionary<string, object> Payload) Map(AllocateCapitalRequest request)
        => ("capital.allocate", new Dictionary<string, object>
        {
            ["vaultId"] = request.VaultId,
            ["spvId"] = request.SpvId,
            ["amount"] = request.Amount,
            ["currency"] = request.Currency,
            ["allocationPurpose"] = request.AllocationPurpose ?? ""
        });

    public static (string Command, Dictionary<string, object> Payload) Map(ContributeCapitalRequest request)
        => ("capital.contribute", new Dictionary<string, object>
        {
            ["vaultId"] = request.VaultId,
            ["contributorId"] = request.ContributorId,
            ["amount"] = request.Amount,
            ["currency"] = request.Currency,
            ["reference"] = request.Reference ?? ""
        });

    public static (string Command, Dictionary<string, object> Payload) Map(CreateVaultRequest request)
        => ("vault.create", new Dictionary<string, object>
        {
            ["name"] = request.Name,
            ["spvId"] = request.SpvId,
            ["currency"] = request.Currency,
            ["description"] = request.Description ?? ""
        });

    public static (string Command, Dictionary<string, object> Payload) Map(TransferVaultRequest request)
        => ("vault.transfer", new Dictionary<string, object>
        {
            ["sourceVaultId"] = request.SourceVaultId,
            ["targetVaultId"] = request.TargetVaultId,
            ["amount"] = request.Amount,
            ["currency"] = request.Currency,
            ["reason"] = request.Reason ?? ""
        });

    public static (string Command, Dictionary<string, object> Payload) Map(ListPropertyRequest request)
        => ("property.list", new Dictionary<string, object>
        {
            ["propertyId"] = request.PropertyId,
            ["address"] = request.Address,
            ["askingPrice"] = request.AskingPrice,
            ["currency"] = request.Currency,
            ["propertyType"] = request.PropertyType ?? "residential"
        });

    public static (string Command, Dictionary<string, object> Payload) Map(RequestRideRequest request)
        => ("ride.request", new Dictionary<string, object>
        {
            ["passengerId"] = request.PassengerId,
            ["pickupLatitude"] = request.PickupLatitude,
            ["pickupLongitude"] = request.PickupLongitude,
            ["dropoffLatitude"] = request.DropoffLatitude,
            ["dropoffLongitude"] = request.DropoffLongitude,
            ["vehicleType"] = request.VehicleType ?? "standard"
        });

    public static (string Command, Dictionary<string, object> Payload) Map(RegisterIdentityRequest request)
        => ("identity.register", new Dictionary<string, object>
        {
            ["displayName"] = request.DisplayName,
            ["email"] = request.Email,
            ["identityType"] = request.IdentityType,
            ["organizationId"] = request.OrganizationId ?? ""
        });

    public static (string Command, Dictionary<string, object> Payload) Map(EvaluatePolicyRequest request)
    {
        var payload = new Dictionary<string, object>
        {
            ["policyId"] = request.PolicyId,
            ["subjectId"] = request.SubjectId,
            ["resource"] = request.Resource,
            ["action"] = request.Action
        };

        if (request.Context is not null)
        {
            foreach (var kvp in request.Context)
                payload[$"ctx.{kvp.Key}"] = kvp.Value;
        }

        return ("policy.evaluate", payload);
    }
}
