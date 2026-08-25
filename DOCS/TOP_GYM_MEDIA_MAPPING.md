# TOP GYM Media Mapping

**Audit date:** 2026-08-25  
**Boundary:** local files/manifests were inspected; no media was changed or downloaded.

## Local media inventory

| Media | Observed inventory | Runtime use |
|---|---:|---|
| Exercise directories | 873 | Start/end image lookup through `exercise-assets.js`. |
| Exercise WebP images | 1,746 | Two images per current exercise. |
| Muscle directories | 188 | Main/front/back/side muscle imagery where mapped. |
| Muscle WebP images | 564 | Manifest-controlled muscle gallery. |
| Anatomy GLB | 1 | `public/assets/anatomy/top-gym-anatomy.glb`; integration coverage requires review. |
| Public manifests | 3 JSON files | Exercise assets, muscle assets, anatomy-muscle mapping. |

Other visible UI assets include login/gym backgrounds and local CSS/font references. The source tree also contains QA image/PDF artifacts; those are evidence artifacts, not product seed records.

## Exercise assets

`public/data/exercise-assets.json` has 873 records and 265 project links. The manifest records upstream repository/revision metadata, WebP format, 720×480 image size, and start/end phases. `public/js/exercise-assets.js` resolves canonical records first and legacy exact/alias links where permitted, then renders a fallback when an image is unavailable.

## Muscle/anatomy assets

`public/data/muscle-assets.json` reports 297 system records, 214 mapped records, 83 manual-review records, 188 unique canonical structures, and 564 downloaded images. Each canonical structure can expose front/back/side views.

`public/data/anatomy-muscle-mapping.json` reports 132 mapped system muscles, 194 mapped meshes, 9 ambiguous elements, and 165 unmapped system muscles. No mapping may be filled by assumption.

## Library-to-media trace

```text
JSON library/source ID
  → SQL library row / API projection
  → exercise-assets.js or muscle-assets.js manifest lookup
  → local /assets WebP or GLB
  → screen/profile/portal/print renderer
```

Exercise and muscle media are not stored as binary data in the SQL tables; metadata and manifests complete the presentation mapping.

## Sources and licenses recorded by TOP GYM

- Exercise media: upstream `yuhonas/free-exercise-db` revision recorded in manifest; license metadata says Unlicense as declared by upstream.
- Muscle renders: BodyParts3D/Anatomography metadata; CC BY-SA 2.1 Japan and attribution are documented in `docs/MUSCLE_ANATOMY_ASSETS.md`.
- Anatomy GLB: source/license/attribution are documented in `docs/ANATOMY-3D.md` and asset README.

These are legacy evidence records. LogicFit must perform its own legal/asset approval before redistribution.

## External/runtime assets

The inspected UI references external Google Fonts, CDNJS html2pdf, QRCode/html5-qrcode, and SweetAlert resources. This conflicts with an assumption that every local development path is fully offline unless the dependency is replaced/bundled behind an adapter.

**BLOCKED: SPECIFICATION GAP** — no approved LogicFit local asset adapter policy has been applied yet.
