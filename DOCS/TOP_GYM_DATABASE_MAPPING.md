# TOP GYM Database Mapping

**Audit date:** 2026-08-25  
**Engine observed:** Microsoft SQL Server via `mssql`.  
**Primary schema evidence:** `database/schema.sql`; migrations and runtime `ensure*` code are listed separately.

## Identity, auth, and permissions

| Table | Purpose observed | Evidence |
|---|---|---|
| `members` | Member identity/contact/registration and portal-code columns | `database/schema.sql`, `src/services/member-service.js` |
| `gym_membership_code_audit` | Portal code issue/revoke audit | `database/schema.sql` |
| `gym_users` | Owner/Assistant users | `database/schema.sql`, `src/services/auth-service.js` |
| `gym_auth_sessions` | Hashed session tokens, expiry/revocation | `database/schema.sql` |
| `gym_user_permissions` | Assistant permission overrides | `database/migrations/006-permissions.sql` |
| `gym_permission_audit` | Permission change audit | `database/migrations/006-permissions.sql` |
| `gym_member_feedback` | Portal feedback and admin review | `database/schema.sql`, `database/migrations/005-member-feedback.sql` |

## Membership, attendance, finance

| Table | Purpose observed |
|---|---|
| `memberships` | Member membership records, dates, plan/type, amounts/status inputs |
| `membership_pricing` | Plan pricing catalog |
| `membership_types` | Membership duration/type catalog |
| `membership_type_prices` | Type/plan price matrix |
| `membership_freezes` | Freeze periods and resumption |
| `gym_day_pass_types` | Day-pass pricing/types |
| `gym_day_pass_sales` | Day-pass sales and status |
| `gym_payments` | Membership payment summary/legacy payment rows |
| `gym_payment_transactions` | Payment ledger/receipts; also runtime-ensured by member service |
| `gym_expenses` | Gym expenses; store migration adds source/category/payment/void columns |
| `gym_attendance` | Check-in/out records and source |
| `membership_events` | Membership lifecycle event log |
|

Evidence for this group: `database/schema.sql`, `database/migrations/007-store.sql`, `src/services/member-service.js`, `src/services/attendance-service.js`.

## Libraries and coaching

| Table | Purpose observed |
|---|---|
| `gym_muscles` | Muscle catalog, body part, bilingual labels/metadata |
| `gym_foods` | Food catalog and nutrient values |
| `gym_exercises` | Exercise catalog, target muscle, secondary metadata, instructions/media metadata |
| `workout_programs` | Program/member/goal/level/duration/status/version |
| `workout_routines` | Program day/routine order and name |
| `workout_exercises` | Exercise configuration inside a routine |
| `diet_plans` | Diet/member/targets/dates/status/version |
| `diet_meals` | Ordered meals and timing/notes |
| `diet_meal_items` | Food assignments and nutrition snapshots |
| `body_measurements` | Exact member measurement history |
| `workout_sessions` | Execution sessions |
| `workout_set_logs` | Per-set execution logs |
| `meal_logs` | Consumed meal item snapshots |

## Operations and store

| Table(s) | Purpose observed |
|---|---|
| `gym_backup_operations`, `gym_backup_archives` | Custom backup/inspect/restore history and archive content |
| `gym_alert_communications` | Alert/manual WhatsApp contact state |
| `gym_store_categories` | Store category |
| `gym_store_suppliers` | Supplier |
| `gym_store_products`, `gym_store_product_variants` | Product/SKU/barcode/variant |
| `gym_store_customers` | Store customer lookup |
| `gym_store_purchases`, `gym_store_purchase_items`, `gym_store_purchase_payments` | Purchase documents and payments |
| `gym_store_inventory_balances`, `gym_store_inventory_batches`, `gym_store_stock_movements` | Inventory state/history |
| `gym_store_sales`, `gym_store_sale_items`, `gym_store_sale_payments` | POS sales and payments |
| `gym_store_returns`, `gym_store_return_items` | Returns and reversal records |
| `gym_store_audit_log` | Store audit events |

Evidence: `database/migrations/007-store.sql`, `src/services/store-service.js`.

## Runtime/schema discrepancies

`src/services/coaching-service.js` adds or ensures runtime structures/columns not fully represented in `database/schema.sql`:

- `workout_exercises.rir`, `workout_exercises.rpe`.
- Diet calculator columns: `calorie_goal`, `calorie_adjustment`, `calculator_weight_kg`, `calculator_height_cm`, `calculator_age`, `calculator_gender`, `calculator_activity`, `bmr`, `tdee`.
- `athlete_checkins`.
- `coaching_activity_events`.

Other services also have compatibility DDL for library, payments, attendance, and membership pricing. This is not a harmless documentation difference: it affects migration order, constraints, and API persistence.

Audit-time status superseded by TOP_GYM_LOGICFIT_DATABASE_DECISION.md: runtime SQL Server is authoritative for current TOP GYM state; schema.sql remains legacy evidence; LogicFit uses a separate canonical mapping.
## Data integrity observations

- SQL Server constraints/FKs/checks are present in the legacy schema.
- The library has no separate category/unit/conversion tables; several taxonomies are stored as text.
- TOP GYM library seed IDs for muscles/foods are derived from array position; this is not a stable LogicFit `seed_key` contract.
- Coaching updates rebuild child rows, and no immutable published snapshot table was found.
- No Control Plane or per-gym database metadata tables were found in this single-gym legacy schema.

Source-code/schema mapping was supplemented by a separate dated read-only metadata snapshot in TOP_GYM_LIVE_DB_READONLY_SNAPSHOT.md; no DDL/DML/migration/seed was executed. The mapping itself remains source evidence, not a replacement for the dated runtime snapshot.

## Source Consolidation Resolution - 2026-08-25

The audit-time schema conflict is resolved by TOP_GYM_LOGICFIT_DATABASE_DECISION.md: runtime SQL Server is authoritative for current TOP GYM state; database/schema.sql is legacy/documentation evidence; neither is copied blindly into LogicFit.


