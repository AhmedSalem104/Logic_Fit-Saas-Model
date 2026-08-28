# Phase 6 Platform Settings and Feature Flags Contract

**Status:** GREEN — scope explicitly bounded; runtime implementation deferred

## Platform settings

No `platform.settings` table, generic dynamic-settings framework, setting
keys, settings API, or settings UI is part of Phase 6. Deployment/environment
configuration remains ASP.NET Core configuration and is not a Platform Admin
data surface.

Commercial, Gym-operational, finance, currency, tax, branding, and payment
settings remain owned by their approved future scope.

## Feature flags

The existing `platform.feature_flags` table is retained as the single
approved boundary. It represents logical Platform/Gym scope and never grants
permissions. No Phase 6 flag keys or flag state records are created.

The following are intentionally not implemented because no approved Phase 6
key registry or permission exists:

- GET/PATCH feature-flag API;
- flag mutation UI;
- cache invalidation;
- emergency-disable workflow;
- arbitrary JSON settings.

The existing decision that flags are Control Plane metadata remains intact.
Any future flag contract must define key/schema/version, scope, precedence,
validation, and an explicitly approved permission before implementation.

## Client scope

`PA-W-008` is not a Phase 6 screen. There is no Flutter settings/flag screen.
