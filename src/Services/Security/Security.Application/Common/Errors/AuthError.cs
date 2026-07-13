namespace Security.Application.Common.Errors;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new("auth.invalid_credentials", "Invalid credentials.");
    public static readonly Error UserAlreadyExists = new("auth.user_already_exists", "User already exists.");
    public static readonly Error UserInactive = new("auth.user_inactive", "User is inactive.");
    public static readonly Error EmailNotVerified = new("auth.email_not_verified", "Email is not verified.");
    public static readonly Error RoleNotFound = new("auth.role_not_found", "Required role was not found.");
    public static readonly Error InvalidRefreshToken = new("auth.invalid_refresh_token", "Refresh token is invalid.");
    public static readonly Error ExpiredRefreshToken = new("auth.expired_refresh_token", "Refresh token is expired.");
    public static readonly Error RevokedRefreshToken = new("auth.revoked_refresh_token", "Refresh token is revoked.");   
    public static readonly Error ConsumedRefreshToken = new("auth.consumed_refresh_token", "Refresh token has already been used.");
    public static readonly Error SessionRevoked = new("auth.session_revoked", "Session is revoked.");
    public static readonly Error RefreshTokenReuseDetected = new("auth.refresh_token_reuse_detected", "Refresh token reuse detected. Session has been revoked.");
    public static readonly Error InvalidSession = new("auth.invalid_session", "Session is invalid.");
    public static readonly Error SessionNotFound = new("auth.session_not_found", "Session was not found.");
    public static readonly Error InvalidPasswordResetToken = new("auth.invalid_password_reset_token", "Password reset token is invalid.");
    public static readonly Error ExpiredPasswordResetToken = new("auth.expired_password_reset_token", "Password reset token is expired.");
    public static readonly Error UsedPasswordResetToken = new("auth.used_password_reset_token", "Password reset token has already been used.");
    public static readonly Error InvalidCurrentPassword = new("auth.invalid_current_password", "Current password is incorrect.");
    public static readonly Error InvalidPasswordChangeToken = new("auth.invalid_password_change_token", "Password change token is invalid.");
    public static readonly Error ExpiredPasswordChangeToken = new("auth.expired_password_change_token", "Password change token is expired.");
    public static readonly Error UsedPasswordChangeToken = new("auth.used_password_change_token", "Password change token has already been used.");
    public static readonly Error InvalidEmailChangeToken = new("auth.invalid_email_change_token", "Email change token is invalid.");
    public static readonly Error ExpiredEmailChangeToken = new("auth.expired_email_change_token", "Email change token is expired.");
    public static readonly Error UsedEmailChangeToken = new("auth.used_email_change_token", "Email change token has already been used.");
    public static readonly Error EmailAlreadyInUse = new("auth.email_already_in_use", "Email address is already in use.");
    public static readonly Error InvalidEmailVerificationToken = new("auth.invalid_email_verification_token", "Email verification token is invalid.");
    public static readonly Error ExpiredEmailVerificationToken = new("auth.expired_email_verification_token", "Email verification token is expired.");
    public static readonly Error UsedEmailVerificationToken = new("auth.used_email_verification_token", "Email verification token has already been used.");
    public static readonly Error EmailAlreadyVerified = new("auth.email_already_verified", "Email is already verified.");
    public static readonly Error MfaNotEnabled = new("auth.mfa_not_enabled", "MFA is not enabled.");
    public static readonly Error InvalidMfaCode = new("auth.invalid_mfa_code", "The MFA code is invalid.");
}