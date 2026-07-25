using BuildingBlocks.Domain;
using Security.Domain.Common;

namespace Security.Domain.Users;

/// <summary>
/// A platform user identified by a globally-unique email address. Users are tenant-independent:
/// a single account can belong to many tenants through <see cref="Security.Domain.Memberships.Membership"/>,
/// and roles are granted per-membership rather than on the user itself.
/// </summary>
public sealed class User : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string NormalizedEmail { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public StudentType StudentType { get; private set; }
    public bool EmailVerified { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }

    private User()
    {
    }

    public User(
        Guid id,
        string email,
        string normalizedEmail,
        string passwordHash,
        string firstName,
        string lastName,
        StudentType studentType,
        DateTime createdAtUtc)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        Email = Guard.AgainstNullOrWhiteSpace(email, nameof(email));
        NormalizedEmail = Guard.AgainstNullOrWhiteSpace(normalizedEmail, nameof(normalizedEmail));
        PasswordHash = Guard.AgainstNullOrWhiteSpace(passwordHash, nameof(passwordHash));
        FirstName = Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        LastName = Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
        StudentType = GuardAgainstUndefinedStudentType(studentType);
        CreatedAtUtc = Guard.AgainstDefault(createdAtUtc, nameof(createdAtUtc));

        EmailVerified = false;
        IsActive = true;
    }

    public void UpdateProfile(string? firstName, string? lastName, StudentType? studentType)
    {
        if (firstName is not null)
            FirstName = Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));

        if (lastName is not null)
            LastName = Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));

        if (studentType is not null)
            StudentType = GuardAgainstUndefinedStudentType(studentType.Value);
    }

    public void MarkEmailVerified()
    {
        EmailVerified = true;
    }

    public void MarkLogin(DateTime utcNow)
    {
        LastLoginAtUtc = Guard.AgainstDefault(utcNow, nameof(utcNow));
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        PasswordHash = Guard.AgainstNullOrWhiteSpace(newPasswordHash, nameof(newPasswordHash));
    }

    public void ChangeEmail(string email, string normalizedEmail)
    {
        Email = Guard.AgainstNullOrWhiteSpace(email, nameof(email));
        NormalizedEmail = Guard.AgainstNullOrWhiteSpace(normalizedEmail, nameof(normalizedEmail));
        EmailVerified = false;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    private static StudentType GuardAgainstUndefinedStudentType(StudentType studentType)
    {
        Guard.Against(
            !Enum.IsDefined(studentType),
            $"{nameof(studentType)} is not a valid student type.");

        return studentType;
    }
}
