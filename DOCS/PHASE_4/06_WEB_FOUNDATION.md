# React Web Foundation

## Technology

React 19, TypeScript, Vite, Tailwind CSS, React Router, TanStack Query, React Hook Form, and Zod are installed in `apps/web`.

## Implemented foundation

- RTL-first HTML shell (`lang="ar"`, `dir="rtl"`).
- Light/dark theme tokens using CSS variables and Tailwind dark mode.
- Responsive App Shell with desktop sidebar and mobile Drawer.
- API client with shared request IDs and safe error mapping.
- TanStack Query provider.
- React ErrorBoundary with sanitized fallback.
- Foundation screen showing health/readiness only.
- Button, input, select, card, table, modal, drawer, loading, empty, error, and toast primitives.
- Vite build and Vitest/jsdom test setup.

No business route, business screen, or business API was implemented.
