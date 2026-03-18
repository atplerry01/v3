using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T4A.Access.Contracts.Mappings;

namespace Whycespace.T4AAccess.Tests;

public sealed class ResultToResponseMapperTests
{
    [Fact]
    public void ToCapitalAllocation_MapsFromDispatchResult()
    {
        var result = DispatchResult.Ok(new Dictionary<string, object>
        {
            ["allocationId"] = "alloc-1",
            ["vaultId"] = "vault-1",
            ["spvId"] = "spv-1",
            ["amount"] = 1000m,
            ["currency"] = "GBP",
            ["status"] = "accepted"
        });

        var response = ResultToResponseMapper.ToCapitalAllocation(result);

        Assert.Equal("alloc-1", response.AllocationId);
        Assert.Equal("vault-1", response.VaultId);
        Assert.Equal("spv-1", response.SpvId);
        Assert.Equal(1000m, response.Amount);
        Assert.Equal("GBP", response.Currency);
        Assert.Equal("accepted", response.Status);
    }

    [Fact]
    public void ToVault_MapsFromDispatchResult()
    {
        var result = DispatchResult.Ok(new Dictionary<string, object>
        {
            ["vaultId"] = "vault-1",
            ["name"] = "Test Vault",
            ["spvId"] = "spv-1",
            ["currency"] = "GBP"
        });

        var response = ResultToResponseMapper.ToVault(result);

        Assert.Equal("vault-1", response.VaultId);
        Assert.Equal("Test Vault", response.Name);
        Assert.Equal("created", response.Status); // default
    }

    [Fact]
    public void ToRide_HandlesNullOptionalFields()
    {
        var result = DispatchResult.Ok(new Dictionary<string, object>
        {
            ["rideId"] = "ride-1",
            ["passengerId"] = "passenger-1",
            ["status"] = "requested"
        });

        var response = ResultToResponseMapper.ToRide(result);

        Assert.Equal("ride-1", response.RideId);
        Assert.Null(response.DriverId);
        Assert.Null(response.EstimatedArrival);
    }

    [Fact]
    public void ToPolicyEvaluation_ExtractsObligations()
    {
        var result = DispatchResult.Ok(new Dictionary<string, object>
        {
            ["policyId"] = "policy-1",
            ["decision"] = "allow",
            ["reason"] = "within scope",
            ["obligation.audit"] = (object)"required",
            ["obligation.notify"] = (object)"admin"
        });

        var response = ResultToResponseMapper.ToPolicyEvaluation(result);

        Assert.Equal("allow", response.Decision);
        Assert.NotNull(response.Obligations);
        Assert.Equal(2, response.Obligations!.Count);
        Assert.Equal("required", response.Obligations["audit"]);
    }
}
