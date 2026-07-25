using Security.Domain.Exceptions;
using Security.Domain.Users;

namespace UnitTests.Security.Domain;

public sealed class UserTests
{
    private static User Create(
        Guid? id = null,
        string email = "ada@example.com",
        string normalizedEmail = "ADA@EXAMPLE.COM",
        string passwordHash = "hash",
        string firstName = "Ada",
        string lastName = "Lovelace",
        StudentType studentType = StudentType.University,
        DateTime? createdAt = null)
        => new(id ?? Guid.NewGuid(), email, normalizedEmail, passwordHash, firstName, lastName,
            studentType, createdAt ?? TestClock.UtcNow);

    [Fact]
    public void Constructor_sets_defaults()
    {
        var user = Create();

        user.EmailVerified.ShouldBeFalse();
        user.IsActive.ShouldBeTrue();
        user.LastLoginAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Constructor_trims_whitespace_fields()
    {
        var user = Create(email: "  ada@example.com  ", firstName: "  Ada  ");

        user.Email.ShouldBe("ada@example.com");
        user.FirstName.ShouldBe("Ada");
    }

    [Fact]
    public void Constructor_rejects_empty_id()
        => Should.Throw<DomainException>(() => Create(id: Guid.Empty));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_rejects_blank_email(string? email)
        => Should.Throw<DomainException>(() => Create(email: email!));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_blank_password_hash(string hash)
        => Should.Throw<DomainException>(() => Create(passwordHash: hash));

    [Fact]
    public void Constructor_rejects_blank_names()
    {
        Should.Throw<DomainException>(() => Create(firstName: " "));
        Should.Throw<DomainException>(() => Create(lastName: " "));
    }

    [Fact]
    public void Constructor_rejects_undefined_student_type()
        => Should.Throw<DomainException>(() => Create(studentType: (StudentType)999));

    [Fact]
    public void Constructor_rejects_default_created_at()
        => Should.Throw<DomainException>(() => new User(
            Guid.NewGuid(), "ada@example.com", "ADA@EXAMPLE.COM", "hash", "Ada", "Lovelace",
            StudentType.University, default));

    [Fact]
    public void UpdateProfile_updates_only_supplied_fields()
    {
        var user = Create(firstName: "Ada", lastName: "Lovelace", studentType: StudentType.University);

        user.UpdateProfile(firstName: "Grace", lastName: null, studentType: null);

        user.FirstName.ShouldBe("Grace");
        user.LastName.ShouldBe("Lovelace");
        user.StudentType.ShouldBe(StudentType.University);
    }

    [Fact]
    public void UpdateProfile_can_change_student_type()
    {
        var user = Create();

        user.UpdateProfile(null, null, StudentType.Graduate);

        user.StudentType.ShouldBe(StudentType.Graduate);
    }

    [Fact]
    public void UpdateProfile_rejects_blank_first_name()
        => Should.Throw<DomainException>(() => Create().UpdateProfile(" ", null, null));

    [Fact]
    public void UpdateProfile_rejects_undefined_student_type()
        => Should.Throw<DomainException>(() => Create().UpdateProfile(null, null, (StudentType)0));

    [Fact]
    public void MarkEmailVerified_sets_flag()
    {
        var user = Create();

        user.MarkEmailVerified();

        user.EmailVerified.ShouldBeTrue();
    }

    [Fact]
    public void MarkLogin_records_timestamp()
    {
        var user = Create();

        user.MarkLogin(TestClock.UtcNow);

        user.LastLoginAtUtc.ShouldBe(TestClock.UtcNow);
    }

    [Fact]
    public void MarkLogin_rejects_default_timestamp()
        => Should.Throw<DomainException>(() => Create().MarkLogin(default));

    [Fact]
    public void ChangePasswordHash_updates_hash()
    {
        var user = Create();

        user.ChangePasswordHash("new-hash");

        user.PasswordHash.ShouldBe("new-hash");
    }

    [Fact]
    public void ChangePasswordHash_rejects_blank()
        => Should.Throw<DomainException>(() => Create().ChangePasswordHash(" "));

    [Fact]
    public void ChangeEmail_updates_and_resets_verification()
    {
        var user = Create();
        user.MarkEmailVerified();

        user.ChangeEmail("grace@example.com", "GRACE@EXAMPLE.COM");

        user.Email.ShouldBe("grace@example.com");
        user.NormalizedEmail.ShouldBe("GRACE@EXAMPLE.COM");
        user.EmailVerified.ShouldBeFalse();
    }

    [Fact]
    public void ChangeEmail_rejects_blank()
        => Should.Throw<DomainException>(() => Create().ChangeEmail(" ", "X"));

    [Fact]
    public void Deactivate_then_activate_toggles_state()
    {
        var user = Create();

        user.Deactivate();
        user.IsActive.ShouldBeFalse();

        user.Activate();
        user.IsActive.ShouldBeTrue();
    }
}
