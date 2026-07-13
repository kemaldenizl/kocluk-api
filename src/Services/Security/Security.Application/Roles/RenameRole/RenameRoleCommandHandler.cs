using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;

namespace Security.Application.Roles.RenameRole;

public sealed class RenameRoleCommandHandler(
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RenameRoleCommand, Result>
{
    public async Task<Result> Handle(
        RenameRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        var name = request.Name.Trim();
        var normalizedName = name.ToUpperInvariant();

        if (!string.Equals(role.NormalizedName, normalizedName, StringComparison.Ordinal))
        {
            var alreadyExists = await roleRepository.ExistsByNormalizedNameAsync(normalizedName, cancellationToken);
            if (alreadyExists)
                return Result.Failure(RoleErrors.AlreadyExists);
        }

        role.Rename(name, normalizedName);

        var auditLog = auditLogFactory.Create(
            AuditActionType.RoleRenamed,
            AuditPayloadBuilder.Build(new
            {
                @event = "role_renamed",
                roleId = role.Id,
                roleName = role.Name
            }));

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
