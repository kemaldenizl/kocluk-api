using MediatR;
using Microsoft.Extensions.Options;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Messaging;
using Security.Application.Abstractions.Messaging.IntegrationEvents;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;
using Security.Domain.Tokens;

namespace Security.Application.Auth.EmailChange.ConfirmEmailChange;

public sealed class ConfirmEmailChangeCommandHandler(
    IUserRepository userRepository,
    IEmailChangeTokenRepository emailChangeTokenRepository,
    IEmailVerificationTokenRepository emailVerificationTokenRepository,
    IRefreshSessionRepository refreshSessionRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IEmailChangeTokenGenerator emailChangeTokenGenerator,
    IEmailVerificationTokenGenerator emailVerificationTokenGenerator,
    IEventPublisher eventPublisher,
    IAccessTokenRevocationStore accessTokenRevocationStore,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<SecurityTokenInvalidationOptions> invalidationOptions)
    : IRequestHandler<ConfirmEmailChangeCommand, Result>
{
    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(24);
    private readonly SecurityTokenInvalidationOptions _invalidationOptions = invalidationOptions.Value;

    public async Task<Result> Handle(
        ConfirmEmailChangeCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var hashedToken = emailChangeTokenGenerator.Hash(request.Token);

        var changeToken = await emailChangeTokenRepository.GetByTokenHashAsync(
            hashedToken,
            cancellationToken);

        if (changeToken is null)
        {
            return Result.Failure(AuthErrors.InvalidEmailChangeToken);
        }

        if (changeToken.Used)
        {
            return Result.Failure(AuthErrors.UsedEmailChangeToken);
        }

        if (changeToken.IsExpired(utcNow))
        {
            return Result.Failure(AuthErrors.ExpiredEmailChangeToken);
        }

        var user = await userRepository.GetByIdAsync(changeToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure(AuthErrors.InvalidEmailChangeToken);
        }

        var newEmail = request.NewEmail.Trim();
        var normalizedNewEmail = newEmail.ToUpperInvariant();

        var alreadyInUse = await userRepository.ExistsByNormalizedEmailAsync(normalizedNewEmail, cancellationToken);
        if (alreadyInUse)
        {
            return Result.Failure(AuthErrors.EmailAlreadyInUse);
        }

        user.ChangeEmail(newEmail, normalizedNewEmail);
        changeToken.MarkUsed(utcNow);

        var verificationTokenPair = emailVerificationTokenGenerator.Generate();

        var verificationToken = new EmailVerificationToken(
            Guid.NewGuid(),
            user.Id,
            verificationTokenPair.HashedToken,
            utcNow.Add(VerificationTokenLifetime),
            utcNow);

        await emailVerificationTokenRepository.AddAsync(verificationToken, cancellationToken);

        var sessions = await refreshSessionRepository.GetByUserIdAsync(user.Id, cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke(utcNow);
        }

        await accessTokenRevocationStore.RevokeUserAsync(
            user.Id,
            utcNow,
            utcNow.AddHours(_invalidationOptions.UserInvalidationRetentionHours),
            cancellationToken);

        var auditLog = auditLogFactory.Create(
            AuditActionType.EmailChangeCompleted,
            AuditPayloadBuilder.Build(new
            {
                @event = "email_change_completed",
                userId = user.Id,
                email = user.Email,
                sessionsRevoked = sessions.Count,
                userAccessTokensInvalidated = true
            }),
            user.Id);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);

        await eventPublisher.PublishAsync(
            new EmailVerificationRequestedIntegrationEvent(
                user.Id,
                user.Email,
                verificationTokenPair.PlainTextToken,
                utcNow),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
