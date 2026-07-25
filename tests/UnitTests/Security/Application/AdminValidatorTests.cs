using FluentValidation.TestHelper;
using Security.Application.Memberships.AddMember;
using Security.Application.Permissions.CreatePermission;
using Security.Application.Permissions.DeletePermission;
using Security.Application.Roles.AddPermissionToRole;
using Security.Application.Roles.CreateRole;
using Security.Application.Roles.DeleteRole;
using Security.Application.Roles.RemovePermissionFromRole;
using Security.Application.Roles.RenameRole;
using Security.Application.Sessions.RevokeSession;
using Security.Application.Users.UpdateProfile;
using Security.Domain.Users;

namespace UnitTests.Security.Application;

public sealed class RoleValidatorTests
{
    [Fact]
    public void CreateRole_valid_passes()
        => new CreateRoleCommandValidator().TestValidate(new CreateRoleCommand(Guid.NewGuid(), Guid.NewGuid(), "Coach"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void CreateRole_rejects_empty_and_too_long_name()
    {
        var validator = new CreateRoleCommandValidator();
        validator.TestValidate(new CreateRoleCommand(Guid.NewGuid(), Guid.NewGuid(), ""))
            .ShouldHaveValidationErrorFor(x => x.Name);
        validator.TestValidate(new CreateRoleCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 201)))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RenameRole_rejects_empty_role_and_name()
    {
        var result = new RenameRoleCommandValidator().TestValidate(new RenameRoleCommand(Guid.Empty, Guid.NewGuid(), ""));
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void DeleteRole_rejects_empty_role()
        => new DeleteRoleCommandValidator().TestValidate(new DeleteRoleCommand(Guid.Empty, Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(x => x.RoleId);

    [Fact]
    public void AddPermissionToRole_rejects_empty_ids()
    {
        var result = new AddPermissionToRoleCommandValidator()
            .TestValidate(new AddPermissionToRoleCommand(Guid.Empty, Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
        result.ShouldHaveValidationErrorFor(x => x.PermissionId);
    }

    [Fact]
    public void RemovePermissionFromRole_rejects_empty_ids()
    {
        var result = new RemovePermissionFromRoleCommandValidator()
            .TestValidate(new RemovePermissionFromRoleCommand(Guid.Empty, Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
        result.ShouldHaveValidationErrorFor(x => x.PermissionId);
    }
}

public sealed class PermissionValidatorTests
{
    [Fact]
    public void CreatePermission_valid_passes()
        => new CreatePermissionCommandValidator().TestValidate(new CreatePermissionCommand("reports.read"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void CreatePermission_rejects_empty_and_too_long_code()
    {
        var validator = new CreatePermissionCommandValidator();
        validator.TestValidate(new CreatePermissionCommand("")).ShouldHaveValidationErrorFor(x => x.Code);
        validator.TestValidate(new CreatePermissionCommand(new string('a', 201))).ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void DeletePermission_rejects_empty_id()
        => new DeletePermissionCommandValidator().TestValidate(new DeletePermissionCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.PermissionId);
}

public sealed class RevokeSessionValidatorTests
{
    [Fact]
    public void Valid_passes()
        => new RevokeSessionCommandValidator()
            .TestValidate(new RevokeSessionCommand(Guid.NewGuid(), Guid.NewGuid(), "jti", TestClock.UtcNow, Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void CurrentSessionId_may_be_null()
        => new RevokeSessionCommandValidator()
            .TestValidate(new RevokeSessionCommand(Guid.NewGuid(), Guid.NewGuid(), "jti", TestClock.UtcNow, null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Rejects_empty_ids_and_default_expiry()
    {
        var result = new RevokeSessionCommandValidator()
            .TestValidate(new RevokeSessionCommand(Guid.Empty, Guid.Empty, "", default, null));
        result.ShouldHaveValidationErrorFor(x => x.UserId);
        result.ShouldHaveValidationErrorFor(x => x.SessionId);
        result.ShouldHaveValidationErrorFor(x => x.AccessTokenJti);
        result.ShouldHaveValidationErrorFor(x => x.AccessTokenExpiresAtUtc);
    }
}

public sealed class AddMemberValidatorTests
{
    private readonly AddMemberCommandValidator _validator = new();

    [Fact]
    public void Accepts_user_id_identification()
        => _validator.TestValidate(new AddMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, []))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Accepts_email_identification()
        => _validator.TestValidate(new AddMemberCommand(Guid.NewGuid(), Guid.NewGuid(), null, "ada@example.com", []))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Rejects_when_neither_user_nor_email()
        => _validator.TestValidate(new AddMemberCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, []))
            .ShouldHaveValidationErrorFor(x => x);

    [Fact]
    public void Rejects_empty_tenant_and_caller()
    {
        var result = _validator.TestValidate(new AddMemberCommand(Guid.Empty, Guid.Empty, Guid.NewGuid(), null, []));
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
        result.ShouldHaveValidationErrorFor(x => x.CallerUserId);
    }

    [Fact]
    public void Rejects_empty_role_id_in_collection()
        => _validator.TestValidate(new AddMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, [Guid.Empty]))
            .ShouldHaveValidationErrorFor("RoleIds[0]");
}

public sealed class UpdateProfileValidatorTests
{
    private readonly UpdateCurrentUserProfileCommandValidator _validator = new();

    [Fact]
    public void Accepts_single_field_update()
        => _validator.TestValidate(new UpdateCurrentUserProfileCommand(Guid.NewGuid(), "Ada", null, null))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Rejects_when_no_field_supplied()
        => _validator.TestValidate(new UpdateCurrentUserProfileCommand(Guid.NewGuid(), null, null, null))
            .ShouldHaveValidationErrorFor(x => x.FirstName);

    [Fact]
    public void Rejects_blank_supplied_first_name()
        => _validator.TestValidate(new UpdateCurrentUserProfileCommand(Guid.NewGuid(), "", null, null))
            .ShouldHaveValidationErrorFor(x => x.FirstName);

    [Fact]
    public void Rejects_too_long_last_name()
        => _validator.TestValidate(new UpdateCurrentUserProfileCommand(Guid.NewGuid(), null, new string('a', 101), null))
            .ShouldHaveValidationErrorFor(x => x.LastName);

    [Fact]
    public void Rejects_undefined_student_type()
        => _validator.TestValidate(new UpdateCurrentUserProfileCommand(Guid.NewGuid(), null, null, (StudentType)999))
            .ShouldHaveValidationErrorFor(x => x.StudentType!.Value);
}
