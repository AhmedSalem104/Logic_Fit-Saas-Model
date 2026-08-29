# Phase 7 - Provisioning Lifecycle Contract

**Status:** GREEN - the approved lifecycle and retry behavior are implemented and verified locally.
**Cancellation:** not implemented and not part of this contract.

## Canonical run states

The run state vocabulary is closed and contains exactly these values:

```text
Requested
Provisioning
Migrating
Seeding
Verifying
Active
ProvisioningFailed
MigrationFailed
SeedingFailed
VerificationFailed
```

No `Pending`, `Running`, `Success`, `Retrying`, `Cancelled`, `Ready`, or
other additional run state is admitted. `Pending`, `Running`, `Success`, and
`Failed` may appear only as technical per-step execution metadata in the
bounded `steps` response/table.

## Ordered steps and state mapping

| Order | Execution step | Run state | Success transition | Failure state |
|---:|---|---|---|---|
| 1 | Request validation | `Requested` | Organization creation | Accepted deterministic validation failure is rejected before `202`; post-acceptance resource/adapter failure uses `ProvisioningFailed` |
| 2 | Organization creation | `Provisioning` | Gym registry creation | `ProvisioningFailed` |
| 3 | Gym registry creation | `Provisioning` | Server placement | `ProvisioningFailed` |
| 4 | Server placement selection | `Provisioning` | Database creation | `ProvisioningFailed` |
| 5 | Database creation | `Provisioning` | EF Core migrations | `ProvisioningFailed` |
| 6 | EF Core migrations | `Migrating` | Canonical seeding | `MigrationFailed` |
| 7 | Canonical seed execution | `Seeding` | Verification | `SeedingFailed` |
| 8 | Schema/context/seed verification | `Verifying` | Owner initialization | `VerificationFailed` |
| 9 | Owner initialization | `Verifying` | Activation | `VerificationFailed` |
| 10 | Activation | `Active` | Terminal success | `VerificationFailed` if activation cannot be committed |

Owner initialization is deliberately a substep of `Verifying`; no separate
Owner lifecycle state is invented. A Gym becomes `Active` only after its
database, schema, seed, context, Owner identity/projection, and registry
transactional outcome are verified.

## Allowed transitions

```text
Requested -> Provisioning
Provisioning -> Migrating | ProvisioningFailed
Migrating -> Seeding | MigrationFailed
Seeding -> Verifying | SeedingFailed
Verifying -> Active | VerificationFailed
```

Retry transitions use the same operation and the failed stage:

```text
ProvisioningFailed -> Provisioning
MigrationFailed -> Migrating
SeedingFailed -> Seeding
VerificationFailed -> Verifying
```

The transition is accepted only when the persisted failure has
`retryable=true`; the retry request itself does not add a `Retrying` state.
`Active` is terminal for this phase and cannot silently move backward.

## Retryability and recovery

The failure state identifies the stage; the persisted `retryable` property
identifies whether controlled retry is allowed. Transient provider, network,
process, and recoverable lock failures may be marked retryable. Invalid input,
duplicate identity, invalid server registration, unowned/colliding database,
unsupported schema, and integrity failures are non-retryable until an
operator changes the underlying condition. This classification is recorded
as safe metadata, not as a new state.

Retry resumes the failed step using the same organization, Gym, server,
database, and owner request. EF migrations and seeds remain idempotent. No
automatic database deletion or replacement occurs.

## Process restart and concurrency

- The accepted operation is persisted before `202` is returned.
- The accepted operation is recovered by the local single-reader worker from
  persisted non-terminal states at application startup.
- A restarted worker resumes the persisted operation/step; it does not create
  a new organization, Gym, database, Owner, or run. Serializable acceptance,
  unique active-operation constraints, and idempotency hashes protect the
  local runtime from duplicate acceptance.
- One active operation is allowed per Gym. A competing request is a
  duplicate/conflict and does not start another worker.
- A partial database is retained and associated with the failed run. Reuse
  requires a matching operation ownership marker; unknown ownership is a
  safe non-retryable failure requiring operator resolution.
- There is no destructive automatic cleanup and no backup/restore step.

## Audit and visibility

State changes emit the controlled provisioning audit events. Status polling
returns the current state, current step, attempt, safe failure information,
and redacted placement/database metadata. The Phase 6 monitoring snapshot may
read this safe status through the same Control Plane registry/audit boundary;
there is no second monitoring system.

## Implementation verification

The workflow uses ten fixed steps, records technical step rows, persists the
four approved failure states, and supports controlled retry from a retryable
failure. The API test suite covers successful activation, retry rejection for
an active run, and startup recovery that safely records a non-retryable server
placement failure.
