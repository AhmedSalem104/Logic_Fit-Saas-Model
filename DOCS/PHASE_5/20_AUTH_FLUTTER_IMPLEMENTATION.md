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

On 2026-08-26, `flutter run -d windows --debug` built and launched the application successfully on the local Windows device, synchronized the application, and exposed the Dart VM service. The launch was then stopped cleanly. No Android/iOS emulator was available on this workstation; Windows runtime launch plus analyzer/widget coverage are the available local Flutter evidence for this checkpoint.
