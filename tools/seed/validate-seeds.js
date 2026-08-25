/* LogicFit Phase 3 seed validator. No database connection is required. */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const PROJECT_ROOT = path.resolve(__dirname, '..', '..');
const SEED_ROOT = path.join(PROJECT_ROOT, 'database', 'seeds');
const DATASET_ROOT = path.join(SEED_ROOT, 'v1');
const EXPECTED_DATASETS = [
  'muscle-groups',
  'muscles',
  'equipment',
  'exercise-categories',
  'levels',
  'exercises',
  'anatomy-mappings',
  'food-categories',
  'units',
  'foods',
  'food-conversions',
];
const EXPECTED_UNITS = ['gram', 'kilogram', 'milliliter', 'liter', 'piece', 'serving'];
const SEED_KEY_PATTERN = /^[a-z0-9][a-z0-9-]*\.[\p{L}\p{N}-]+\.[a-f0-9]{12}$/u;

function readJson(filePath) {
  return JSON.parse(fs.readFileSync(filePath, 'utf8'));
}

function sha256(value) {
  return crypto.createHash('sha256').update(value, 'utf8').digest('hex');
}

function addError(errors, code, message, context = {}) {
  errors.push({ code, message, ...context });
}

function addWarning(warnings, code, message, context = {}) {
  warnings.push({ code, message, ...context });
}

function isNonNegativeNumber(value) {
  return typeof value === 'number' && Number.isFinite(value) && value >= 0;
}

function validateEnvelope(dataset, record, index, errors) {
  const context = { dataset, index: index + 1, seed_key: record?.seed_key };
  if (!record || typeof record !== 'object' || Array.isArray(record)) {
    addError(errors, 'INVALID_RECORD', 'Record must be an object.', context);
    return;
  }
  for (const field of ['seed_key', 'source', 'source_path', 'version', 'source_version', 'relationships', 'validation', 'record']) {
    if (!(field in record)) addError(errors, 'MISSING_ENVELOPE_FIELD', `Missing envelope field ${field}.`, context);
  }
  if (typeof record.seed_key !== 'string' || !SEED_KEY_PATTERN.test(record.seed_key)) {
    addError(errors, 'INVALID_SEED_KEY', 'Seed key does not match the stable-key shape.', context);
  }
  if (record.source === 'top-gym' && (!record.source_path || (Array.isArray(record.source_path) && record.source_path.length === 0))) {
    addError(errors, 'MISSING_SOURCE_PATH', 'TOP GYM record must have source path provenance.', context);
  }
  if (record.version !== 'v1') addError(errors, 'INVALID_SEED_VERSION', 'Record version must be v1.', context);
  if (!record.source_version) addError(errors, 'MISSING_SOURCE_VERSION', 'Record source_version is required.', context);
  if (!record.validation || typeof record.validation.status !== 'string') {
    addError(errors, 'INVALID_VALIDATION_ENVELOPE', 'Record validation status is required.', context);
  }
  if (!record.record || typeof record.record !== 'object' || Array.isArray(record.record)) {
    addError(errors, 'INVALID_RECORD_PAYLOAD', 'Record payload must be an object.', context);
  }
}

function makeKeySet(dataset) {
  return new Set(dataset.records.map((record) => record.seed_key));
}

function assertReference(errors, keySet, key, context, field) {
  if (typeof key !== 'string' || !keySet.has(key)) {
    addError(errors, 'BROKEN_REFERENCE', `${field} does not resolve to a seed record.`, { ...context, reference: key });
  }
}

function validateDatasetShape(dataset, errors, warnings) {
  if (!dataset || dataset.schema_version !== 'logicfit.seed.dataset.v1') {
    addError(errors, 'INVALID_DATASET_SCHEMA', 'Dataset schema_version is invalid.', { dataset: dataset?.dataset });
    return;
  }
  if (!EXPECTED_DATASETS.includes(dataset.dataset)) addError(errors, 'UNEXPECTED_DATASET', 'Dataset is not in the approved Phase 3 set.', { dataset: dataset.dataset });
  if (!Array.isArray(dataset.records)) addError(errors, 'RECORDS_NOT_ARRAY', 'Dataset records must be an array.', { dataset: dataset.dataset });
  if (!Array.isArray(dataset.dependencies)) addError(errors, 'DEPENDENCIES_NOT_ARRAY', 'Dataset dependencies must be an array.', { dataset: dataset.dataset });
  if (!Array.isArray(dataset.source_paths) || dataset.source_paths.length === 0) addError(errors, 'SOURCE_PATHS_MISSING', 'Dataset source_paths must be non-empty.', { dataset: dataset.dataset });
  if (dataset.destination_table === undefined) addError(errors, 'DESTINATION_MISSING', 'Dataset destination_table must be explicit, including null for contract-only metadata.', { dataset: dataset.dataset });
  const keys = new Set();
  let previousKey = '';
  for (let index = 0; index < (dataset.records || []).length; index += 1) {
    const record = dataset.records[index];
    validateEnvelope(dataset.dataset, record, index, errors);
    if (record?.seed_key && keys.has(record.seed_key)) addError(errors, 'DUPLICATE_SEED_KEY', 'Duplicate seed key in dataset.', { dataset: dataset.dataset, seed_key: record.seed_key });
    if (record?.seed_key) keys.add(record.seed_key);
    if (record?.seed_key && previousKey && record.seed_key.localeCompare(previousKey) < 0) {
      addError(errors, 'UNSTABLE_RECORD_ORDER', 'Records must be sorted by seed_key for reviewable deterministic output.', { dataset: dataset.dataset, seed_key: record.seed_key, previous_key: previousKey });
    }
    if (record?.seed_key) previousKey = record.seed_key;
  }
  if (dataset.unresolved !== undefined && !Array.isArray(dataset.unresolved)) {
    addError(errors, 'UNRESOLVED_NOT_ARRAY', 'Unresolved metadata must be an array.', { dataset: dataset.dataset });
  }
  if (dataset.dataset === 'food-conversions' && dataset.destination_table !== null) {
    addError(errors, 'CONVERSION_DESTINATION_CONTRADICTION', 'Food conversions are contract-only in Phase 2 and must not invent a DB table.', {});
  }
  if (dataset.records.length === 0) addWarning(warnings, 'EMPTY_DATASET', 'Dataset has no installable records.', { dataset: dataset.dataset });
}

function validateRelations(datasets, errors) {
  const keys = Object.fromEntries(Object.entries(datasets).map(([name, value]) => [name, makeKeySet(value)]));
  const muscleGroups = datasets['muscle-groups'];
  for (const [index, record] of datasets.muscles.records.entries()) {
    assertReference(errors, keys['muscle-groups'], record.record.muscle_group_seed_key, { dataset: 'muscles', index: index + 1, seed_key: record.seed_key }, 'muscle_group_seed_key');
  }
  for (const [index, record] of datasets.exercises.records.entries()) {
    const context = { dataset: 'exercises', index: index + 1, seed_key: record.seed_key };
    assertReference(errors, keys.muscles, record.record.primary_muscle_seed_key, context, 'primary_muscle_seed_key');
    assertReference(errors, keys['exercise-categories'], record.record.category_seed_key, context, 'category_seed_key');
    assertReference(errors, keys.levels, record.record.level_seed_key, context, 'level_seed_key');
    for (const key of record.record.equipment_seed_keys || []) assertReference(errors, keys.equipment, key, context, 'equipment_seed_key');
    for (const item of record.record.secondary_muscles || []) assertReference(errors, keys.muscles, item.muscle_seed_key, context, 'secondary_muscle_seed_key');
    for (const key of record.relationships.secondary_muscle_seed_keys || []) assertReference(errors, keys.muscles, key, context, 'relationship.secondary_muscle_seed_key');
    if (record.record.catalog_status === 'active' && record.record.selectable !== true) addError(errors, 'ACTIVE_NOT_SELECTABLE', 'Active exercise must be selectable.', context);
    if (record.record.catalog_status === 'legacy-compatibility' && record.record.selectable !== false) addError(errors, 'LEGACY_SELECTABLE', 'Legacy compatibility exercise must not be selectable by default.', context);
  }
  for (const [index, record] of datasets['anatomy-mappings'].records.entries()) {
    const context = { dataset: 'anatomy-mappings', index: index + 1, seed_key: record.seed_key };
    assertReference(errors, keys.muscles, record.record.muscle_seed_key, context, 'muscle_seed_key');
    if (record.record.mapping_status !== 'mapped' || !record.record.asset_key) addError(errors, 'INVALID_ANATOMY_MAPPING', 'Installable anatomy mapping must be mapped and have an asset key.', context);
  }
  for (const [index, record] of datasets['foods'].records.entries()) {
    const context = { dataset: 'foods', index: index + 1, seed_key: record.seed_key };
    assertReference(errors, keys['food-categories'], record.record.category_seed_key, context, 'category_seed_key');
    assertReference(errors, keys.units, record.record.serving_unit_key, context, 'serving_unit_key');
    assertReference(errors, keys.units, record.record.calculation_unit_key, context, 'calculation_unit_key');
  }
  for (const [index, record] of datasets['food-conversions'].records.entries()) {
    const context = { dataset: 'food-conversions', index: index + 1, seed_key: record.seed_key };
    assertReference(errors, keys.units, record.record.source_unit_seed_key, context, 'source_unit_seed_key');
    assertReference(errors, keys.units, record.record.destination_unit_seed_key, context, 'destination_unit_seed_key');
    if (record.record.source_unit_seed_key !== record.record.destination_unit_seed_key || record.record.factor !== 1) {
      addError(errors, 'UNAPPROVED_CONVERSION', 'Only explicit same-unit identity conversions are allowed in v1.', context);
    }
  }
  if (muscleGroups.records.length < 1) addError(errors, 'MUSCLE_GROUPS_EMPTY', 'At least one muscle group is required.');
}

function validateDomainRules(datasets, errors, warnings) {
  const levels = datasets.levels.records;
  const levelCodes = new Set(levels.map((record) => `${record.record.level_type}:${record.record.code}`));
  for (const required of ['exercise_difficulty:beginner', 'exercise_difficulty:intermediate', 'exercise_difficulty:expert', 'plan_level:beginner', 'plan_level:intermediate', 'plan_level:advanced']) {
    if (!levelCodes.has(required)) addError(errors, 'MISSING_LEVEL', `Missing approved level ${required}.`);
  }
  const units = datasets.units.records.map((record) => record.record.code);
  for (const unit of EXPECTED_UNITS) if (!units.includes(unit)) addError(errors, 'MISSING_UNIT', `Missing approved unit ${unit}.`);
  if (new Set(units).size !== units.length) addError(errors, 'DUPLICATE_UNIT_CODE', 'Unit codes must be unique.');

  for (const [index, record] of datasets.foods.records.entries()) {
    const context = { dataset: 'foods', index: index + 1, seed_key: record.seed_key };
    const item = record.record;
    for (const nutrient of ['calories', 'protein', 'carbs', 'fat', 'fiber', 'sugar', 'sodium']) {
      if (!isNonNegativeNumber(item[nutrient])) addError(errors, 'INVALID_NUTRITION_VALUE', `${nutrient} must be a finite non-negative number.`, { ...context, field: nutrient, value: item[nutrient] });
    }
    if (!isNonNegativeNumber(item.serving_quantity) || item.serving_quantity <= 0) addError(errors, 'INVALID_SERVING_QUANTITY', 'Food serving quantity must be positive.', context);
    if (item.serving_quantity !== item.calculation_quantity || item.serving_unit_key !== item.calculation_unit_key) {
      addError(errors, 'UNSUPPORTED_FOOD_CONVERSION', 'Food v1 must preserve the source serving basis unless an explicit conversion exists.', context);
    }
    if (!item.name_en || !item.name_ar) addError(errors, 'MISSING_FOOD_BILINGUAL_NAME', 'Source food must have Arabic and English names.', context);
  }

  for (const [index, record] of datasets.muscles.records.entries()) {
    const context = { dataset: 'muscles', index: index + 1, seed_key: record.seed_key };
    if (!record.record.name_en || !record.record.name_ar) addError(errors, 'MISSING_MUSCLE_BILINGUAL_NAME', 'Source muscle must have Arabic and English names.', context);
  }
  for (const [index, record] of datasets.exercises.records.entries()) {
    const context = { dataset: 'exercises', index: index + 1, seed_key: record.seed_key };
    if (!record.record.name_en || !record.record.name_ar) addError(errors, 'MISSING_EXERCISE_BILINGUAL_NAME', 'Exercise must have Arabic and English names.', context);
    if (!['beginner', 'intermediate', 'expert'].includes(record.record.difficulty_code)) addError(errors, 'INVALID_EXERCISE_DIFFICULTY', 'Exercise difficulty must use ExerciseDifficulty canonical values.', context);
    if (record.record.catalog_status === 'legacy-compatibility' && record.record.active !== false) addError(errors, 'LEGACY_ACTIVE', 'Legacy compatibility records must be inactive.', context);
  }

  const canonicalExerciseIds = new Map();
  const canonicalSlugs = new Map();
  for (const record of datasets.exercises.records) {
    const identity = JSON.stringify({
      slug: record.record.slug,
      category: record.record.category_seed_key,
      equipment: [...(record.record.equipment_seed_keys || [])].sort(),
      primary: record.record.primary_muscle_seed_key,
    });
    if (canonicalExerciseIds.has(identity)) addError(errors, 'DUPLICATE_CANONICAL_EXERCISE', 'Two seed records share the frozen canonical exercise identity.', { seed_key: record.seed_key, other_seed_key: canonicalExerciseIds.get(identity) });
    canonicalExerciseIds.set(identity, record.seed_key);
    if (canonicalSlugs.has(record.record.slug)) addError(errors, 'DUPLICATE_CANONICAL_SLUG', 'Canonical exercise slugs must be unique after deterministic collision handling.', { seed_key: record.seed_key, slug: record.record.slug, other_seed_key: canonicalSlugs.get(record.record.slug) });
    canonicalSlugs.set(record.record.slug, record.seed_key);
  }
  const activeCount = datasets.exercises.records.filter((record) => record.record.catalog_status === 'active').length;
  const legacyCount = datasets.exercises.records.filter((record) => record.record.catalog_status === 'legacy-compatibility').length;
  const mergedCount = datasets.exercises.records.filter((record) => record.provenance?.legacy_duplicate_merged_into_active).length;
  const metadata = datasets.exercises.metadata || {};
  if (activeCount !== 873 || metadata.active_source_record_count !== 873) addError(errors, 'EXERCISE_ACTIVE_COUNT_MISMATCH', 'Expected 873 active source/canonical records.', { activeCount, metadata });
  if (metadata.legacy_source_record_count !== 265) addError(errors, 'EXERCISE_LEGACY_SOURCE_COUNT_MISMATCH', 'Expected 265 legacy source records.', { metadata });
  if (legacyCount !== 260 || mergedCount !== 5) addError(errors, 'EXERCISE_LEGACY_MAPPING_MISMATCH', 'Expected 260 legacy-only records and five merged legacy duplicate references.', { legacyCount, mergedCount });
  if (metadata.source_record_count !== 1138) addError(errors, 'EXERCISE_SOURCE_COUNT_MISMATCH', 'Expected 1,138 source records.', { metadata });
  if (datasets.muscles.records.length !== 297) addError(errors, 'MUSCLE_COUNT_MISMATCH', 'Expected 297 muscles.', { count: datasets.muscles.records.length });
  if (datasets.foods.records.length !== 367) addError(errors, 'FOOD_COUNT_MISMATCH', 'Expected 367 foods.', { count: datasets.foods.records.length });
  if (datasets['anatomy-mappings'].records.length !== 194 || datasets['anatomy-mappings'].unresolved?.length !== 165) addError(errors, 'ANATOMY_COUNT_MISMATCH', 'Expected 194 mapped entries and 165 unsupported muscle metadata rows.', { mapped: datasets['anatomy-mappings'].records.length, unresolved: datasets['anatomy-mappings'].unresolved?.length });
  if (datasets['anatomy-mappings'].metadata?.mapped_muscle_count !== 132) addError(errors, 'ANATOMY_MAPPED_MUSCLE_COUNT_MISMATCH', 'Expected 132 unique mapped muscles.', { metadata: datasets['anatomy-mappings'].metadata });
  if (datasets['food-conversions'].records.some((record) => record.record.status !== 'supported')) addError(errors, 'INVALID_CONVERSION_STATUS', 'All v1 conversion records must be supported identity records.');
  if ((metadata.source_slug_collision_group_count || 0) > 0) addWarning(warnings, 'SOURCE_SLUG_COLLISIONS', 'Source exercise slugs collide across distinct source identities; canonical slugs use deterministic hash suffixes and source_slug is preserved.', { groups: metadata.source_slug_collision_group_count, records: metadata.source_slug_collision_record_count });
  addWarning(warnings, 'LOCALIZED_LOOKUP_LABELS', 'Arabic labels remain null for lookup/unit records when the approved source has no Arabic label; this is explicit provenance, not a guessed translation.');
  addWarning(warnings, 'ANATOMY_AUDIT_METADATA', 'The audit summary reported nine ambiguous elements, while the verified mapping file has 194 record-level entries all marked documented; no unverified mapping was promoted.', { reported: 9, record_level: datasets['anatomy-mappings'].metadata?.record_level_ambiguous_count_in_verified_manifest });
}

function validateManifest(manifest, datasets, errors) {
  if (!manifest || manifest.schema_version !== 'logicfit.seed.manifest.v1') addError(errors, 'INVALID_MANIFEST_SCHEMA', 'Manifest schema_version is invalid.');
  if (manifest.seed_version !== 'v1') addError(errors, 'INVALID_MANIFEST_VERSION', 'Manifest seed_version must be v1.');
  if (!Array.isArray(manifest.datasets)) addError(errors, 'MANIFEST_DATASETS_NOT_ARRAY', 'Manifest datasets must be an array.');
  const manifestNames = (manifest.datasets || []).map((item) => item.dataset);
  if (JSON.stringify(manifestNames) !== JSON.stringify(EXPECTED_DATASETS)) addError(errors, 'MANIFEST_DATASET_ORDER', 'Manifest dataset order differs from the approved install order.', { expected: EXPECTED_DATASETS, actual: manifestNames });
  for (const item of manifest.datasets || []) {
    const filePath = path.join(SEED_ROOT, item.file || '');
    if (!fs.existsSync(filePath)) {
      addError(errors, 'MANIFEST_FILE_MISSING', 'Manifest file does not exist.', { dataset: item.dataset, file: item.file });
      continue;
    }
    const raw = fs.readFileSync(filePath, 'utf8');
    const actualChecksum = sha256(raw);
    if (actualChecksum !== item.checksum_sha256) addError(errors, 'CHECKSUM_MISMATCH', 'Manifest checksum does not match dataset bytes.', { dataset: item.dataset, expected: item.checksum_sha256, actual: actualChecksum });
    if (!datasets[item.dataset]) addError(errors, 'MANIFEST_UNKNOWN_DATASET', 'Manifest references an unloaded dataset.', { dataset: item.dataset });
    else if (item.record_count !== datasets[item.dataset].records.length) addError(errors, 'MANIFEST_COUNT_MISMATCH', 'Manifest record count differs from dataset.', { dataset: item.dataset, manifest: item.record_count, actual: datasets[item.dataset].records.length });
  }
}

function writeManifestValidation(manifest, result) {
  const updated = JSON.parse(JSON.stringify(manifest));
  updated.validation = {
    status: result.status,
    error_count: result.error_count,
    warning_count: result.warning_count,
    validated_at_utc: new Date().toISOString(),
  };
  updated.datasets = updated.datasets.map((item) => ({ ...item, validation_status: result.status === 'GREEN' ? 'passed' : 'failed' }));
  fs.writeFileSync(path.join(SEED_ROOT, 'manifest.json'), `${JSON.stringify(updated, null, 2)}\n`, 'utf8');
}

function main() {
  const writeManifest = process.argv.includes('--write-manifest');
  const errors = [];
  const warnings = [];
  const datasets = {};
  for (const name of EXPECTED_DATASETS) {
    const filePath = path.join(DATASET_ROOT, `${name}.json`);
    if (!fs.existsSync(filePath)) {
      addError(errors, 'DATASET_FILE_MISSING', 'Expected dataset file is missing.', { dataset: name, file: filePath });
      continue;
    }
    datasets[name] = readJson(filePath);
    validateDatasetShape(datasets[name], errors, warnings);
  }
  if (Object.keys(datasets).length === EXPECTED_DATASETS.length) {
    validateRelations(datasets, errors);
    validateDomainRules(datasets, errors, warnings);
  }
  const manifestPath = path.join(SEED_ROOT, 'manifest.json');
  const manifest = fs.existsSync(manifestPath) ? readJson(manifestPath) : null;
  validateManifest(manifest, datasets, errors);
  const result = {
    status: errors.length === 0 ? 'GREEN' : 'RED',
    error_count: errors.length,
    warning_count: warnings.length,
    errors,
    warnings,
    counts: Object.fromEntries(Object.entries(datasets).map(([name, value]) => [name, { records: value.records.length, unresolved: value.unresolved?.length || 0 }])),
  };
  if (writeManifest && errors.length === 0) writeManifestValidation(manifest, result);
  console.log(JSON.stringify(result, null, 2));
  if (errors.length > 0) process.exitCode = 1;
}

main();
