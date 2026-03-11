using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityCoreModelTests
{
    [Fact]
    public void IdentityId_New_ShouldGenerateUniqueId()
    {
        var id1 = IdentityId.New();
        var id2 = IdentityId.New();

        Assert.NotEqual(id1.Value, id2.Value);
        Assert.NotEqual(Guid.Empty, id1.Value);
        Assert.NotEqual(Guid.Empty, id2.Value);
    }

    [Fact]
    public void IdentityId_From_ShouldRejectEmptyGuid()
    {
        Assert.Throws<ArgumentException>(() => IdentityId.From(Guid.Empty));
    }

    [Fact]
    public void IdentityId_From_ShouldAcceptValidGuid()
    {
        var guid = Guid.NewGuid();
        var id = IdentityId.From(guid);

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void IdentityAggregate_ShouldStartAsPending()
    {
        var identity = new IdentityAggregate(IdentityId.New(), IdentityType.User);

        Assert.Equal(IdentityStatus.Pending, identity.Status);
    }

    [Fact]
    public void Verify_ShouldChangeStatusToVerified()
    {
        var identity = new IdentityAggregate(IdentityId.New(), IdentityType.User);

        identity.Verify();

        Assert.Equal(IdentityStatus.Verified, identity.Status);
        Assert.NotNull(identity.VerifiedAt);
    }

    [Fact]
    public void Verify_OnlyAllowedFromPending()
    {
        var identity = new IdentityAggregate(IdentityId.New(), IdentityType.User);
        identity.Verify();

        Assert.Throws<InvalidOperationException>(() => identity.Verify());
    }

    [Fact]
    public void Suspend_ShouldTransitionCorrectly()
    {
        var identity = new IdentityAggregate(IdentityId.New(), IdentityType.Service);

        identity.Suspend();

        Assert.Equal(IdentityStatus.Suspended, identity.Status);
    }

    [Fact]
    public void Revoked_Identity_CannotBeSuspended()
    {
        var identity = new IdentityAggregate(IdentityId.New(), IdentityType.User);
        identity.Revoke();

        Assert.Throws<InvalidOperationException>(() => identity.Suspend());
    }

    [Fact]
    public void CreatedAt_ShouldBeSetOnConstruction()
    {
        var before = DateTime.UtcNow;
        var identity = new IdentityAggregate(IdentityId.New(), IdentityType.Operator);
        var after = DateTime.UtcNow;

        Assert.InRange(identity.CreatedAt, before, after);
    }

    [Fact]
    public void IdentityAggregate_NullId_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new IdentityAggregate(null!, IdentityType.User));
    }

    [Fact]
    public void Revoke_ShouldSetStatusToRevoked()
    {
        var identity = new IdentityAggregate(IdentityId.New(), IdentityType.Guardian);

        identity.Revoke();

        Assert.Equal(IdentityStatus.Revoked, identity.Status);
    }

    [Fact]
    public void IdentityId_ValueEquality()
    {
        var guid = Guid.NewGuid();
        var id1 = IdentityId.From(guid);
        var id2 = IdentityId.From(guid);

        Assert.Equal(id1, id2);
    }
}
