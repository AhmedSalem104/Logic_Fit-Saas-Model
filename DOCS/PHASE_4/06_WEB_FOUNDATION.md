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

## Direct browser verification — 2026-08-26

An isolated installed Google Chrome session (`Chrome/151.0.7922.170`) opened `http://localhost:5173/` directly. The React app mounted successfully, rendered the App Shell, and reached the ASP.NET Core API at `http://127.0.0.1:5199` from the approved Web origin `http://localhost:5173`.

- `dir=rtl` and `lang=ar` were present.
- Arabic content rendered.
- Light and dark themes both rendered and persisted after the theme control was exercised.
- Responsive emulation at 390×844 hid the desktop sidebar, exposed the mobile menu, opened the drawer, and had no horizontal overflow.
- Browser fetches to `/api/v1/health`, `/api/v1/readiness`, and `/api/v1/version` returned HTTP 200 with successful CORS responses.
- The page produced no JavaScript exceptions or application console errors.

The reported `Cannot redefine property: process` does not originate from LogicFit. It is emitted by the external browser-control adapter during bootstrap at `browser-client.mjs:33` (`globalThis.process = processShim`), before a LogicFit tab is loaded. Direct Chrome verification did not reproduce it. This is classified as an **EXTERNAL BROWSER CONTROL ADAPTER LIMITATION**; no Web code or dependency workaround is required.

Chrome also requested `/favicon.ico`, which returned 404 because the Web foundation does not declare a favicon. This is a non-functional static-asset observation and is unrelated to the `process` error; it was not changed in this root-cause verification.
