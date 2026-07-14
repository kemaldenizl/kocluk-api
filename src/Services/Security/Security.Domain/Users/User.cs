using BuildingBlocks.Domain;
using Security.Domain.Abstractions;
using Security.Domain.Common;

namespace Security.Domain.Users;

public sealed class User : AggregateRoot, ITenantScoped
{
    private readonly List<UserRole> _roles = [];

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
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

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

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

    public void AddRole(Guid roleId, DateTime assignedAtUtc)
    {
        roleId = Guard.AgainstEmpty(roleId, nameof(roleId));
        assignedAtUtc = Guard.AgainstDefault(assignedAtUtc, nameof(assignedAtUtc));

        if (_roles.Any(x => x.RoleId == roleId))
            return;

        _roles.Add(new UserRole(Id, roleId, assignedAtUtc));
    }

    public void RemoveRole(Guid roleId)
    {
        roleId = Guard.AgainstEmpty(roleId, nameof(roleId));

        var existing = _roles.FirstOrDefault(x => x.RoleId == roleId);
        if (existing is null)
            return;

        _roles.Remove(existing);
    }

    private static StudentType GuardAgainstUndefinedStudentType(StudentType studentType)
    {
        Guard.Against(
            !Enum.IsDefined(studentType),
            $"{nameof(studentType)} is not a valid student type.");

        return studentType;
    }
}