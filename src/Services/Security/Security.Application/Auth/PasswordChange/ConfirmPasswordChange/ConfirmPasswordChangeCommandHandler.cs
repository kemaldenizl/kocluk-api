using MediatR;
using Microsoft.Extensions.Options;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;

namespace Security.Application.Auth.PasswordChange.ConfirmPasswordChange;

public sealed class ConfirmPasswordChangeCommandHandler(
    IUserRepository userRepository,
    IPasswordChangeTokenRepository passwordChangeTokenRepository,
    IRefreshSessionRepository refreshSessionRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IPasswordChangeTokenGenerator passwordChangeTokenGenerator,
    IPasswordHasher passwordHasher,
    IAccessTokenRevocationStore accessTokenRevocationStore,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    IOptions<SecurityTokenInvalidationOptions> invalidationOptions)
    : IRequestHandler<ConfirmPasswordChangeCommand, Result>
{
    private readonly SecurityTokenInvalidationOptions _invalidationOptions = invalidationOptions.Value;

    public async Task<Result> Handle(
        ConfirmPasswordChangeCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;
        var hashedToken = passwordChangeTokenGenerator.Hash(request.Token);

        var changeToken = await passwordChangeTokenRepository.GetByTokenHashAsync(
            hashedToken,
            cancellationToken);

        if (changeToken is null)
        {
            return Result.Failure(AuthErrors.InvalidPasswordChangeToken);
        }

        if (changeToken.Used)
        {
            return Result.Failure(AuthErrors.UsedPasswordChangeToken);
        }

        if (changeToken.IsExpired(utcNow))
        {
            return Result.Failure(AuthErrors.ExpiredPasswordChangeToken);
        }

        var user = await userRepository.GetByIdAsync(changeToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure(AuthErrors.InvalidPasswordChangeToken);
        }

        user.ChangePasswordHash(passwordHasher.Hash(request.NewPassword));
        changeToken.MarkUsed(utcNow);

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
            AuditActionType.PasswordChangeCompleted,
            AuditPayloadBuilder.Build(new
            {
                @event = "password_change_completed",
                userId = user.Id,
                email = user.Email,
                sessionsRevoked = sessions.Count,
                userAccessTokensInvalidated = true
            }),
            user.Id);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
