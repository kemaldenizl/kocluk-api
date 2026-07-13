# Frontend Integration Guide — Kocluk API

Everything the frontend needs to talk to the backend. All traffic goes through the
**API Gateway**; you never call the individual services directly.

- **Base URL (local Docker):** `http://localhost:8080`
- **Content type:** `application/json` for every request/response body
- **JSON casing:** all fields are **camelCase** (e.g. `accessToken`, `emailVerified`)

> Routes are grouped by area below. For each route you get: method + path, whether it
> needs authentication, any required permission, the request body, the success
> response, and the notable error statuses.

---

## 1. Authentication model

Auth is JWT bearer. The flow:

1. `POST /api/auth/register` → creates the account.
2. `POST /api/auth/login` → returns an **access token** (short-lived, ~15 min) and a
   **refresh token**. _If the user has MFA enabled_, login instead returns an MFA
   challenge (see [MFA](#4-mfa--apimfa)).
3. Send the access token on every protected call:
   ```http
   Authorization: Bearer <accessToken>
   ```
4. When the access token expires, call `POST /api/auth/refresh` with the refresh token
   to get a new pair (the old refresh token is rotated/invalidated).
5. `POST /api/auth/logout` revokes the current session; `logout-all` revokes every
   session for the user.

### Multi-tenancy header

The Security service is multi-tenant. Send the tenant id on requests:

```http
X-Tenant-Id: 00000000-0000-0000-0000-000000000001
```

`00000000-0000-0000-0000-000000000001` is the default/dev tenant. After login the
tenant is also embedded in the access token; keep sending the header for
unauthenticated calls (register/login/forgot-password) so they resolve to the right
tenant.

### Token payload (access token claims)

The access token carries `sub` (user id), `email`, `jti`, `sid` (session id), tenant
id, and the user's resolved **permission** claims. The frontend can decode it for
display, but authorization is always enforced server-side.

---

## 2. Conventions

### Errors — RFC 7807 `application/problem+json`

Every error (400/401/403/404/409/429/5xx) returns:

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
    "password": ["Password must be at least 8 characters."]
  }
}
```

### Rate limiting — 429

Security enforces **per-endpoint** rate limits. On exceeding a limit you get `429`
with a problem+json body. Limits (per 60s window, per client) include:

| Area                         | Limit / min |
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
| Admin (users/roles/perms)    | 30          |

Handle `429` by backing off and retrying.

### Auth-related status codes

- **401 Unauthorized** — missing/invalid/expired/revoked access token.
- **403 Forbidden** — authenticated, but missing the required permission.

---

## 3. Permissions catalogue

Admin endpoints require a permission claim on the user (granted via roles):

| Code                  | Grants                                   |
|-----------------------|------------------------------------------|
| `users.read`          | read user info                           |
| `users.manage`        | assign/remove user roles                 |
| `roles.read`          | list/read roles                          |
| `roles.manage`        | create/rename/delete roles, edit role permissions |
| `permissions.read`    | list permissions                         |
| `permissions.manage`  | create/delete permissions                |
| `sessions.read`       | (reserved) read sessions                 |
| `sessions.manage`     | (reserved) manage sessions               |

The seeded `admin@local` user has the full set in development.

---

## 4. Auth — `/api/auth`

### POST `/api/auth/register`  · public
Create a new account.
```json
// request
{ "email": "user@example.com", "password": "P@ssw0rd123" }
```
```json
// 201 Created
{ "user": { "id": "guid", "email": "user@example.com", "emailVerified": false, "isActive": true } }
```
Errors: `400` validation · `409` email already exists · `429`.

---

### POST `/api/auth/login`  · public
Authenticate. Returns tokens, **or** an MFA challenge when MFA is enabled.
```json
// request
{ "email": "user@example.com", "password": "P@ssw0rd123" }
```
```json
// 200 OK — no MFA
{
  "user": { "id": "guid", "email": "user@example.com", "emailVerified": true, "isActive": true },
  "tokens": {
    "accessToken": "eyJ...",
    "accessTokenExpiresAtUtc": "2026-07-14T12:15:00Z",
    "refreshToken": "base64...",
    "refreshTokenExpiresAtUtc": "2026-07-21T12:00:00Z"
  },
  "mfaChallenge": null,
  "requiresMfa": false
}
```
```json
// 200 OK — MFA required (no tokens yet)
{
  "user": { "id": "guid", "email": "user@example.com", "emailVerified": true, "isActive": true },
  "tokens": null,
  "mfaChallenge": { "challengeToken": "opaque-token", "expiresAtUtc": "2026-07-14T12:05:00Z" },
  "requiresMfa": true
}
```
When `requiresMfa` is `true`, call `POST /api/mfa/login/complete` with the
`challengeToken`. Errors: `400` · `401` invalid credentials · `403` inactive/blocked · `429`.

---

### POST `/api/auth/refresh`  · public
Rotate tokens using a valid refresh token.
```json
// request
{ "refreshToken": "base64..." }
```
```json
// 200 OK
{ "tokens": { "accessToken": "eyJ...", "accessTokenExpiresAtUtc": "...", "refreshToken": "...", "refreshTokenExpiresAtUtc": "..." } }
```
Errors: `400` · `401` invalid/expired/used refresh token · `429`.

---

### POST `/api/auth/logout`  · 🔒 auth required
Revoke the current session. No request body.
- **200/204:** `204 No Content`. Errors: `400` bad session context · `401` · `429`.

### POST `/api/auth/logout-all`  · 🔒 auth required
Revoke all sessions of the current user. No body. → `204`. Errors: `401` · `429`.

---

### POST `/api/auth/forgot-password`  · public
Start password reset (always `202` even if the email is unknown — no enumeration).
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
{ "token": "reset-token", "newPassword": "N3wP@ss123" }
```
→ `204`. Errors: `400` invalid/expired token or weak password · `429`.

---

### POST `/api/auth/change-password/request`  · 🔒 auth required
Verify current password and email a confirmation link.
```json
// request
{ "currentPassword": "P@ssw0rd123" }
```
```json
// 202 Accepted
{ "message": "A confirmation link has been sent." }
```
Errors: `400` wrong password · `401` · `429`.

### POST `/api/auth/change-password/confirm`  · public (token-based)
```json
// request
{ "token": "change-token", "newPassword": "N3wP@ss123" }
```
→ `204`. Errors: `400` · `429`.

---

### POST `/api/auth/change-email/request`  · 🔒 auth required
```json
// request
{ "currentPassword": "P@ssw0rd123" }
```
```json
// 202 Accepted
{ "message": "A confirmation link has been sent to your current email." }
```
Errors: `400` · `401` · `429`.

### GET `/api/auth/change-email/validate?token=<token>`  · public
Check whether an email-change token is still valid before showing the form.
```json
// 200 OK
{ "isValid": true, "message": "Token is valid." }
```
Errors: `400` · `429`.

### POST `/api/auth/change-email/confirm`  · public (token-based)
```json
// request
{ "token": "change-token", "newEmail": "new@example.com" }
```
→ `204`. Errors: `400` · `409` email already in use · `429`.

---

### POST `/api/auth/verify-email`  · public
```json
// request
{ "token": "verification-token" }
```
→ `204`. Errors: `400` · `429`.

### POST `/api/auth/resend-verification`  · public
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

TOTP-based (authenticator app) with recovery codes.

### POST `/api/mfa/setup/begin`  · 🔒 auth required
Start setup. No body.
```json
// 200 OK
{ "manualEntryKey": "JBSWY3DPEHPK3PXP", "otpAuthUri": "otpauth://totp/Kocluk:user@example.com?secret=..." }
```
Render `otpAuthUri` as a QR code; show `manualEntryKey` as fallback. Errors: `401`.

### POST `/api/mfa/setup/complete`  · 🔒 auth required
Confirm setup with a code from the app; returns one-time recovery codes.
```json
// request
{ "code": "123456" }
```
```json
// 200 OK
{ "recoveryCodes": ["ab12-cd34", "ef56-gh78", "..."] }
```
Errors: `400` wrong code · `401`.

### POST `/api/mfa/login/complete`  · public (challenge-based)
Finish an MFA login. Supply **either** `totpCode` **or** `recoveryCode`.
```json
// request
{ "challengeToken": "opaque-token", "totpCode": "123456", "recoveryCode": null }
```
```json
// 200 OK — same shape as a successful login
{ "user": { ... }, "tokens": { ... }, "mfaChallenge": null, "requiresMfa": false }
```
Errors: `400` · `401` invalid/expired challenge or code.

### POST `/api/mfa/disable`  · 🔒 auth required
Supply `totpCode` or `recoveryCode` to turn MFA off.
```json
// request
{ "totpCode": "123456", "recoveryCode": null }
```
→ `204`. Errors: `400` · `401`.

### POST `/api/mfa/recovery-codes/regenerate`  · 🔒 auth required
```json
// request
{ "totpCode": "123456" }
```
```json
// 200 OK
{ "recoveryCodes": ["...", "..."] }
```
Errors: `400` · `401`.

---

## 6. Users — `/api/users`

All routes require authentication.

### GET `/api/users/me`  · 🔒 auth required
Current user + resolved permissions (handy for building the UI's capability set).
```json
// 200 OK
{
  "userId": "guid",
  "email": "user@example.com",
  "sessionId": "guid",
  "accessTokenJti": "guid",
  "permissions": ["users.read", "roles.read"]
}
```
Errors: `401`.

### POST `/api/users/{userId}/roles`  · 🔒 auth + `users.manage`
Assign a role to a user (revokes that user's access tokens).
```json
// request
{ "roleId": "guid" }
```
→ `204`. Errors: `400` · `401` · `403` · `404` · `429`.

### DELETE `/api/users/{userId}/roles/{roleId}`  · 🔒 auth + `users.manage`
Remove a role from a user. → `204`. Errors: `401` · `403` · `404` · `429`.

---

## 7. Sessions — `/api/sessions`

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
    "createdAtUtc": "2026-07-14T12:00:00Z",
    "revoked": false,
    "revokedAtUtc": null,
    "isCurrent": true
  }
]
```
Errors: `401` · `429`.

### DELETE `/api/sessions/{id}`  · 🔒 auth required
Revoke one of your sessions. → `204`. Errors: `400` · `401` · `404` · `429`.

---

## 8. Roles — `/api/roles`

All routes require authentication (and are rate-limited under the Admin bucket).

### GET `/api/roles`  · 🔒 auth + `roles.read`
```json
// 200 OK
[
  { "id": "guid", "name": "Admin", "permissions": [ { "id": "guid", "code": "users.manage" } ] }
]
```
Errors: `401` · `403` · `429`.

### GET `/api/roles/{id}`  · 🔒 auth + `roles.read`
→ `200` single `RoleResponse`. Errors: `401` · `403` · `404` · `429`.

### POST `/api/roles`  · 🔒 auth + `roles.manage`
```json
// request
{ "name": "Editor" }
```
```json
// 201 Created
{ "id": "guid", "name": "Editor", "permissions": [] }
```
Errors: `400` · `401` · `403` · `409` name taken · `429`.

### PUT `/api/roles/{id}`  · 🔒 auth + `roles.manage`
```json
// request
{ "name": "New name" }
```
→ `204`. Errors: `400` · `401` · `403` · `404` · `409` · `429`.

### DELETE `/api/roles/{id}`  · 🔒 auth + `roles.manage`
→ `204` (revokes affected users' tokens). Errors: `401` · `403` · `404` · `429`.

### POST `/api/roles/{id}/permissions`  · 🔒 auth + `roles.manage`
```json
// request
{ "permissionId": "guid" }
```
→ `204`. Errors: `400` · `401` · `403` · `404` · `429`.

### DELETE `/api/roles/{id}/permissions/{permissionId}`  · 🔒 auth + `roles.manage`
→ `204`. Errors: `401` · `403` · `404` · `429`.

---

## 9. Permissions — `/api/permissions`

All routes require authentication (Admin rate-limit bucket).

### GET `/api/permissions`  · 🔒 auth + `permissions.read`
```json
// 200 OK
[ { "id": "guid", "code": "users.read" }, { "id": "guid", "code": "roles.manage" } ]
```
Errors: `401` · `403` · `429`.

### POST `/api/permissions`  · 🔒 auth + `permissions.manage`
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
→ `204`. Errors: `401` · `403` · `404` · `409` still assigned to a role · `429`.

---

## 10. Tenants — `/api/tenants`

Served by the Tenant service.

> ⚠️ **Authorization note:** these endpoints are **not currently protected** by
> authentication/permissions at the service level — the gateway forwards them as-is.
> Treat them as admin-only from the UI and restrict access accordingly; add a
> permission check server-side before exposing tenant management to end users.

### GET `/api/tenants`
```json
// 200 OK
[
  {
    "id": "guid",
    "name": "Acme Inc.",
    "slug": "acme-inc",
    "isActive": true,
    "createdAtUtc": "2026-07-14T12:00:00Z",
    "updatedAtUtc": "2026-07-14T12:00:00Z"
  }
]
```

### GET `/api/tenants/{id}`
→ `200` single `TenantResponse`. Errors: `404`.

### POST `/api/tenants`
`slug` is optional; it's derived from `name` when omitted.
```json
// request
{ "name": "Acme Inc.", "slug": "acme-inc" }
```
```json
// 201 Created — TenantResponse (see above)
```
Errors: `400` validation · `409` slug already exists.

### PUT `/api/tenants/{id}`
```json
// request
{ "name": "Acme International" }
```
→ `204`. Errors: `400` · `404`.

### POST `/api/tenants/{id}/activate`
→ `204`. Errors: `404`.

### POST `/api/tenants/{id}/deactivate`
→ `204`. Errors: `404`.

### DELETE `/api/tenants/{id}`
→ `204`. Errors: `404`.

---

## 11. Quick reference — all routes

| Method | Path | Auth | Permission |
|--------|------|------|------------|
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
| GET | `/api/auth/change-email/validate` | — | — |
| POST | `/api/auth/change-email/confirm` | — | — |
| POST | `/api/auth/verify-email` | — | — |
| POST | `/api/auth/resend-verification` | — | — |
| POST | `/api/mfa/setup/begin` | 🔒 | — |
| POST | `/api/mfa/setup/complete` | 🔒 | — |
| POST | `/api/mfa/login/complete` | — | — |
| POST | `/api/mfa/disable` | 🔒 | — |
| POST | `/api/mfa/recovery-codes/regenerate` | 🔒 | — |
| GET | `/api/users/me` | 🔒 | — |
| POST | `/api/users/{userId}/roles` | 🔒 | `users.manage` |
| DELETE | `/api/users/{userId}/roles/{roleId}` | 🔒 | `users.manage` |
| GET | `/api/sessions` | 🔒 | — |
| DELETE | `/api/sessions/{id}` | 🔒 | — |
| GET | `/api/roles` | 🔒 | `roles.read` |
| GET | `/api/roles/{id}` | 🔒 | `roles.read` |
| POST | `/api/roles` | 🔒 | `roles.manage` |
| PUT | `/api/roles/{id}` | 🔒 | `roles.manage` |
| DELETE | `/api/roles/{id}` | 🔒 | `roles.manage` |
| POST | `/api/roles/{id}/permissions` | 🔒 | `roles.manage` |
| DELETE | `/api/roles/{id}/permissions/{permissionId}` | 🔒 | `roles.manage` |
| GET | `/api/permissions` | 🔒 | `permissions.read` |
| POST | `/api/permissions` | 🔒 | `permissions.manage` |
| DELETE | `/api/permissions/{id}` | 🔒 | `permissions.manage` |
| GET | `/api/tenants` | — ⚠️ | — |
| GET | `/api/tenants/{id}` | — ⚠️ | — |
| POST | `/api/tenants` | — ⚠️ | — |
| PUT | `/api/tenants/{id}` | — ⚠️ | — |
| POST | `/api/tenants/{id}/activate` | — ⚠️ | — |
| POST | `/api/tenants/{id}/deactivate` | — ⚠️ | — |
| DELETE | `/api/tenants/{id}` | — ⚠️ | — |

🔒 = requires `Authorization: Bearer <accessToken>` · ⚠️ = unprotected server-side today; gate in the UI.
