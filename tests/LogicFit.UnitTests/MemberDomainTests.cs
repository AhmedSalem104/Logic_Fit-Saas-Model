using LogicFit.Domain.Members;

namespace LogicFit.UnitTests;

public sealed class MemberDomainTests
{
    [Fact]
    public void ArchiveChangesStatusWithoutChangingIdentityOrGym()
    {
        var member = NewMember(MemberStatuses.Active);

        var archived = member.Archive();

        Assert.Equal(MemberStatuses.Archived, archived.Status);
        Assert.Equal(member.MemberId, archived.MemberId);
        Assert.Equal(member.GymId, archived.GymId);
        Assert.Equal(member.MemberCode, archived.MemberCode);
    }

    [Fact]
    public void ArchiveIsIdempotent()
    {
        var member = NewMember(MemberStatuses.Archived);

        Assert.Equal(member, member.Archive());
    }

    [Fact]
    public void ArchivedMemberCannotBeNormallyUpdated()
    {
        var member = NewMember(MemberStatuses.Archived);

        Assert.Throws<InvalidOperationException>(() => member.Update(
            "Updated",
            "+201000000000",
            null,
            new DateOnly(2026, 8, 30),
            null,
            MemberStatuses.Active));
    }

    [Fact]
    public void NormalUpdateCannotReopenAnArchivedMemberOrUseAnUnknownStatus()
    {
        var active = NewMember(MemberStatuses.Active);

        Assert.Throws<ArgumentException>(() => active.Update(
            "Updated",
            "+201000000000",
            null,
            new DateOnly(2026, 8, 30),
            null,
            MemberStatuses.Archived));
    }

    private static MemberProfile NewMember(string status) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "LF-0001",
        "Test Member",
        "+201000000000",
        null,
        new DateOnly(2026, 8, 30),
        null,
        status);
}
