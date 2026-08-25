# Decision 2.14 — Documents and Storage Adapter

**Status:** APPROVED  
**Authority:** Phase 2 Final Gap Resolution  
**Scope:** Member/Gym documents and generated files.

## Decision

- Development uses the local filesystem.
- Production candidate uses `StorageAdapter`, with Cloudflare R2 as the target implementation.
- Business logic depends only on the adapter, never directly on R2.
- Document metadata includes owner/member when applicable, category, filename, MIME type, size, storage key, creation date, uploader, status, and retention metadata.
- Access, upload, download, replacement, and deletion are permission-checked and audited where required.
