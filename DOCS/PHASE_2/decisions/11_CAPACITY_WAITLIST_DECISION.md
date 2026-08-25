# Decision 2.11 — Capacity and Waitlist

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Class bookings.

## Decision

- Capacity is configurable per session.
- If capacity is reached and waitlist is disabled, booking is rejected.
- If waitlist is enabled, eligible members enter a FIFO waitlist.
- When a slot becomes available, the earliest eligible waitlisted member receives the opportunity or booking according to the configured workflow; the contract must record the resulting state and audit event.
- Cancellation cutoff defaults to two hours before session start and is configurable per Gym.
