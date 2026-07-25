using BuildingBlocks.Messaging.Abstractions;
using MediatR;
using Microsoft.Extensions.Options;
using Security.Application.Abstractions.Auditing;
using BuildingBlocks.Messaging.Contracts;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Auth.Dtos;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Application.Common.Tenancy;
using Security.Domain.Auditing;
using Security.Domain.Authorization;
using Security.Domain.Memberships;
using Security.Domain.Tokens;
using Security.Domain.Users;

namespace Security.Application.Auth.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IMembershipRepository membershipRepository,
    IRoleRepository roleRepository,
    IEmailVerificationTokenRepository emailVerificationTokenRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IPasswordHasher passwordHasher,
    IEmailVerificationTokenGenerator emailVerificationTokenGenerator,
    IEventPublisher eventPublisher,
    IOptions<TenantDefaultsOptions> tenantDefaults,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private const string DefaultMemberRoleNormalizedName = "MEMBER";
    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(24);

    private readonly TenantDefaultsOptions _tenantDefaults = tenantDefaults.Value;

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        var alreadyExists = await userRepository.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (alreadyExists)
        {
            return Result<RegisterResponse>.Failure(AuthErrors.UserAlreadyExists);
        }

        var utcNow = dateTimeProvider.UtcNow;
        var publicTenantId = _tenantDefaults.PublicTenantId == Guid.Empty
            ? TenantDefaultsOptions.PublicTenantIdValue
            : _tenantDefaults.PublicTenantId;

        var user = new User(
            Guid.NewGuid(),
            request.Email.Trim(),
            normalizedEmail,
            passwordHasher.Hash(request.Password),
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.StudentType,
            utcNow);

        await userRepository.AddAsync(user, cancellationToken);

        // Self-registration always enrolls the user into the public tenant with the default member role.
        var membership = new Membership(Guid.NewGuid(), user.Id, publicTenantId, utcNow);

        var memberRole = await roleRepository.GetByTenantAndNormalizedNameAsync(
            publicTenantId,
            DefaultMemberRoleNormalizedName,
            cancellationToken);

        if (memberRole is not null)
        {
            membership.AddRole(memberRole.Id, utcNow);
        }

        await membershipRepository.AddAsync(membership, cancellationToken);

        var tokenPair = emailVerificationTokenGenerator.Generate();

        var verificationToken = new EmailVerificationToken(
            Guid.NewGuid(),
            user.Id,
            tokenPair.HashedToken,
            utcNow.Add(VerificationTokenLifetime),
            utcNow);

        await emailVerificationTokenRepository.AddAsync(verificationToken, cancellationToken);

        var auditLog = auditLogFactory.Create(
            AuditActionType.UserRegistered,
            AuditPayloadBuilder.Build(new
            {
                @event = "user_registered",
                userId = user.Id,
                email = user.Email,
                tenantId = publicTenantId,
                emailVerificationStarted = true
            }),
            user.Id);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);

        var verificationAuditLog = auditLogFactory.Create(
            AuditActionType.EmailVerificationRequested,
            AuditPayloadBuilder.Build(new
            {
                @event = "email_verification_requested",
                userId = user.Id,
                email = user.Email,
                reason = "register"
            }),
            user.Id);

        await auditLogRepository.AddAsync(verificationAuditLog, cancellationToken);

        await eventPublisher.PublishAsync(
            new EmailVerificationRequestedIntegrationEvent(
                publicTenantId,
                user.Id,
                user.Email,
                tokenPair.PlainTextToken,
                utcNow),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new RegisterResponse(new UserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.StudentType,
            user.EmailVerified,
            user.IsActive));
        return Result<RegisterResponse>.Success(response);
    }
}
