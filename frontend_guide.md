# Frontend Integration Guide — Kocluk API

Everything the frontend needs to talk to the backend. All browser/app traffic goes
through the **API Gateway**; do not call the individual services directly.

- **Base URL (local Docker):** `http://localhost:8080`
- **Content type:** `application/json` for every request/response body
- **JSON casing:** all fields are **camelCase** (e.g. `accessToken`, `emailVerified`)
- **Enums:** serialized as strings in Security API responses/requests

> Routes are grouped by area below. For each route you get: method + path, whether it
> needs authentication, any required permission, the request body, the success
> response, and notable error statuses.

---

## 1. Authentication model

Auth is JWT bearer. The flow:

1. `POST /api/auth/register` creates the account and enrolls the user into the
   seeded **public tenant**.
2. `POST /api/auth/login` returns an **access token** (short-lived, 15 min in
   development) and a **refresh token**. If the user has MFA enabled, login returns
   an MFA challenge instead of tokens (see [MFA](#5-mfa--apimfa)).
3. Send the access token on every protected call:
   ```http
   Authorization: Bearer <accessToken>
   ```
4. When the access token expires, call `POST /api/auth/refresh` with the refresh
   token to get a new token pair. Refresh tokens are rotated.
5. `POST /api/auth/logout` revokes the current session; `POST /api/auth/logout-all`
   revokes every session for the user.

### No tenant header

Login is **tenant-independent**: users authenticate with email + password only.
There is **no `X-Tenant-Id` header**. A user is globally unique by email and can
belong to many tenants; self-registration automatically creates an active
membership in the public tenant.

Tenant-scoped management routes use an explicit `tenantId` in the query string or
request body. The backend checks the caller's active membership in that tenant,
unless the caller is a platform system administrator.

### Token payload (access token claims)

The access token carries:

- `sub`: user id
- `email`
- `jti`: access token id
- `sid`: refresh session id
- `system_admin`: `true` / `false`
- `platform_role`: repeated platform role claims, if any
- `permission`: repeated permission claims; this is the union of the user's active
  permissions
- `memberships`: JSON array of `{ tenantId, roles, status }`

The frontend can decode claims for display and optimistic UI decisions, but
authorization is always enforced server-side.

### User profile fields

User-facing profile responses include:

```json
{
  "id": "guid",
  "email": "user@example.com",
  "firstName": "Ada",
  "lastName": "Lovelace",
  "studentType": "HighSchool",
  "emailVerified": false,
  "isActive": true
}
```

Allowed `studentType` values:

- `MiddleSchool`
- `HighSchool`
- `University`
- `Graduate`
- `Other`

---

## 2. Conventions

### Errors — RFC 7807 `application/problem+json`

Every error (400/401/403/404/409/429/5xx) returns a problem response:

```json
{
  "type": "https://httpstatuses.com/401",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication is required or the access token is invalid.",
  "instance": "/api/users/me",
  "correlationId": "0HN..."
}
```

### Validation errors — 400

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "...",
  "errors": {
    "email": ["'Email' is not a valid email address."],
    "password": ["Password must be at least 12 characters."]
  }
}
```

### Common validation notes

- Passwords must be 12-200 chars and include uppercase, lowercase, digit, and
  non-alphanumeric characters.
- Email max length is 320 chars.
- Names max length is 100 chars for user profile fields, 200 chars for role/tenant
  names.
- Token fields max length is 2048 chars.
- Tenant slugs are optional on create; when supplied they must match
  `^[a-z0-9]+(?:-[a-z0-9]+)*$` and max length is 100.

### Rate limiting — 429

Security enforces per-endpoint rate limits. On exceeding a limit you get `429`
with a problem+json body and usually a `Retry-After` header.

| Area                         | Limit / 60s |
|------------------------------|-------------|
| Register                     | 3           |
| Login / MFA login complete   | 5           |
| Refresh                      | 10          |
| Logout / logout-all          | 10          |
| Sessions                     | 20          |
| Forgot password              | 3           |
| Reset password               | 5           |
| Change-password request      | 3           |
| Change-password confirm      | 5           |
| Change-email request         | 3           |
| Change-email validate        | 10          |
| Change-email confirm         | 5           |
| Verify email                 | 10          |
| Resend verification          | 3           |
| Admin bucket                 | 30          |

The Admin bucket covers memberships, roles, permissions, and test/admin helpers.
MFA setup/disable/recovery-code routes currently reuse the verify-email rate-limit
bucket.

### Auth-related status codes

- **401 Unauthorized** — missing/invalid/expired/revoked access token.
- **403 Forbidden** — authenticated, but missing the required permission or tenant
  access.

---

## 3. Permissions catalogue

Permission claims are issued in the access token and are also checked against the
database for tenant-scoped actions.

| Code                  | Grants                                   |
|-----------------------|------------------------------------------|
| `users.read`          | read user/admin user information          |
| `users.manage`        | manage users                              |
| `roles.read`          | list/read tenant roles                    |
| `roles.manage`        | create/rename/delete tenant roles, edit role permissions |
| `permissions.read`    | list permission catalogue                 |
| `permissions.manage`  | create/delete permissions                 |
| `sessions.read`       | reserved session-read capability          |
| `sessions.manage`     | reserved session-management capability    |
| `members.read`        | list members in a tenant                  |
| `members.manage`      | add/remove/suspend/reactivate members and assign roles |
| `tenants.read`        | list/read tenants                         |
| `tenants.manage`      | create/rename/activate/deactivate/delete tenants |

The seeded development admin (`admin@local.com` / `DummyPw123!` in Compose defaults)
has the platform `SystemAdmin` role and full access.

---

## 4. Auth — `/api/auth`

Gateway route: `/api/auth/{**catch-all}` is allowed through the gateway without edge
auth, but individual Security API endpoints still enforce auth where listed.

### POST `/api/auth/register`  · public
Create a new account, enroll it into the public tenant, and start email verification.

```json
// request
{
  "email": "user@example.com",
  "password": "DummyPw123!",
  "confirmPassword": "DummyPw123!",
  "firstName": "Ada",
  "lastName": "Lovelace",
  "studentType": "HighSchool"
}
```

```json
// 201 Created
{
  "user": {
    "id": "guid",
    "email": "user@example.com",
    "firstName": "Ada",
    "lastName": "Lovelace",
    "studentType": "HighSchool",
    "emailVerified": false,
    "isActive": true
  }
}
```

Errors: `400` validation · `409` email already exists · `429`.

---

### POST `/api/auth/login`  · public
Authenticate. Returns tokens, or an MFA challenge when MFA is enabled.

```json
// request
{ "email": "user@example.com", "password": "DummyPw123!" }
```

```json
// 200 OK — no MFA
{
  "user": {
    "id": "guid",
    "email": "user@example.com",
    "firstName": "Ada",
    "lastName": "Lovelace",
    "studentType": "HighSchool",
    "emailVerified": true,
    "isActive": true
  },
  "tokens": {
    "accessToken": "eyJ...",
    "accessTokenExpiresAtUtc": "2026-07-25T12:15:00Z",
    "refreshToken": "base64...",
    "refreshTokenExpiresAtUtc": "2026-08-01T12:00:00Z"
  },
  "mfaChallenge": null,
  "requiresMfa": false
}
```

```json
// 200 OK — MFA required (no tokens yet)
{
  "user": {
    "id": "guid",
    "email": "user@example.com",
    "firstName": "Ada",
    "lastName": "Lovelace",
    "studentType": "HighSchool",
    "emailVerified": true,
    "isActive": true
  },
  "tokens": null,
  "mfaChallenge": {
    "challengeToken": "opaque-token",
    "expiresAtUtc": "2026-07-25T12:05:00Z"
  },
  "requiresMfa": true
}
```

When `requiresMfa` is `true`, call `POST /api/mfa/login/complete` with the
`challengeToken`. Errors: `400` · `401` invalid credentials · `403` inactive/blocked
· `429`.

---

### POST `/api/auth/refresh`  · public
Rotate tokens using a valid refresh token.

```json
// request
{ "refreshToken": "base64..." }
```

```json
// 200 OK
{
  "tokens": {
    "accessToken": "eyJ...",
    "accessTokenExpiresAtUtc": "2026-07-25T12:15:00Z",
    "refreshToken": "base64...",
    "refreshTokenExpiresAtUtc": "2026-08-01T12:00:00Z"
  }
}
```

Errors: `400` · `401` invalid/expired/used refresh token · `429`.

---

### POST `/api/auth/logout`  · 🔒 auth required
Revoke the current session. No request body.

Success: `204 No Content`.

Errors: `400` invalid token/session context · `401` · `429`.

### POST `/api/auth/logout-all`  · 🔒 auth required
Revoke all sessions of the current user. No request body.

Success: `204 No Content`.

Errors: `400` invalid token context · `401` · `429`.

---

### POST `/api/auth/forgot-password`  · public
Start password reset. Response is intentionally generic.

```json
// request
{ "email": "user@example.com" }
```

```json
// 202 Accepted
{ "message": "If the account exists, a reset link has been sent." }
```

Errors: `400` · `429`.

### POST `/api/auth/reset-password`  · public
Complete reset with the emailed token.

```json
// request
{
  "token": "reset-token",
  "newPassword": "NewDummyPw123!",
  "confirmNewPassword": "NewDummyPw123!"
}
```

Success: `204 No Content`.

Errors: `400` invalid/expired token or weak password · `429`.

---

### POST `/api/auth/change-password/request`  · 🔒 auth required
Verify current password and email a confirmation link.

```json
// request
{ "currentPassword": "DummyPw123!" }
```

```json
// 202 Accepted
{ "message": "A confirmation link has been sent." }
```

Errors: `400` wrong password/validation · `401` · `429`.

### POST `/api/auth/change-password/confirm`  · public (token-based)
Complete password change with the emailed token.

```json
// request
{
  "token": "change-token",
  "newPassword": "NewDummyPw123!",
  "confirmNewPassword": "NewDummyPw123!"
}
```

Success: `204 No Content`.

Errors: `400` invalid/expired token or weak password · `429`.

---

### POST `/api/auth/change-email/request`  · 🔒 auth required
Verify current password and email a confirmation link to the current address.

```json
// request
{ "currentPassword": "DummyPw123!" }
```

```json
// 202 Accepted
{ "message": "A confirmation link has been sent to your current email." }
```

Errors: `400` wrong password/validation · `401` · `429`.

### GET `/api/auth/change-email/validate?token=<token>`  · public
Check whether an email-change token is valid before showing the form.

```json
// 200 OK
{ "isValid": true, "message": "Token is valid." }
```

Errors: `400` invalid/missing token · `429`.

### POST `/api/auth/change-email/confirm`  · public (token-based)
Complete email change with the emailed token.

```json
// request
{ "token": "change-token", "newEmail": "new@example.com" }
```

Success: `204 No Content`.

Errors: `400` · `409` email already in use · `429`.

---

### POST `/api/auth/verify-email`  · public
Verify a user's email address.

```json
// request
{ "token": "verification-token" }
```

Success: `204 No Content`.

Errors: `400` invalid/expired token · `429`.

### POST `/api/auth/resend-verification`  · public
Request a new verification email.

```json
// request
{ "email": "user@example.com" }
```

```json
// 202 Accepted
{ "message": "If the account exists and is unverified, a new link has been sent." }
```

Errors: `400` · `429`.

---

## 5. MFA — `/api/mfa`

TOTP-based MFA with recovery codes. Gateway lets `/api/mfa/**` through, but the
Security API requires auth for setup/disable/recovery-code routes.

### POST `/api/mfa/setup/begin`  · 🔒 auth required
Start authenticator-app setup. No request body.

```json
// 200 OK
{
  "manualEntryKey": "JBSWY3DPEHPK3PXP",
  "otpAuthUri": "otpauth://totp/Kocluk:user@example.com?secret=..."
}
```

Render `otpAuthUri` as a QR code; show `manualEntryKey` as fallback.

Errors: `401` · `429`.

### POST `/api/mfa/setup/complete`  · 🔒 auth required
Confirm setup with a code from the authenticator app; returns one-time recovery
codes.

```json
// request
{ "code": "123456" }
```

```json
// 200 OK
{ "recoveryCodes": ["ab12-cd34", "ef56-gh78"] }
```

Errors: `400` wrong code/validation · `401` · `429`.

### POST `/api/mfa/login/complete`  · public (challenge-based)
Finish an MFA login. Supply **either** `totpCode` **or** `recoveryCode`.

```json
// request
{
  "challengeToken": "opaque-token",
  "totpCode": "123456",
  "recoveryCode": null
}
```

```json
// 200 OK — same shape as successful login
{
  "user": { "...": "..." },
  "tokens": { "...": "..." },
  "mfaChallenge": null,
  "requiresMfa": false
}
```

Errors: `400` · `401` invalid/expired challenge or code · `429`.

### POST `/api/mfa/disable`  · 🔒 auth required
Supply `totpCode` or `recoveryCode` to turn MFA off.

```json
// request
{ "totpCode": "123456", "recoveryCode": null }
```

Success: `204 No Content`.

Errors: `400` · `401` · `429`.

### POST `/api/mfa/recovery-codes/regenerate`  · 🔒 auth required
Regenerate recovery codes after confirming with TOTP.

```json
// request
{ "totpCode": "123456" }
```

```json
// 200 OK
{ "recoveryCodes": ["ab12-cd34", "ef56-gh78"] }
```

Errors: `400` · `401` · `429`.

---

## 6. Users — `/api/users`

All routes require authentication.

### GET `/api/users/me`  · 🔒 auth required
Current user profile + resolved permission claims from the access token.

```json
// 200 OK
{
  "userId": "guid",
  "email": "user@example.com",
  "firstName": "Ada",
  "lastName": "Lovelace",
  "studentType": "HighSchool",
  "emailVerified": true,
  "sessionId": "guid",
  "accessTokenJti": "guid",
  "permissions": ["users.read", "roles.read"]
}
```

Errors: `401` · `404`.

### PUT `/api/users/me/update`  · 🔒 auth required
Update the current user's profile. Send at least one field; omitted fields stay
unchanged.

```json
// request
{
  "firstName": "Ada",
  "lastName": "Byron",
  "studentType": "University"
}
```

```json
// 200 OK — CurrentUserResponse
{
  "userId": "guid",
  "email": "user@example.com",
  "firstName": "Ada",
  "lastName": "Byron",
  "studentType": "University",
  "emailVerified": true,
  "sessionId": "guid",
  "accessTokenJti": "guid",
  "permissions": ["users.read", "roles.read"]
}
```

Errors: `400` validation · `401` · `404`.

---

## 7. Memberships — `/api/memberships`

All routes require authentication and use the Security Admin rate-limit bucket.
Membership actions are tenant-scoped. A platform system admin can act in any tenant;
otherwise the caller must have the required permission through an active membership
in the target tenant.

### GET `/api/memberships/me`  · 🔒 auth required
List the current user's memberships, including history.

```json
// 200 OK
[
  {
    "id": "guid",
    "userId": "guid",
    "tenantId": "guid",
    "status": "Active",
    "roles": ["Member"],
    "joinedAtUtc": "2026-07-25T12:00:00Z",
    "updatedAtUtc": "2026-07-25T12:00:00Z"
  }
]
```

Errors: `401` · `429`.

### GET `/api/memberships?tenantId=<tenantId>`  · 🔒 auth + tenant `members.read`
List members of a tenant.

```json
// 200 OK
[
  {
    "userId": "guid",
    "email": "user@example.com",
    "firstName": "Ada",
    "lastName": "Lovelace",
    "status": "Active",
    "roles": ["Member"],
    "joinedAtUtc": "2026-07-25T12:00:00Z",
    "updatedAtUtc": "2026-07-25T12:00:00Z"
  }
]
```

Errors: `400` missing/invalid `tenantId` · `401` · `403` · `429`.

### POST `/api/memberships`  · 🔒 auth + tenant `members.manage`
Add an existing user to a tenant. Provide either `userId` or `email`. `roleIds` is
optional.

```json
// request
{
  "tenantId": "guid",
  "userId": "guid",
  "email": null,
  "roleIds": ["guid"]
}
```

```json
// 201 Created
{
  "id": "guid",
  "userId": "guid",
  "tenantId": "guid",
  "status": "Active",
  "roles": ["Coach"],
  "joinedAtUtc": "2026-07-25T12:00:00Z",
  "updatedAtUtc": "2026-07-25T12:00:00Z"
}
```

Errors: `400` validation · `401` · `403` · `404` user/tenant not found · `409` already active
or invalid membership state · `429`.

### DELETE `/api/memberships/{userId}?tenantId=<tenantId>`  · 🔒 auth + tenant `members.manage`
Remove a member from a tenant. This is soft removal; history is kept.

Success: `204 No Content`.

Errors: `400` missing/invalid `tenantId` · `401` · `403` · `404` · `429`.

### POST `/api/memberships/{userId}/suspend?tenantId=<tenantId>`  · 🔒 auth + tenant `members.manage`
Suspend a member in a tenant.

Success: `204 No Content`.

Errors: `400` missing/invalid `tenantId` · `401` · `403` · `404` · `429`.

### POST `/api/memberships/{userId}/reactivate?tenantId=<tenantId>`  · 🔒 auth + tenant `members.manage`
Reactivate a suspended or removed member in a tenant.

Success: `204 No Content`.

Errors: `400` missing/invalid `tenantId` · `401` · `403` · `404` · `429`.

### POST `/api/memberships/{userId}/roles?tenantId=<tenantId>`  · 🔒 auth + tenant `members.manage`
Grant a tenant role to a member.

```json
// request
{ "roleId": "guid" }
```

Success: `204 No Content`.

Errors: `400` missing/invalid `tenantId` · `401` · `403` · `404` · `429`.

### DELETE `/api/memberships/{userId}/roles/{roleId}?tenantId=<tenantId>`  · 🔒 auth + tenant `members.manage`
Remove a tenant role from a member.

Success: `204 No Content`.

Errors: `400` missing/invalid `tenantId` · `401` · `403` · `404` · `429`.

---

## 8. Sessions — `/api/sessions`

All routes require authentication.

### GET `/api/sessions`  · 🔒 auth required
List the current user's sessions.

```json
// 200 OK
[
  {
    "sessionId": "guid",
    "deviceName": "Chrome on macOS",
    "ipAddress": "203.0.113.4",
    "createdAtUtc": "2026-07-25T12:00:00Z",
    "revoked": false,
    "revokedAtUtc": null,
    "isCurrent": true
  }
]
```

Errors: `401` · `429`.

### DELETE `/api/sessions/{id}`  · 🔒 auth required
Revoke one of the current user's sessions.

Success: `204 No Content`.

Errors: `400` · `401` · `404` · `429`.

---

## 9. Roles — `/api/roles`

All routes require authentication and use the Security Admin rate-limit bucket.
Tenant roles are scoped by `tenantId`. Platform roles can only be changed by a
platform system admin; normal frontend role management should use tenant roles.

### GET `/api/roles?tenantId=<tenantId>`  · 🔒 auth + tenant `roles.read`
List roles in a tenant.

```json
// 200 OK
[
  {
    "id": "guid",
    "name": "Admin",
    "permissions": [
      { "id": "guid", "code": "users.read" },
      { "id": "guid", "code": "members.manage" }
    ]
  }
]
```

Errors: `400` missing/invalid `tenantId` · `401` · `403` · `429`.

### GET `/api/roles/{id}`  · 🔒 auth + role access
Get one role by id. Tenant roles require `roles.read` in that role's tenant; platform
roles require system admin.

```json
// 200 OK
{
  "id": "guid",
  "name": "Admin",
  "permissions": [{ "id": "guid", "code": "roles.manage" }]
}
```

Errors: `401` · `403` · `404` · `429`.

### POST `/api/roles`  · 🔒 auth + tenant `roles.manage`
Create a tenant role.

```json
// request
{ "tenantId": "guid", "name": "Coach" }
```

```json
// 201 Created
{ "id": "guid", "name": "Coach", "permissions": [] }
```

Errors: `400` · `401` · `403` · `409` name taken · `429`.

### PUT `/api/roles/{id}`  · 🔒 auth + role access
Rename a role. Tenant roles require `roles.manage` in that role's tenant; platform
roles require system admin.

```json
// request
{ "name": "Lead Coach" }
```

Success: `204 No Content`.

Errors: `400` · `401` · `403` · `404` · `409` · `429`.

### DELETE `/api/roles/{id}`  · 🔒 auth + role access
Delete a role and revoke affected users' access tokens.

Success: `204 No Content`.

Errors: `401` · `403` · `404` · `429`.

### POST `/api/roles/{id}/permissions`  · 🔒 auth + role access
Add a permission to a role and revoke affected users' access tokens.

```json
// request
{ "permissionId": "guid" }
```

Success: `204 No Content`.

Errors: `400` · `401` · `403` · `404` · `429`.

### DELETE `/api/roles/{id}/permissions/{permissionId}`  · 🔒 auth + role access
Remove a permission from a role and revoke affected users' access tokens.

Success: `204 No Content`.

Errors: `401` · `403` · `404` · `429`.

---

## 10. Permissions — `/api/permissions`

All routes require authentication and use the Security Admin rate-limit bucket.

### GET `/api/permissions`  · 🔒 auth + `permissions.read`
List the full permission catalogue.

```json
// 200 OK
[
  { "id": "guid", "code": "users.read" },
  { "id": "guid", "code": "roles.manage" }
]
```

Errors: `401` · `403` · `429`.

### POST `/api/permissions`  · 🔒 auth + `permissions.manage`
Create a new permission code.

```json
// request
{ "code": "reports.read" }
```

```json
// 201 Created
{ "id": "guid", "code": "reports.read" }
```

Errors: `400` · `401` · `403` · `409` code taken · `429`.

### DELETE `/api/permissions/{id}`  · 🔒 auth + `permissions.manage`
Delete a permission that is not assigned to any role.

Success: `204 No Content`.

Errors: `401` · `403` · `404` · `409` still assigned to a role · `429`.

---

## 11. Tenants — `/api/tenants`

Served by the Tenant service. The gateway requires authentication for `/api/tenants/**`,
and the Tenant service enforces `tenants.read` / `tenants.manage`.

### GET `/api/tenants`  · 🔒 auth + `tenants.read`
List all tenants.

```json
// 200 OK
[
  {
    "id": "guid",
    "name": "Acme Inc.",
    "slug": "acme-inc",
    "isActive": true,
    "createdAtUtc": "2026-07-25T12:00:00Z",
    "updatedAtUtc": "2026-07-25T12:00:00Z"
  }
]
```

Errors: `401` · `403`.

### GET `/api/tenants/{id}`  · 🔒 auth + `tenants.read`
Get a tenant by id.

```json
// 200 OK
{
  "id": "guid",
  "name": "Acme Inc.",
  "slug": "acme-inc",
  "isActive": true,
  "createdAtUtc": "2026-07-25T12:00:00Z",
  "updatedAtUtc": "2026-07-25T12:00:00Z"
}
```

Errors: `401` · `403` · `404`.

### POST `/api/tenants`  · 🔒 auth + `tenants.manage`
Create a tenant. `slug` is optional and derived from `name` when omitted.

```json
// request
{ "name": "Acme Inc.", "slug": "acme-inc" }
```

```json
// 201 Created — TenantResponse
{
  "id": "guid",
  "name": "Acme Inc.",
  "slug": "acme-inc",
  "isActive": true,
  "createdAtUtc": "2026-07-25T12:00:00Z",
  "updatedAtUtc": "2026-07-25T12:00:00Z"
}
```

Errors: `400` validation · `401` · `403` · `409` slug already exists.

### PUT `/api/tenants/{id}`  · 🔒 auth + `tenants.manage`
Rename a tenant.

```json
// request
{ "name": "Acme International" }
```

Success: `204 No Content`.

Errors: `400` · `401` · `403` · `404`.

### POST `/api/tenants/{id}/activate`  · 🔒 auth + `tenants.manage`
Activate a tenant.

Success: `204 No Content`.

Errors: `401` · `403` · `404`.

### POST `/api/tenants/{id}/deactivate`  · 🔒 auth + `tenants.manage`
Deactivate a tenant.

Success: `204 No Content`.

Errors: `401` · `403` · `404`.

### DELETE `/api/tenants/{id}`  · 🔒 auth + `tenants.manage`
Delete a tenant.

Success: `204 No Content`.

Errors: `401` · `403` · `404`.

---

## 12. Test & system routes

These are useful for local/debug checks. Do not build product UI around `/api/test/**`.

### GET `/api/test/authenticated`  · 🔒 auth required

```json
// 200 OK
{ "message": "You are authenticated." }
```

Errors: `401`.

### GET `/api/test/users-read`  · 🔒 auth + `users.read`

```json
// 200 OK
{ "message": "You have users.read permission." }
```

Errors: `401` · `403`.

### GET `/`
Gateway/service identity endpoint. Through the gateway it returns:

```json
// 200 OK
{ "service": "ApiGateway", "status": "running" }
```

### GET `/health/live`
Gateway liveness probe.

### GET `/health/ready`
Gateway readiness probe.

---

## 13. Quick reference — all gateway routes

| Method | Path | Auth | Permission / tenant rule |
|--------|------|------|--------------------------|
| POST | `/api/auth/register` | — | — |
| POST | `/api/auth/login` | — | — |
| POST | `/api/auth/refresh` | — | — |
| POST | `/api/auth/logout` | 🔒 | — |
| POST | `/api/auth/logout-all` | 🔒 | — |
| POST | `/api/auth/forgot-password` | — | — |
| POST | `/api/auth/reset-password` | — | — |
| POST | `/api/auth/change-password/request` | 🔒 | — |
| POST | `/api/auth/change-password/confirm` | — | — |
| POST | `/api/auth/change-email/request` | 🔒 | — |
| GET | `/api/auth/change-email/validate?token=<token>` | — | — |
| POST | `/api/auth/change-email/confirm` | — | — |
| POST | `/api/auth/verify-email` | — | — |
| POST | `/api/auth/resend-verification` | — | — |
| POST | `/api/mfa/setup/begin` | 🔒 | — |
| POST | `/api/mfa/setup/complete` | 🔒 | — |
| POST | `/api/mfa/login/complete` | — | — |
| POST | `/api/mfa/disable` | 🔒 | — |
| POST | `/api/mfa/recovery-codes/regenerate` | 🔒 | — |
| GET | `/api/users/me` | 🔒 | — |
| PUT | `/api/users/me/update` | 🔒 | — |
| GET | `/api/memberships/me` | 🔒 | — |
| GET | `/api/memberships?tenantId=<tenantId>` | 🔒 | tenant `members.read` |
| POST | `/api/memberships` | 🔒 | tenant `members.manage` |
| DELETE | `/api/memberships/{userId}?tenantId=<tenantId>` | 🔒 | tenant `members.manage` |
| POST | `/api/memberships/{userId}/suspend?tenantId=<tenantId>` | 🔒 | tenant `members.manage` |
| POST | `/api/memberships/{userId}/reactivate?tenantId=<tenantId>` | 🔒 | tenant `members.manage` |
| POST | `/api/memberships/{userId}/roles?tenantId=<tenantId>` | 🔒 | tenant `members.manage` |
| DELETE | `/api/memberships/{userId}/roles/{roleId}?tenantId=<tenantId>` | 🔒 | tenant `members.manage` |
| GET | `/api/sessions` | 🔒 | — |
| DELETE | `/api/sessions/{id}` | 🔒 | — |
| GET | `/api/roles?tenantId=<tenantId>` | 🔒 | tenant `roles.read` |
| GET | `/api/roles/{id}` | 🔒 | tenant `roles.read` or system admin for platform role |
| POST | `/api/roles` | 🔒 | tenant `roles.manage` |
| PUT | `/api/roles/{id}` | 🔒 | tenant `roles.manage` or system admin for platform role |
| DELETE | `/api/roles/{id}` | 🔒 | tenant `roles.manage` or system admin for platform role |
| POST | `/api/roles/{id}/permissions` | 🔒 | tenant `roles.manage` or system admin for platform role |
| DELETE | `/api/roles/{id}/permissions/{permissionId}` | 🔒 | tenant `roles.manage` or system admin for platform role |
| GET | `/api/permissions` | 🔒 | `permissions.read` |
| POST | `/api/permissions` | 🔒 | `permissions.manage` |
| DELETE | `/api/permissions/{id}` | 🔒 | `permissions.manage` |
| GET | `/api/tenants` | 🔒 | `tenants.read` |
| GET | `/api/tenants/{id}` | 🔒 | `tenants.read` |
| POST | `/api/tenants` | 🔒 | `tenants.manage` |
| PUT | `/api/tenants/{id}` | 🔒 | `tenants.manage` |
| POST | `/api/tenants/{id}/activate` | 🔒 | `tenants.manage` |
| POST | `/api/tenants/{id}/deactivate` | 🔒 | `tenants.manage` |
| DELETE | `/api/tenants/{id}` | 🔒 | `tenants.manage` |
| GET | `/api/test/authenticated` | 🔒 | — |
| GET | `/api/test/users-read` | 🔒 | `users.read` |
| GET | `/` | — | gateway status |
| GET | `/health/live` | — | gateway liveness |
| GET | `/health/ready` | — | gateway readiness |

🔒 = requires `Authorization: Bearer <accessToken>`.
