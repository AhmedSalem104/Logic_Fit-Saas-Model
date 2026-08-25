# Flutter Foundation

## Technology

`apps/mobile` is a real Flutter iOS/Android project using Dart, Riverpod, GoRouter, Dio, and Flutter localization.

## Implemented foundation

- `ProviderScope` bootstrap.
- GoRouter routes for foundation and diagnostics.
- Dio API client using the same REST health contract as Web.
- Request ID interceptor.
- Arabic locale, RTL Directionality, and localization delegates.
- Light/dark Material 3 themes.
- Loading, empty, error, card, button, field, dialog, and bottom-sheet foundation widgets.
- Android debug build configuration pinned to the locally installed NDK `27.0.12077973` so local builds do not require a paid/external service.
- Flutter widget test and analyzer configuration.

No business mobile screen was implemented.
