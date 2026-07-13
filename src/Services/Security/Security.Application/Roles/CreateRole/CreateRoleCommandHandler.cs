using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Application.Roles.Dtos;
using Security.Domain.Auditing;
using Security.Domain.Authorization;

namespace Security.Application.Roles.CreateRole;

public sealed class CreateRoleCommandHandler(
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var normalizedName = name.ToUpperInvariant();

        var alreadyExists = await roleRepository.ExistsByNormalizedNameAsync(normalizedName, cancellationToken);
        if (alreadyExists)
            return Result<RoleDto>.Failure(RoleErrors.AlreadyExists);

        var role = new Role(Guid.NewGuid(), name, normalizedName);

        await roleRepository.AddAsync(role, cancellationToken);

        var auditLog = auditLogFactory.Create(
            AuditActionType.RoleCreated,
            AuditPayloadBuilder.Build(new
            {
                @event = "role_created",
                roleId = role.Id,
                roleName = role.Name
            }));

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = RoleMapper.ToDto(role, new Dictionary<Guid, string>());

        return Result<RoleDto>.Success(response);
    }
}
