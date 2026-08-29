# API Spec — Authentication

> Roles: Public (no auth required unless noted)

---

## POST /auth/login

Authenticates an internal user (Admin, Manager, Agent) or customer portal user.

**Auth:** None

**Request:**
```json
{
  "email": "agent@company.com",
  "password": "SecurePass123!"
}
```

**Response 200:**
```json
{
  "data": {
    "accessToken": "eyJhbGci...",
    "expiresIn": 900,
    "user": {
      "id": "uuid",
      "firstName": "Ahmed",
      "lastName": "Al-Farsi",
      "email": "agent@company.com",
      "role": "Agent",
      "primaryDepartmentId": "uuid",
      "departmentIds": ["uuid"],
      "requiresPasswordChange": false
    }
  }
}
```

Refresh token set as `HttpOnly` cookie (`crm_refresh_token`, 7-day TTL).

**Errors:** `401` invalid credentials | `403` account deactivated

---

## POST /auth/refresh

Issues a new access token using the refresh token cookie.

**Auth:** None (reads `crm_refresh_token` cookie)

**Request:** *(no body)*

**Response 200:**
```json
{
  "data": {
    "accessToken": "eyJhbGci...",
    "expiresIn": 900
  }
}
```

**Errors:** `401` missing/expired/invalid refresh token

---

## POST /auth/logout

Revokes the refresh token.

**Auth:** Bearer token

**Request:** *(no body)*

**Response 200:**
```json
{ "data": { "message": "Logged out successfully." } }
```

---

## POST /auth/portal/register

Customer self-registration on the portal.

**Auth:** None

**Request:**
```json
{
  "fullName": "Sara Al-Mansouri",
  "email": "sara@example.com",
  "phone": "+966501234567",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}
```

**Response 201:**
```json
{
  "data": {
    "message": "Registration successful. Please check your email to verify your account."
  }
}
```

Sends verification email via Gmail SMTP. Customer account is inactive until email is verified.

**Errors:** `409` email already registered | `400` passwords don't match

---

## POST /auth/portal/verify-email

Verifies customer email from the link sent after registration.

**Auth:** None

**Request:**
```json
{ "token": "email-verification-token-from-link" }
```

**Response 200:**
```json
{ "data": { "message": "Email verified. You can now log in." } }
```

**Errors:** `400` token invalid or expired

---

## POST /auth/forgot-password

Sends a password reset link to the user's email.

**Auth:** None

**Request:**
```json
{ "email": "agent@company.com" }
```

**Response 200:**
```json
{ "data": { "message": "If that email exists, a reset link has been sent." } }
```

Always returns 200 (no email enumeration).

---

## POST /auth/reset-password

Resets password using the token from the reset email.

**Auth:** None

**Request:**
```json
{
  "token": "reset-token-from-email",
  "newPassword": "NewSecurePass123!",
  "confirmPassword": "NewSecurePass123!"
}
```

**Response 200:**
```json
{ "data": { "message": "Password reset successful." } }
```

**Errors:** `400` token invalid/expired | `400` passwords don't match

---

## GET /auth/me

Returns the currently authenticated user's profile.

**Auth:** Bearer token | **Roles:** `[Any]`

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "firstName": "Ahmed",
    "lastName": "Al-Farsi",
    "firstNameAr": "أحمد",
    "lastNameAr": "الفارسي",
    "jobTitle": "Senior Support Agent",
    "jobTitleAr": "موظف دعم أول",
    "email": "agent@company.com",
    "role": "Agent",
    "primaryDepartmentId": "uuid",
    "departmentIds": ["uuid"],
    "availabilityStatus": "Available",
    "requiresPasswordChange": false,
    "isActive": true
  }
}
```
