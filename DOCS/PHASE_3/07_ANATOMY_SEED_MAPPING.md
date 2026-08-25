# Anatomy Seed Mapping

The verified `public/data/anatomy-muscle-mapping.json` manifest contains 194 record-level mappings, covering 132 unique system muscles. These 194 records are the only installable anatomy mappings.

Each mapping resolves to a muscle seed key and preserves the BodyParts3D element ID, concept IDs, representation IDs, source file, mapping method, confidence, and source manifest key. The canonical identity is:

```text
resolved muscle seed key + body region + bodyparts3d-element view + asset key
```

`view=bodyparts3d-element` explicitly describes the source representation; it does not claim a front/back/side visual orientation. The source asset key is `bodyparts3d:<elementId>`.

The remaining 165 muscles are listed in `unresolved` with `mapping_status=unsupported`, no asset key, and no destination insert. No anatomy relationship is invented. The audit summary reported nine ambiguous elements, but the verified manifest records all 194 entries with `confidence=documented`; the package preserves that discrepancy as audit metadata and does not promote an unverified mapping.

