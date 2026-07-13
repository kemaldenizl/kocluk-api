using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Messaging;
using Security.Application.Abstractions.Messaging.IntegrationEvents;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Auth.EmailVerification.Dtos;
using Security.Application.Common.Auditing;
using Security.Application.Common.Results;
using Security.Domain.Auditing;
using Security.Domain.Tokens;

namespace Security.Application.Auth.EmailVerification.ResendVerification;

public sealed class ResendVerificationCommandHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository emailVerificationTokenRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IEmailVerificationTokenGenerator emailVerificationTokenGenerator,
    IEventPublisher eventPublisher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ResendVerificationCommand, Result<ResendVerificationResponse>>
{
    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(24);

    public async Task<Result<ResendVerificationResponse>> Handle(
        ResendVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is not null && user.IsActive && !user.EmailVerified)
        {
            var utcNow = dateTimeProvider.UtcNow;
            var tokenPair = emailVerificationTokenGenerator.Generate();

            var verificationToken = new EmailVerificationToken(
                Guid.NewGuid(),
                user.Id,
                tokenPair.HashedToken,
                utcNow.Add(VerificationTokenLifetime),
                utcNow);

            await emailVerificationTokenRepository.AddAsync(verificationToken, cancellationToken);

            var auditLog = auditLogFactory.Create(
                AuditActionType.EmailVerificationRequested,
                AuditPayloadBuilder.Build(new
                {
                    @event = "email_verification_requested",
                    userId = user.Id,
                    email = user.Email,
                    reason = "resend"
                }),
                user.Id);

            await auditLogRepository.AddAsync(auditLog, cancellationToken);

            await eventPublisher.PublishAsync(
                new EmailVerificationRequestedIntegrationEvent(
                    user.Id,
                    user.Email,
                    tokenPair.PlainTextToken,
                    utcNow),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result<ResendVerificationResponse>.Success(
            new ResendVerificationResponse(
                "If the account exists and is not verified, a new verification link has been generated."));
    }
}