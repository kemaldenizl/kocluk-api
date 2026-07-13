using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Messaging;
using Security.Application.Abstractions.Messaging.IntegrationEvents;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Auth.EmailChange.Dtos;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;
using Security.Domain.Tokens;

namespace Security.Application.Auth.EmailChange.RequestEmailChange;

public sealed class RequestEmailChangeCommandHandler(
    IUserRepository userRepository,
    IEmailChangeTokenRepository emailChangeTokenRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IEmailChangeTokenGenerator emailChangeTokenGenerator,
    IPasswordHasher passwordHasher,
    IEventPublisher eventPublisher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RequestEmailChangeCommand, Result<RequestEmailChangeResponse>>
{
    private static readonly TimeSpan EmailChangeTokenLifetime = TimeSpan.FromMinutes(30);

    public async Task<Result<RequestEmailChangeResponse>> Handle(RequestEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result<RequestEmailChangeResponse>.Failure(AuthErrors.InvalidCurrentPassword);
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            return Result<RequestEmailChangeResponse>.Failure(AuthErrors.InvalidCurrentPassword);
        }

        var utcNow = dateTimeProvider.UtcNow;
        var tokenPair = emailChangeTokenGenerator.Generate();

        var changeToken = new EmailChangeToken(
            Guid.NewGuid(),
            user.Id,
            tokenPair.HashedToken,
            utcNow.Add(EmailChangeTokenLifetime),
            utcNow);

        await emailChangeTokenRepository.AddAsync(changeToken, cancellationToken);

        var auditLog = auditLogFactory.Create(
            AuditActionType.EmailChangeRequested,
            AuditPayloadBuilder.Build(new
            {
                @event = "email_change_requested",
                userId = user.Id,
                email = user.Email
            }),
            user.Id);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);

        await eventPublisher.PublishAsync(
            new EmailChangeRequestedIntegrationEvent(
                user.Id,
                user.Email,
                tokenPair.PlainTextToken,
                utcNow),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RequestEmailChangeResponse>.Success(new RequestEmailChangeResponse("A confirmation link has been sent to your email address."));
    }
}
