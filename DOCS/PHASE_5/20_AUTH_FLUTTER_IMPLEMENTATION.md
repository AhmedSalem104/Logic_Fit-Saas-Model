# Authentication/RBAC Flutter Implementation

**Status:** IMPLEMENTED — analyzer, widget tests, and Windows runtime launch passing

## Client boundary

Flutter uses `Dio` through `apps/mobile/lib/auth.dart`; it never connects to SQL Server and does not duplicate backend security decisions. `Riverpod` owns presentation/session state and `GoRouter` guards protected routes.

## Screens and routes

| Screen | Route | API use |
|---|---|---|
| Login/MFA | `/login` | login and combined TOTP/recovery verification |
| Password reset | `/password-reset` | canonical hyphenated request/complete routes |
| Authenticated shell | `/app` | session state and logout |
| Security | `/app/security` | session list/revoke, password change, TOTP, recovery codes |
| Platform access | none | Explicitly Web-only in the approved contract |

## UX/security behavior

The existing mobile theme and localization foundation supports Arabic/RTL and light/dark themes. Loading/error/validation states are surfaced without exposing secret values. Server responses control authentication and authorization state.

## Verification

`flutter analyze` reports no issues. `flutter test` passes the RTL shell and login-validation tests. API calls use the same routes and request shapes as Web, including `method: recovery_code` on the existing MFA verification route.

On 2026-08-26, `flutter run -d windows --debug` built and launched the application successfully on the local Windows device, synchronized the application, and exposed the Dart VM service. On 2026-08-27, the same app was built/installed and exercised on Android emulator `Medium_Phone_API_36.0` (`emulator-5554`, Android 16/API 36) for the available authentication/mobile scope, including login, invalid credentials, MFA/TOTP, recovery-code verification, password-reset request, session/security view, logout, API communication, Arabic/RTL, and light/dark themes. iOS interactive UAT is unavailable on this Windows workstation because `xcrun`/`simctl` and an iOS device are not available. Password-reset completion is covered by API tests only because the approved request contract never exposes a raw reset token.
