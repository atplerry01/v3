using Whycespace.Engines.T4A.Access.Contracts.Mappings;
using Whycespace.Engines.T4A.Access.Contracts.Requests;

namespace Whycespace.T4AAccess.Tests;

public sealed class RequestToCommandMapperTests
{
    [Fact]
    public void AllocateCapitalRequest_MapsToCorrectCommand()
    {
        var request = new AllocateCapitalRequest("vault-1", "spv-1", 1000m, "GBP", "investment");
        var (command, payload) = RequestToCommandMapper.Map(request);

        Assert.Equal("capital.allocate", command);
        Assert.Equal("vault-1", payload["vaultId"]);
        Assert.Equal("spv-1", payload["spvId"]);
        Assert.Equal(1000m, payload["amount"]);
        Assert.Equal("GBP", payload["currency"]);
        Assert.Equal("investment", payload["allocationPurpose"]);
    }

    [Fact]
    public void ContributeCapitalRequest_MapsToCorrectCommand()
    {
        var request = new ContributeCapitalRequest("vault-1", "contributor-1", 500m, "USD", null);
        var (command, payload) = RequestToCommandMapper.Map(request);

        Assert.Equal("capital.contribute", command);
        Assert.Equal("vault-1", payload["vaultId"]);
        Assert.Equal("contributor-1", payload["contributorId"]);
        Assert.Equal(500m, payload["amount"]);
        Assert.Equal("USD", payload["currency"]);
        Assert.Equal("", payload["reference"]);
    }

    [Fact]
    public void CreateVaultRequest_MapsToCorrectCommand()
    {
        var request = new CreateVaultRequest("Test Vault", "spv-1", "GBP", "A test vault");
        var (command, payload) = RequestToCommandMapper.Map(request);

        Assert.Equal("vault.create", command);
        Assert.Equal("Test Vault", payload["name"]);
        Assert.Equal("spv-1", payload["spvId"]);
        Assert.Equal("GBP", payload["currency"]);
        Assert.Equal("A test vault", payload["description"]);
    }

    [Fact]
    public void TransferVaultRequest_MapsToCorrectCommand()
    {
        var request = new TransferVaultRequest("source-1", "target-1", 250m, "EUR", "rebalance");
        var (command, payload) = RequestToCommandMapper.Map(request);

        Assert.Equal("vault.transfer", command);
        Assert.Equal("source-1", payload["sourceVaultId"]);
        Assert.Equal("target-1", payload["targetVaultId"]);
        Assert.Equal(250m, payload["amount"]);
        Assert.Equal("EUR", payload["currency"]);
        Assert.Equal("rebalance", payload["reason"]);
    }

    [Fact]
    public void ListPropertyRequest_MapsToCorrectCommand()
    {
        var request = new ListPropertyRequest("prop-1", "123 Main St", 500000m, "GBP", "commercial");
        var (command, payload) = RequestToCommandMapper.Map(request);

        Assert.Equal("property.list", command);
        Assert.Equal("prop-1", payload["propertyId"]);
        Assert.Equal("123 Main St", payload["address"]);
        Assert.Equal(500000m, payload["askingPrice"]);
        Assert.Equal("commercial", payload["propertyType"]);
    }

    [Fact]
    public void RequestRideRequest_MapsToCorrectCommand()
    {
        var request = new RequestRideRequest("passenger-1", 51.5074, -0.1278, 51.5155, -0.1419, null);
        var (command, payload) = RequestToCommandMapper.Map(request);

        Assert.Equal("ride.request", command);
        Assert.Equal("passenger-1", payload["passengerId"]);
        Assert.Equal(51.5074, payload["pickupLatitude"]);
        Assert.Equal("standard", payload["vehicleType"]);
    }

    [Fact]
    public void RegisterIdentityRequest_MapsToCorrectCommand()
    {
        var request = new RegisterIdentityRequest("John Doe", "john@example.com", "individual", null);
        var (command, payload) = RequestToCommandMapper.Map(request);

        Assert.Equal("identity.register", command);
        Assert.Equal("John Doe", payload["displayName"]);
        Assert.Equal("john@example.com", payload["email"]);
        Assert.Equal("individual", payload["identityType"]);
        Assert.Equal("", payload["organizationId"]);
    }

    [Fact]
    public void EvaluatePolicyRequest_MapsContextKeys()
    {
        var context = new Dictionary<string, string> { ["region"] = "UK", ["tier"] = "premium" };
        var request = new EvaluatePolicyRequest("policy-1", "subject-1", "vault", "read", context);
        var (command, payload) = RequestToCommandMapper.Map(request);

        Assert.Equal("policy.evaluate", command);
        Assert.Equal("UK", payload["ctx.region"]);
        Assert.Equal("premium", payload["ctx.tier"]);
    }
}
