# TOP GYM Screen Inventory

**Audit date:** 2026-08-25  
**Platform observed:** Web only; Vanilla JS/HTML/CSS.  
**Identifier note:** `TG-SCR-*` values below are audit identifiers, not legacy database or route IDs.

## Primary screens

| Audit ID | Screen/route | Role evidence | Purpose | Main evidence |
|---|---|---|---|---|
| TG-SCR-001 | `/` login | Public | Login and session bootstrap | `public/index.html`, `public/js/auth-ui.js` |
| TG-SCR-002 | `#dashboard` | Owner/Assistant by permission | KPIs, alerts, finance/day-pass/store summaries | `public/index.html`, `public/js/page-tabs.js` |
| TG-SCR-003 | `#members` | `members.read` | Search, filters, pagination, member list | `public/index.html`, `public/js/app.js` |
| TG-SCR-004 | Member create/edit dialog | `members.create/update` | Member, membership, pricing, initial payment | `public/index.html`, `public/js/app.js` |
| TG-SCR-005 | Member action dialog | Membership/payment permissions | Freeze, resume, renew, payment | `public/js/app.js` |
| TG-SCR-006 | Member details dialog | `members.read` | Memberships, freezes, events, payments, coaching extension | `public/index.html`, `public/js/app.js`, `public/js/member-details-ui.js` |
| TG-SCR-007 | Finance/expenses | Finance permissions | Expenses and monthly financial summary | `public/js/pages/finance/monthly-finance.js` |
| TG-SCR-008 | Attendance | Attendance permissions | Phone/QR check-in, checkout, day view, report | `public/js/pages/attendance/attendance.js` |
| TG-SCR-009 | Day passes | Day-pass permissions | Pricing, create/edit, void, WhatsApp-opened tracking | `public/js/enhancements/day-passes.js` |
| TG-SCR-010 | Library | Library permissions | Muscles, foods, exercises catalog and CRUD | `public/js/pages/library/library.js` |
| TG-SCR-011 | Trainees/coaching profile | Coaching read | External trainees and training/nutrition overview | `public/js/pages/coaching/coaching.js` |
| TG-SCR-012 | Training builder | Coaching create/update | Multi-step workout draft, structure, review, save | `public/js/pages/coaching/coaching.js`, `public/index.html` |
| TG-SCR-013 | Nutrition builder | Coaching create/update | Multi-step diet draft, meals, foods, totals, save | `public/js/pages/coaching/coaching.js` |
| TG-SCR-014 | Intelligence | Intelligence read/generate | Query, churn, workout/diet suggestions, refine | `public/js/pages/intelligence/intelligence.js` |
| TG-SCR-015 | Reports | Reports permissions | Attendance, membership, finance, coaching, library reports | `public/js/pages/reports/reports.js` |
| TG-SCR-016 | Store/POS | Store permissions | Products, cart, sale, receipt, purchases, inventory | `public/js/pages/store/store.js` |
| TG-SCR-017 | Feedback administration | Owner/feedback read | Review member feedback | `public/js/pages/management/member-feedback.js` |
| TG-SCR-018 | Permissions | Owner | Manage Assistant permissions and reason | `public/js/pages/management/permissions.js` |
| TG-SCR-019 | Auth users | Owner | Create/update/disable/delete Assistant users | `public/js/pages/management/auth-users.js` |
| TG-SCR-020 | Backup/restore | Owner | Download, inspect, history, restore | `public/js/pages/management/backup.js` |
| TG-SCR-021 | Smart Assistant | Permission-dependent | In-app assistant surface | `public/js/enhancements/smart-assistant.js` |
| TG-SCR-022 | `/member-portal` | Membership code | Member report, attendance, payments, feedback, library | `public/member-portal.html`, `public/js/member-portal.js` |
| TG-SCR-023 | Portal exercise/food library | Portal member | Search, filters, details, pagination | `public/js/member-portal-library.js` |
| TG-SCR-024 | `/qr/:id` | Public/behavior boundary unknown | QR/member summary rendering | `server.js` |

## Important dialogs and action surfaces

Member form, member details, member action, pricing, membership types, coaching profile, training builder, nutrition builder, measurement, check-in, workout session, meal log, subscription, product/category/supplier/purchase/sale/return/expense dialogs, user/permission/backup dialogs, feedback dialog, and QR dialog are present in the HTML or lazy feature modules.

## UI state evidence

Loading rows/skeletons, empty states, retry/error states, toast/SweetAlert fallbacks, disabled submit/actions, permission-driven visibility, responsive table wrappers, RTL rendering, and print-specific states were observed. State coverage is not uniform: Store and some dynamic modules use toast-only errors.

## Platform gap

There is no native mobile screen inventory in the legacy source. Responsive Web evidence must not be converted into Flutter requirements without an approved LogicFit screen contract.
