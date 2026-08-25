# Seed Validation

Validator: [`tools/seed/validate-seeds.js`](../../tools/seed/validate-seeds.js)

The validator performs:

- dataset/schema/envelope validation;
- stable-key syntax, uniqueness, deterministic ordering, and source/version checks;
- duplicate canonical exercise and canonical slug checks;
- muscle-group, muscle, exercise, anatomy, food-category, unit, and level reference checks;
- active/legacy exercise state checks;
- exact source-count assertions (1,138 exercise references, 873 active, 265 legacy, 297 muscles, 367 foods);
- anatomy mapping and unsupported-count checks;
- non-negative nutrition and positive serving-basis validation;
- unit/conversion safety checks;
- manifest file/checksum/count validation.

Command used:

```powershell
node tools\seed\validate-seeds.js --write-manifest
```

Result: `GREEN`, 0 errors. The three warnings are explicit data-source/legacy metadata classifications and are not silent assumptions.

