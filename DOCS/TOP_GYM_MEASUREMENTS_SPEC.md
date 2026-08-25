# TOP GYM Measurements Specification â€” Observed Behavior

**Audit date:** 2026-08-25  
**Status:** YELLOW â€” exact legacy measurement inventory captured.

## Body measurements

The actual measurement set found in UI, service, and schema is:

| UI/API key | SQL column | Observed validation |
|---|---|---|
| `measuredAt` | `measured_at` | Required date. |
| `weightKg` | `weight_kg` | Optional metric; 0â€“1000. |
| `heightCm` | `height_cm` | Optional metric; 0â€“300. |
| `bodyFatPercent` | `body_fat_percent` | Optional metric; 0â€“100. |
| `chestCm` | `chest_cm` | Optional metric; 0â€“500. |
| `waistCm` | `waist_cm` | Optional metric; 0â€“500. |
| `hipsCm` | `hips_cm` | Optional metric; 0â€“500. |
| `armsCm` | `arms_cm` | Optional metric; 0â€“500. |
| `thighsCm` | `thighs_cm` | Optional metric; 0â€“500. |
| `notes` | `notes` | Optional; max 1000. |

At least one metric besides the date is required. No evidence was found for neck, shoulders, calves, or other additional measurements; they must not be invented.

Evidence: `src/services/coaching-service.js`, `database/schema.sql`, `public/js/pages/coaching/coaching.js`.

## CRUD and API

```text
GET    /api/clients/:id/measurements
POST   /api/clients/:id/measurements
PUT    /api/clients/:id/measurements/:measurementId
DELETE /api/clients/:id/measurements/:measurementId
```

Routes are protected by coaching read/create/update/delete permission families. Records are scoped to the member in service queries, activity is logged, and delete is a hard delete in the observed implementation.

## UI behavior

The coaching profile displays recent measurements, and the Nutrition Builder uses the newest available weight/height to prefill calculator inputs. The date of that source measurement is transient in the builder; no permanent â€œcalculated from measurement Xâ€ link was found in the diet plan.

## Check-ins are separate

Runtime coaching setup also creates `athlete_checkins` with:

```text
checkin_date, sleep_hours, sleep_quality, fatigue, soreness,
stress, mood, resting_hr, hrv, bodyweight_kg, notes
```

The service validates date uniqueness per member and observed ranges (sleep 0â€“24, rating values 1â€“5, vital ranges), and exposes check-in CRUD under the client route family. This table is runtime-created and not present in the baseline `database/schema.sql`.

**BLOCKED: SPECIFICATION CONFLICT** â€” baseline schema and runtime coaching DDL differ.

## Derived progress behavior

The training overview/progress code uses oldest/newest weight to calculate weight change and aggregates training volume, sessions, meal logs, and activity. It is a legacy derived summary; no LogicFit calculation contract is approved by this audit.

## Gaps

- No measurement approval/history workflow was found.
- No immutable measurement snapshot is linked to a published training/nutrition plan.
- No native mobile implementation exists in TOP GYM.
- No documents/files or medical-data consent boundary was found.
- Live SQL state was not queried; this document maps implementation/schema, not current production rows.

## Source Consolidation Resolution - 2026-08-25

The live database snapshot and runtime DDL now supplement this static measurement mapping. Runtime-only check-in/calculator structures are handled by TOP_GYM_LOGICFIT_DATABASE_DECISION.md; no additional measurements are invented. Any LogicFit measurement-to-calculation linkage and snapshot semantics belong to the Nutrition/Training contracts.

