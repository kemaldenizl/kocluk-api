using Security.Domain.Authorization;
using Security.Domain.Memberships;
using Security.Domain.Mfa;
using Security.Domain.Sessions;
using Security.Domain.Users;

namespace TestCommon.Builders;

/// <summary>
/// Fluent builders for the Security aggregates. Each builder starts from a valid, fully-populated state so
/// a test only has to override the one field it cares about — the rest stay realistic. This keeps the
/// arrange step short and the intent of every test obvious.
/// </summary>
public sealed class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _email = "ada@example.com";
    private string _normalizedEmail = "ADA@EXAMPLE.COM";
    private string _passwordHash = "hashed-password";
    private string _firstName = "Ada";
    private string _lastName = "Lovelace";
    private StudentType _studentType = StudentType.University;
    private DateTime _createdAtUtc = TestClock.UtcNow;

    public UserBuilder WithId(Guid id) { _id = id; return this; }
    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithNormalizedEmail(string normalizedEmail) { _normalizedEmail = normalizedEmail; return this; }
    public UserBuilder WithPasswordHash(string hash) { _passwordHash = hash; return this; }
    public UserBuilder WithFirstName(string firstName) { _firstName = firstName; return this; }
    public UserBuilder WithLastName(string lastName) { _lastName = lastName; return this; }
    public UserBuilder WithStudentType(StudentType studentType) { _studentType = studentType; return this; }
    public UserBuilder WithCreatedAt(DateTime createdAtUtc) { _createdAtUtc = createdAtUtc; return this; }

    public User Build() => new(
        _id, _email, _normalizedEmail, _passwordHash, _firstName, _lastName, _studentType, _createdAtUtc);

    public static User Default() => new UserBuilder().Build();
}

public sealed class MembershipBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private Guid _tenantId = Guid.NewGuid();
    private DateTime _joinedAtUtc = TestClock.UtcNow;

    public MembershipBuilder WithId(Guid id) { _id = id; return this; }
    public MembershipBuilder WithUserId(Guid userId) { _userId = userId; return this; }
    public MembershipBuilder WithTenantId(Guid tenantId) { _tenantId = tenantId; return this; }
    public MembershipBuilder WithJoinedAt(DateTime joinedAtUtc) { _joinedAtUtc = joinedAtUtc; return this; }

    public Membership Build() => new(_id, _userId, _tenantId, _joinedAtUtc);

    public static Membership Default() => new MembershipBuilder().Build();
}

public sealed class RefreshSessionBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private string _deviceName = "Chrome on macOS";
    private string _ipAddress = "203.0.113.4";
    private DateTime _createdAtUtc = TestClock.UtcNow;

    public RefreshSessionBuilder WithId(Guid id) { _id = id; return this; }
    public RefreshSessionBuilder WithUserId(Guid userId) { _userId = userId; return this; }
    public RefreshSessionBuilder WithDeviceName(string deviceName) { _deviceName = deviceName; return this; }
    public RefreshSessionBuilder WithIpAddress(string ipAddress) { _ipAddress = ipAddress; return this; }
    public RefreshSessionBuilder WithCreatedAt(DateTime createdAtUtc) { _createdAtUtc = createdAtUtc; return this; }

    public RefreshSession Build() => new(_id, _userId, _deviceName, _ipAddress, _createdAtUtc);

    public static RefreshSession Default() => new RefreshSessionBuilder().Build();
}

public sealed class RefreshTokenBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _sessionId = Guid.NewGuid();
    private string _tokenHash = "token-hash";
    private DateTime _expiresAtUtc = TestClock.OneDayLater;
    private DateTime _createdAtUtc = TestClock.UtcNow;

    public RefreshTokenBuilder WithId(Guid id) { _id = id; return this; }
    public RefreshTokenBuilder WithSessionId(Guid sessionId) { _sessionId = sessionId; return this; }
    public RefreshTokenBuilder WithTokenHash(string tokenHash) { _tokenHash = tokenHash; return this; }
    public RefreshTokenBuilder WithExpiresAt(DateTime expiresAtUtc) { _expiresAtUtc = expiresAtUtc; return this; }
    public RefreshTokenBuilder WithCreatedAt(DateTime createdAtUtc) { _createdAtUtc = createdAtUtc; return this; }

    public RefreshToken Build() => new(_id, _sessionId, _tokenHash, _expiresAtUtc, _createdAtUtc);

    public static RefreshToken Default() => new RefreshTokenBuilder().Build();
}

public sealed class MfaMethodBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private MfaMethodType _type = MfaMethodType.Totp;
    private string _secretHash = "secret-hash";
    private string _secretEncrypted = "secret-encrypted";
    private DateTime _createdAtUtc = TestClock.UtcNow;

    public MfaMethodBuilder WithId(Guid id) { _id = id; return this; }
    public MfaMethodBuilder WithUserId(Guid userId) { _userId = userId; return this; }
    public MfaMethodBuilder WithSecretHash(string secretHash) { _secretHash = secretHash; return this; }
    public MfaMethodBuilder WithSecretEncrypted(string secretEncrypted) { _secretEncrypted = secretEncrypted; return this; }
    public MfaMethodBuilder WithCreatedAt(DateTime createdAtUtc) { _createdAtUtc = createdAtUtc; return this; }

    public MfaMethod Build() => new(_id, _userId, _type, _secretHash, _secretEncrypted, _createdAtUtc);

    public static MfaMethod Default() => new MfaMethodBuilder().Build();
}

public static class RoleFactory
{
    public static Role Tenant(Guid? id = null, Guid? tenantId = null, string name = "Coach")
        => Role.ForTenant(id ?? Guid.NewGuid(), tenantId ?? Guid.NewGuid(), name, name.ToUpperInvariant());

    public static Role Platform(Guid? id = null, string name = "SystemAdmin")
        => Role.Platform(id ?? Guid.NewGuid(), name, name.ToUpperInvariant());
}
