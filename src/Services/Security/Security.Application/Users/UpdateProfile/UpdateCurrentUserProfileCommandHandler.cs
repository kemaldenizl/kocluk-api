using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Application.Users.GetCurrentUser;
using Security.Domain.Auditing;

namespace Security.Application.Users.UpdateProfile;

public sealed class UpdateCurrentUserProfileCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCurrentUserProfileCommand, Result<CurrentUserProfileDto>>
{
    public async Task<Result<CurrentUserProfileDto>> Handle(
        UpdateCurrentUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<CurrentUserProfileDto>.Failure(UserErrors.NotFound);
        }

        user.UpdateProfile(request.FirstName, request.LastName, request.StudentType);

        var auditLog = auditLogFactory.Create(
            AuditActionType.UserProfileUpdated,
            AuditPayloadBuilder.Build(new
            {
                @event = "user_profile_updated",
                userId = user.Id,
                firstNameChanged = request.FirstName is not null,
                lastNameChanged = request.LastName is not null,
                studentTypeChanged = request.StudentType is not null
            }),
            user.Id);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var profile = new CurrentUserProfileDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.StudentType,
            user.EmailVerified,
            user.IsActive);

        return Result<CurrentUserProfileDto>.Success(profile);
    }
}
