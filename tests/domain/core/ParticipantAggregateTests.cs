namespace Whycespace.Tests.Domain;

using Whycespace.Domain.Core.Participants;
using Whycespace.Domain.Events.Core.Participants;
using Xunit;

public sealed class ParticipantAggregateTests
{
    private static ParticipantProfile CreateValidProfile(ParticipantId? id = null)
    {
        var participantId = id ?? ParticipantId.New();
        return new ParticipantProfile(
            participantId,
            "John",
            "Doe",
            "john.doe@example.com",
            "+44123456789",
            "GB",
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RegisterParticipant_CreatesAggregate_WithDefaultRole()
    {
        var profile = CreateValidProfile();

        var aggregate = ParticipantAggregate.RegisterParticipant(profile);

        Assert.Equal(profile.ParticipantId, aggregate.ParticipantId);
        Assert.Equal(ParticipantStatus.PendingRegistration, aggregate.Status);
        Assert.Single(aggregate.Roles);
        Assert.Contains(ParticipantRole.Participant, aggregate.Roles);
        Assert.Single(aggregate.DomainEvents);
        Assert.IsType<ParticipantRegisteredEvent>(aggregate.DomainEvents[0]);
    }

    [Fact]
    public void RegisterParticipant_WithCustomRole_AssignsRole()
    {
        var profile = CreateValidProfile();

        var aggregate = ParticipantAggregate.RegisterParticipant(profile, ParticipantRole.Investor);

        Assert.Single(aggregate.Roles);
        Assert.Contains(ParticipantRole.Investor, aggregate.Roles);
    }

    [Fact]
    public void RegisterParticipant_WithEmptyId_Throws()
    {
        var profile = CreateValidProfile(ParticipantId.Empty);

        Assert.Throws<InvalidOperationException>(
            () => ParticipantAggregate.RegisterParticipant(profile));
    }

    [Fact]
    public void AssignRole_AddsNewRole()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());

        aggregate.AssignRole(ParticipantRole.Worker);

        Assert.Equal(2, aggregate.Roles.Count);
        Assert.Contains(ParticipantRole.Worker, aggregate.Roles);
    }

    [Fact]
    public void AssignRole_DuplicateRole_Throws()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());

        Assert.Throws<InvalidOperationException>(
            () => aggregate.AssignRole(ParticipantRole.Participant));
    }

    [Fact]
    public void AssignRole_WhenSuspended_Throws()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());
        aggregate.Activate();
        aggregate.Suspend();

        Assert.Throws<InvalidOperationException>(
            () => aggregate.AssignRole(ParticipantRole.Operator));
    }

    [Fact]
    public void AssignRole_RaisesRoleAssignedEvent()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());
        aggregate.ClearDomainEvents();

        aggregate.AssignRole(ParticipantRole.Worker);

        Assert.Single(aggregate.DomainEvents);
        Assert.IsType<ParticipantRoleAssignedEvent>(aggregate.DomainEvents[0]);
    }

    [Fact]
    public void RemoveRole_RemovesExistingRole()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());
        aggregate.AssignRole(ParticipantRole.Worker);

        aggregate.RemoveRole(ParticipantRole.Participant);

        Assert.Single(aggregate.Roles);
        Assert.Contains(ParticipantRole.Worker, aggregate.Roles);
    }

    [Fact]
    public void RemoveRole_LastRole_Throws()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());

        Assert.Throws<InvalidOperationException>(
            () => aggregate.RemoveRole(ParticipantRole.Participant));
    }

    [Fact]
    public void RemoveRole_NonexistentRole_Throws()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());

        Assert.Throws<InvalidOperationException>(
            () => aggregate.RemoveRole(ParticipantRole.Administrator));
    }

    [Fact]
    public void Activate_SetsStatusToActive()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());

        aggregate.Activate();

        Assert.Equal(ParticipantStatus.Active, aggregate.Status);
    }

    [Fact]
    public void Activate_RaisesActivatedEvent()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());
        aggregate.ClearDomainEvents();

        aggregate.Activate();

        Assert.Single(aggregate.DomainEvents);
        Assert.IsType<ParticipantActivatedEvent>(aggregate.DomainEvents[0]);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_Throws()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());
        aggregate.Activate();

        Assert.Throws<InvalidOperationException>(() => aggregate.Activate());
    }

    [Fact]
    public void Suspend_SetsStatusToSuspended()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());
        aggregate.Activate();

        aggregate.Suspend();

        Assert.Equal(ParticipantStatus.Suspended, aggregate.Status);
    }

    [Fact]
    public void Suspend_RaisesSuspendedEvent()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());
        aggregate.Activate();
        aggregate.ClearDomainEvents();

        aggregate.Suspend();

        Assert.Single(aggregate.DomainEvents);
        Assert.IsType<ParticipantSuspendedEvent>(aggregate.DomainEvents[0]);
    }

    [Fact]
    public void Suspend_WhenNotActive_Throws()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());

        Assert.Throws<InvalidOperationException>(() => aggregate.Suspend());
    }

    [Fact]
    public void Deactivate_SetsStatusToDisabled()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());
        aggregate.Activate();

        aggregate.Deactivate();

        Assert.Equal(ParticipantStatus.Disabled, aggregate.Status);
    }

    [Fact]
    public void Deactivate_WhenAlreadyDisabled_Throws()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());
        aggregate.Activate();
        aggregate.Deactivate();

        Assert.Throws<InvalidOperationException>(() => aggregate.Deactivate());
    }

    [Fact]
    public void Profile_IsImmutable()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());

        Assert.Equal("John", aggregate.Profile.FirstName);
        Assert.Equal("Doe", aggregate.Profile.LastName);
        Assert.Equal("john.doe@example.com", aggregate.Profile.Email);
    }

    [Fact]
    public void ParticipantId_IsStronglyTyped()
    {
        var id = ParticipantId.New();
        Guid guid = id;

        Assert.NotEqual(Guid.Empty, guid);
        Assert.Equal(id.Value, guid);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var aggregate = ParticipantAggregate.RegisterParticipant(CreateValidProfile());
        Assert.NotEmpty(aggregate.DomainEvents);

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }
}
