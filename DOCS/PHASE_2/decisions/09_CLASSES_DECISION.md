# Decision 2.09 — Classes and Booking

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Class definitions, sessions, trainers, bookings, attendance, and no-show.

## Decision

Classes include definitions, sessions, trainer assignment, one-time/weekly recurrence, capacity, booking, cancellation, FIFO waitlist, attendance, and separate no-show tracking. A session has an optional/configurable capacity and a Gym-configurable cancellation cutoff whose default is two hours before start.

When capacity is reached, a booking is rejected unless waitlist is enabled. Cancellation does not equal no-show. The workflow does not add unapproved recurrence engines or walk-in/PT rules.
