/*
 * LogicFit Phase 3 canonical seed generator.
 *
 * This file is intentionally a data extraction tool, not application code.
 * It reads only the verified TOP GYM source paths and writes LogicFit seed
 * artifacts under database/seeds. It never writes to TOP GYM.
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const PROJECT_ROOT = path.resolve(__dirname, '..', '..');
const TOP_GYM_ROOT = process.env.TOP_GYM_ROOT || 'C:\\Users\\B-SMART\\gym-membership-app';
const SEED_ROOT = path.join(PROJECT_ROOT, 'database', 'seeds');
const DATASET_ROOT = path.join(SEED_ROOT, 'v1');
const SEED_VERSION = 'v1';
const SCHEMA_VERSION = 'logicfit.seed.dataset.v1';
const SOURCE_VERSION = 'top-gym-audit-2026-08-25';
const GENERATED_AT = process.env.LOGICFIT_SEED_GENERATED_AT || new Date().toISOString();

const SOURCE_PATHS = {
  activeExercises: 'data/library/exercises.json',
  duplicateExerciseDataset: 'data/library/exercises-dataset.json',
  legacyExercises: 'data/library/exercises-legacy.json',
  muscles: 'data/library/muscles.json',
  foods: 'data/library/foods.json',
  anatomy: 'public/data/anatomy-muscle-mapping.json',
  muscleAssets: 'public/data/muscle-assets.json',
  exerciseAssets: 'public/data/exercise-assets.json',
  planLevelUi: 'public/index.html',
  planLevelService: 'src/services/intelligence-service.js',
};

const DESTINATIONS = {
  'muscle-groups': 'library.muscle_groups',
  muscles: 'library.muscles',
  equipment: 'library.equipment',
  'exercise-categories': 'library.exercise_categories',
  levels: 'library.levels',
  exercises: 'library.exercises',
  'anatomy-mappings': 'library.anatomy_mappings',
  'food-categories': 'library.food_categories',
  units: 'library.food_units',
  foods: 'library.foods',
  // Phase 2 intentionally has no food_conversions table. This artifact is
  // contract/validation metadata and is not installed into a DB table.
  'food-conversions': null,
};

function readJson(relativePath) {
  const absolutePath = path.join(TOP_GYM_ROOT, relativePath);
  return JSON.parse(fs.readFileSync(absolutePath, 'utf8'));
}

function ensureDir(directory) {
  fs.mkdirSync(directory, { recursive: true });
}

function canonicalize(value) {
  if (Array.isArray(value)) return value.map(canonicalize);
  if (value && typeof value === 'object') {
    return Object.keys(value).sort().reduce((result, key) => {
      result[key] = canonicalize(value[key]);
      return result;
    }, {});
  }
  return value;
}

function stableJson(value) {
  return JSON.stringify(canonicalize(value));
}

function sha256(value) {
  return crypto.createHash('sha256').update(value, 'utf8').digest('hex');
}

function normalizeText(value) {
  return String(value ?? '')
    .normalize('NFKC')
    .trim()
    .toLocaleLowerCase('en-US')
    .replace(/[\s_./\\]+/gu, ' ')
    .replace(/\s+/gu, ' ');
}

function normalizeCode(value) {
  return normalizeText(value)
    .replace(/[^\p{L}\p{N}]+/gu, '_')
    .replace(/^_+|_+$/gu, '')
    .replace(/_+/gu, '_');
}

function slugify(value) {
  const slug = String(value ?? '')
    .normalize('NFKC')
    .trim()
    .toLocaleLowerCase('en-US')
    .replace(/[\s_./\\]+/gu, '-')
    .replace(/[^\p{L}\p{N}-]+/gu, '-')
    .replace(/-+/gu, '-')
    .replace(/^-|-$/gu, '');
  return slug || 'unavailable';
}

function uniqueSorted(values) {
  return [...new Set(values.filter((value) => value !== null && value !== undefined && value !== ''))]
    .sort((left, right) => String(left).localeCompare(String(right), 'en', { sensitivity: 'variant' }));
}

function asList(value) {
  if (Array.isArray(value)) return value;
  if (value === null || value === undefined || value === '') return [];
  return [value];
}

function sourceHash(sourceRecord) {
  const clone = JSON.parse(JSON.stringify(sourceRecord));
  delete clone._source_kind;
  delete clone._source_ordinal;
  return sha256(stableJson(clone));
}

function seedKey(domain, humanSlug, identity) {
  return `${domain}.${slugify(humanSlug)}.${sha256(stableJson(identity)).slice(0, 12)}`;
}

function envelope({
  seedKeyValue,
  source = 'top-gym',
  sourceId,
  sourceIdKind,
  sourcePath,
  destinationTable,
  relationships = {},
  record,
  provenance,
}) {
  const result = {
    seed_key: seedKeyValue,
    source,
    source_path: sourcePath,
    destination_table: destinationTable,
    version: SEED_VERSION,
    source_version: SOURCE_VERSION,
    relationships,
    validation: { status: 'generated' },
    record,
  };
  if (sourceId !== undefined) result.source_id = sourceId;
  if (sourceIdKind) result.source_id_kind = sourceIdKind;
  if (provenance) result.provenance = provenance;
  return result;
}

function dataset({
  name,
  destinationTable,
  dependencies,
  sourcePaths,
  records,
  sourcePolicy,
  metadata,
  unresolved,
}) {
  const result = {
    schema_version: SCHEMA_VERSION,
    dataset: name,
    seed_version: SEED_VERSION,
    destination_table: destinationTable,
    dependencies,
    source_paths: sourcePaths,
    source_policy: sourcePolicy,
    records: records.sort((left, right) => left.seed_key.localeCompare(right.seed_key)),
  };
  if (metadata) result.metadata = metadata;
  if (unresolved) result.unresolved = unresolved.sort((left, right) => left.seed_key.localeCompare(right.seed_key));
  return result;
}

function writeJson(filePath, value) {
  fs.writeFileSync(filePath, `${JSON.stringify(canonicalize(value), null, 2)}\n`, 'utf8');
}

function compareSourceExerciseDatasets(activeExercises, duplicateDataset) {
  if (activeExercises.length !== duplicateDataset.length) {
    throw new Error(`Exercise duplicate evidence count mismatch: ${activeExercises.length} !== ${duplicateDataset.length}`);
  }
  activeExercises.forEach((exercise, index) => {
    if (stableJson(exercise) !== stableJson(duplicateDataset[index])) {
      throw new Error(`Exercise duplicate evidence differs at ordinal ${index + 1}`);
    }
  });
}

function makeLookupRecords({
  values,
  domain,
  destinationTable,
  sourcePaths,
  sourceValueLabel,
}) {
  const groups = new Map();
  for (const item of values) {
    const raw = String(item.value ?? '').trim();
    if (!raw) throw new Error(`Missing ${sourceValueLabel} source value`);
    const identityValue = normalizeText(raw);
    if (!groups.has(identityValue)) groups.set(identityValue, []);
    groups.get(identityValue).push(item);
  }

  return [...groups.entries()].map(([identityValue, items]) => {
    const rawValues = uniqueSorted(items.map((item) => String(item.value).trim()));
    const nameEn = rawValues[0];
    const identity = { namespace: domain, label: identityValue };
    return envelope({
      seedKeyValue: seedKey(domain, nameEn, identity),
      sourcePath: sourcePaths,
      destinationTable,
      relationships: {},
      record: {
        code: normalizeCode(nameEn),
        name_en: nameEn,
        name_ar: null,
        active: true,
        record_scope: 'canonical',
        source_values: rawValues,
        localized_label_status: 'unavailable-in-verified-source',
      },
      provenance: {
        source_value_count: items.length,
        source_scopes: uniqueSorted(items.map((item) => item.scope || 'unknown')),
        source_value_field: sourceValueLabel,
      },
    });
  });
}

function buildSources() {
  const activeExercises = readJson(SOURCE_PATHS.activeExercises);
  const duplicateExerciseDataset = readJson(SOURCE_PATHS.duplicateExerciseDataset);
  const legacyExercises = readJson(SOURCE_PATHS.legacyExercises);
  const muscles = readJson(SOURCE_PATHS.muscles);
  const foods = readJson(SOURCE_PATHS.foods);
  const anatomyManifest = readJson(SOURCE_PATHS.anatomy);
  const muscleAssetsManifest = readJson(SOURCE_PATHS.muscleAssets);
  const exerciseAssetsManifest = readJson(SOURCE_PATHS.exerciseAssets);

  compareSourceExerciseDatasets(activeExercises, duplicateExerciseDataset);

  const active = activeExercises.map((record, index) => ({
    ...record,
    _source_kind: 'active',
    _source_ordinal: index + 1,
  }));
  const legacy = legacyExercises.map((record, index) => ({
    ...record,
    _source_kind: 'legacy-compatibility',
    _source_ordinal: index + 1,
  }));

  return {
    active,
    duplicateExerciseDataset,
    legacy,
    muscles,
    foods,
    anatomyManifest,
    muscleAssetsManifest,
    exerciseAssetsManifest,
  };
}

function buildSeedPackage() {
  const sources = buildSources();
  const {
    active,
    legacy,
    muscles: sourceMuscles,
    foods: sourceFoods,
    anatomyManifest,
    muscleAssetsManifest,
    exerciseAssetsManifest,
  } = sources;

  const muscleAssetById = new Map(
    (muscleAssetsManifest.records || []).map((record) => [Number(record.systemMuscleId), record]),
  );
  const exerciseAssetBySourceId = new Map(
    (exerciseAssetsManifest.records || []).map((record) => [Number(record.catalogSourceId), record]),
  );
  const legacyMediaByOrdinal = new Map(
    (exerciseAssetsManifest.projectLinks || []).map((record) => [Number(record.legacySourceId), record]),
  );

  const bodyPartValues = sourceMuscles.map((record) => ({ value: record.bodyPart, scope: 'muscles.json' }));
  const muscleGroupRecords = makeLookupRecords({
    values: bodyPartValues,
    domain: 'muscle-group',
    destinationTable: DESTINATIONS['muscle-groups'],
    sourcePaths: [SOURCE_PATHS.muscles],
    sourceValueLabel: 'bodyPart',
  });
  const muscleGroupByIdentity = new Map(
    muscleGroupRecords.map((record) => [normalizeText(record.record.name_en), record.seed_key]),
  );

  const muscleRecords = sourceMuscles.map((sourceRecord, index) => {
    const sourceId = index + 1;
    const groupSeedKey = muscleGroupByIdentity.get(normalizeText(sourceRecord.bodyPart));
    if (!groupSeedKey) throw new Error(`Missing muscle group for source muscle ${sourceId}`);
    const identity = {
      name_en: normalizeText(sourceRecord.name),
      body_part: normalizeText(sourceRecord.bodyPart),
      name_ar: normalizeText(sourceRecord.nameAr),
    };
    const asset = muscleAssetById.get(sourceId);
    return envelope({
      seedKeyValue: seedKey('muscle', sourceRecord.name, identity),
      sourceId,
      sourceIdKind: 'top-gym-runtime-array-index-provenance-only',
      sourcePath: [SOURCE_PATHS.muscles, SOURCE_PATHS.muscleAssets],
      destinationTable: DESTINATIONS.muscles,
      relationships: { muscle_group_seed_key: groupSeedKey },
      record: {
        name_en: sourceRecord.name,
        name_ar: sourceRecord.nameAr,
        body_part: sourceRecord.bodyPart,
        description_en: sourceRecord.description,
        description_ar: sourceRecord.descriptionAr,
        icon: sourceRecord.icon,
        muscle_group_seed_key: groupSeedKey,
        active: true,
        record_scope: 'canonical',
        media: asset ? {
          status: asset.status,
          confidence: asset.confidence,
          mapping_method: asset.mappingMethod,
          asset_slug: asset.assetSlug,
          canonical_key: asset.canonicalKey,
          image_assets: asset.imageAssets || null,
          source_anatomy_ids: asset.sourceAnatomyIds || [],
          source_representation_ids: asset.sourceRepresentationIds || [],
        } : {
          status: 'unavailable-in-source-manifest',
          confidence: null,
          mapping_method: null,
          asset_slug: null,
          canonical_key: null,
          image_assets: null,
          source_anatomy_ids: [],
          source_representation_ids: [],
        },
      },
      provenance: {
        source_record_ordinal: sourceId,
        source_id_is_not_identity: true,
        asset_manifest_record_status: asset?.status || 'unavailable-in-source-manifest',
      },
    });
  });
  const muscleBySourceId = new Map(muscleRecords.map((record, index) => [index + 1, record]));

  const equipmentValues = [];
  const categoryValues = [];
  for (const record of active) {
    for (const value of asList(record.equipment)) equipmentValues.push({ value, scope: 'active' });
    categoryValues.push({ value: record.category, scope: 'active' });
  }
  for (const record of legacy) {
    for (const value of asList(record.equipment)) equipmentValues.push({ value, scope: 'legacy-compatibility' });
    categoryValues.push({ value: record.category, scope: 'legacy-compatibility' });
  }

  const equipmentRecords = makeLookupRecords({
    values: equipmentValues,
    domain: 'equipment',
    destinationTable: DESTINATIONS.equipment,
    sourcePaths: [SOURCE_PATHS.activeExercises, SOURCE_PATHS.legacyExercises],
    sourceValueLabel: 'equipment',
  });
  const categoryRecords = makeLookupRecords({
    values: categoryValues,
    domain: 'exercise-category',
    destinationTable: DESTINATIONS['exercise-categories'],
    sourcePaths: [SOURCE_PATHS.activeExercises, SOURCE_PATHS.legacyExercises],
    sourceValueLabel: 'category',
  });
  const equipmentByIdentity = new Map(
    equipmentRecords.map((record) => [normalizeText(record.record.name_en), record.seed_key]),
  );
  const categoryByIdentity = new Map(
    categoryRecords.map((record) => [normalizeText(record.record.name_en), record.seed_key]),
  );

  const levelDefinitions = [
    {
      levelType: 'exercise_difficulty',
      code: 'beginner',
      nameEn: 'beginner',
      nameAr: null,
      sourceValues: ['beginner', 'Beginner'],
      sourcePaths: [SOURCE_PATHS.activeExercises, SOURCE_PATHS.legacyExercises],
      legacyMappings: [],
    },
    {
      levelType: 'exercise_difficulty',
      code: 'intermediate',
      nameEn: 'intermediate',
      nameAr: null,
      sourceValues: ['intermediate', 'Intermediate'],
      sourcePaths: [SOURCE_PATHS.activeExercises, SOURCE_PATHS.legacyExercises],
      legacyMappings: [],
    },
    {
      levelType: 'exercise_difficulty',
      code: 'expert',
      nameEn: 'expert',
      nameAr: null,
      sourceValues: ['expert'],
      sourcePaths: [SOURCE_PATHS.activeExercises],
      legacyMappings: [{ legacy_value: 'Advanced', canonical_code: 'expert', scope: 'exercise_difficulty' }],
    },
    {
      levelType: 'plan_level',
      code: 'beginner',
      nameEn: 'beginner',
      nameAr: 'مبتدئ',
      sourceValues: ['beginner'],
      sourcePaths: [SOURCE_PATHS.planLevelUi, SOURCE_PATHS.planLevelService],
      legacyMappings: [],
    },
    {
      levelType: 'plan_level',
      code: 'intermediate',
      nameEn: 'intermediate',
      nameAr: 'متوسط',
      sourceValues: ['intermediate'],
      sourcePaths: [SOURCE_PATHS.planLevelUi, SOURCE_PATHS.planLevelService],
      legacyMappings: [],
    },
    {
      levelType: 'plan_level',
      code: 'advanced',
      nameEn: 'advanced',
      nameAr: 'متقدم',
      sourceValues: ['advanced'],
      sourcePaths: [SOURCE_PATHS.planLevelUi, SOURCE_PATHS.planLevelService],
      legacyMappings: [],
    },
  ];
  const levelRecords = levelDefinitions.map((definition) => {
    const identity = { level_type: definition.levelType, code: definition.code };
    return envelope({
      seedKeyValue: seedKey('level', `${definition.levelType}-${definition.code}`, identity),
      sourcePath: definition.sourcePaths,
      destinationTable: DESTINATIONS.levels,
      relationships: {},
      record: {
        code: definition.code,
        level_type: definition.levelType,
        name_en: definition.nameEn,
        name_ar: definition.nameAr,
        active: true,
        record_scope: 'canonical',
        source_values: definition.sourceValues,
        legacy_mappings: definition.legacyMappings,
        localized_label_status: definition.nameAr ? 'source-evidenced' : 'unavailable-in-verified-source',
      },
      provenance: {
        separate_concept: true,
        source_value_scope: definition.levelType,
      },
    });
  });
  const levelByIdentity = new Map(
    levelRecords.map((record) => [`${record.record.level_type}:${record.record.code}`, record.seed_key]),
  );

  const exerciseIdentity = (record) => ({
    // The active file supplies a slug while the legacy file supplies only a
    // display name. Slugifying both makes the five source-level duplicates
    // converge without using an ordinal or numeric source ID.
    slug_or_name: slugify(record.slug || record.name),
    category: normalizeCode(record.category),
    equipment: asList(record.equipment).map((value) => normalizeText(value)).sort(),
    target_muscle_seed_key: muscleBySourceId.get(Number(record.targetMuscleId))?.seed_key || null,
  });
  const exerciseGroups = new Map();
  for (const sourceRecord of [...active, ...legacy]) {
    const identity = exerciseIdentity(sourceRecord);
    if (!identity.target_muscle_seed_key) throw new Error(`Exercise target muscle does not resolve: ${sourceRecord.name}`);
    const identityHash = stableJson(identity);
    if (!exerciseGroups.has(identityHash)) exerciseGroups.set(identityHash, { identity, records: [] });
    exerciseGroups.get(identityHash).records.push(sourceRecord);
  }
  for (const group of exerciseGroups.values()) {
    const activeCount = group.records.filter((record) => record._source_kind === 'active').length;
    const legacyCount = group.records.filter((record) => record._source_kind === 'legacy-compatibility').length;
    if (activeCount > 1 || legacyCount > 1) {
      throw new Error(`Unresolved duplicate exercise semantic identity: ${stableJson(group.identity)}`);
    }
  }

  // The source slug is not globally unique: distinct source exercises can
  // share a display slug while differing in category/equipment/target. Keep
  // the source slug for provenance and give the canonical DB slug a stable
  // content-hash suffix only for collision groups. This preserves meaning,
  // satisfies the Phase 2 slug uniqueness contract, and never uses order.
  const slugGroups = new Map();
  for (const group of exerciseGroups.values()) {
    const selected = group.records.find((record) => record._source_kind === 'active') || group.records[0];
    const sourceSlug = slugify(selected.slug || selected.name);
    if (!slugGroups.has(sourceSlug)) slugGroups.set(sourceSlug, []);
    slugGroups.get(sourceSlug).push(group);
  }
  const canonicalSlugByIdentity = new Map();
  for (const [sourceSlug, groups] of slugGroups.entries()) {
    for (const group of groups) {
      const canonicalSlug = groups.length === 1
        ? sourceSlug
        : `${sourceSlug}-${sha256(stableJson(group.identity)).slice(0, 8)}`;
      canonicalSlugByIdentity.set(stableJson(group.identity), { sourceSlug, canonicalSlug });
    }
  }

  const mediaForExercise = (sourceRecord) => {
    if (sourceRecord._source_kind === 'active') {
      const asset = exerciseAssetBySourceId.get(Number(sourceRecord.sourceId));
      return asset ? {
        status: asset.imageAudit?.status || 'manifest-recorded',
        asset_key: `exercise.${slugify(sourceRecord.slug || sourceRecord.name)}`,
        source_refs: asset.imageAssets || sourceRecord.imageAssets || null,
        image_audit: asset.imageAudit || sourceRecord.imageAudit || null,
        source_manifest_path: SOURCE_PATHS.exerciseAssets,
      } : {
        status: 'unavailable-in-source-manifest',
        asset_key: null,
        source_refs: sourceRecord.imageAssets || null,
        image_audit: sourceRecord.imageAudit || null,
        source_manifest_path: SOURCE_PATHS.exerciseAssets,
      };
    }
    const link = legacyMediaByOrdinal.get(Number(sourceRecord._source_ordinal));
    return link ? {
      status: link.status,
      asset_key: link.imageAssets ? `exercise.${slugify(link.projectNameEn || sourceRecord.name)}` : null,
      source_refs: link.imageAssets || null,
      link_method: link.method || null,
      confidence: link.confidence ?? null,
      source_manifest_path: SOURCE_PATHS.exerciseAssets,
    } : {
      status: 'unavailable-in-source-manifest',
      asset_key: null,
      source_refs: null,
      link_method: null,
      confidence: null,
      source_manifest_path: SOURCE_PATHS.exerciseAssets,
    };
  };

  const canonicalExerciseRecords = [...exerciseGroups.values()].map((group) => {
    const selected = group.records.find((record) => record._source_kind === 'active') || group.records[0];
    const legacySource = group.records.find((record) => record._source_kind === 'legacy-compatibility');
    const targetMuscle = muscleBySourceId.get(Number(selected.targetMuscleId));
    const secondaryMuscles = asList(selected.secondaryMuscles).map((secondary) => {
      const sourceMuscleId = typeof secondary === 'object'
        ? (secondary.muscleId ?? secondary.sourceId)
        : secondary;
      const muscle = muscleBySourceId.get(Number(sourceMuscleId));
      if (!muscle) throw new Error(`Exercise secondary muscle does not resolve: ${selected.name} -> ${sourceMuscleId}`);
      return {
        muscle_seed_key: muscle.seed_key,
        source_muscle_id: Number(sourceMuscleId),
        contribution_percent: typeof secondary === 'object' ? (secondary.contributionPercent ?? null) : null,
        role: typeof secondary === 'object' ? (secondary.role ?? 'secondary') : 'secondary',
      };
    }).sort((left, right) => `${left.muscle_seed_key}:${left.role}`.localeCompare(`${right.muscle_seed_key}:${right.role}`));
    const equipmentSeedKeys = asList(selected.equipment).map((value) => {
      const key = equipmentByIdentity.get(normalizeText(value));
      if (!key) throw new Error(`Exercise equipment does not resolve: ${selected.name} -> ${value}`);
      return key;
    }).sort();
    const categorySeedKey = categoryByIdentity.get(normalizeText(selected.category));
    if (!categorySeedKey) throw new Error(`Exercise category does not resolve: ${selected.name} -> ${selected.category}`);
    const difficultyCode = String(selected._source_kind === 'legacy-compatibility' ? selected.difficulty : selected.difficulty).toLocaleLowerCase('en-US');
    const canonicalDifficulty = difficultyCode === 'advanced' ? 'expert' : difficultyCode;
    const levelSeedKey = levelByIdentity.get(`exercise_difficulty:${canonicalDifficulty}`);
    if (!levelSeedKey) throw new Error(`Exercise difficulty does not resolve: ${selected.name} -> ${selected.difficulty}`);
    const identity = group.identity;
    const slugInfo = canonicalSlugByIdentity.get(stableJson(identity));
    if (!slugInfo) throw new Error(`Missing canonical slug mapping for ${selected.name}`);
    const selectedMedia = mediaForExercise(selected);
    const sourceSummaries = group.records.map((record) => ({
      source_path: record._source_kind === 'active' ? SOURCE_PATHS.activeExercises : SOURCE_PATHS.legacyExercises,
      source_record_ordinal: record._source_ordinal,
      source_id: record.sourceId ?? null,
      source_id_kind: record.sourceId != null ? 'top-gym-catalog-sourceId' : 'not-present-in-legacy-file',
      catalog_status: record._source_kind === 'active' ? 'active' : 'legacy-compatibility',
      source_hash: sourceHash(record),
    })).sort((left, right) => `${left.source_path}:${left.source_record_ordinal}`.localeCompare(`${right.source_path}:${right.source_record_ordinal}`));
    const isActive = Boolean(group.records.find((record) => record._source_kind === 'active'));
    const record = {
      name_en: selected.name,
      name_ar: selected.nameAr,
      slug: slugInfo.canonicalSlug,
      source_slug: slugInfo.sourceSlug,
      description_en: selected.description ?? null,
      description_ar: selected.descriptionAr ?? null,
      primary_muscle_seed_key: targetMuscle.seed_key,
      secondary_muscles: secondaryMuscles,
      equipment_seed_keys: equipmentSeedKeys,
      category_seed_key: categorySeedKey,
      difficulty_code: canonicalDifficulty,
      level_seed_key: levelSeedKey,
      movement_pattern: selected.movementPattern ?? null,
      mechanic: selected.mechanic ?? null,
      force: selected.force ?? null,
      is_high_impact: selected.isHighImpact ?? null,
      instructions_en: selected.instructions ?? null,
      instructions_ar: selected.instructionsAr ?? null,
      tips_en: selected.tips ?? null,
      tips_ar: selected.tipsAr ?? null,
      common_mistakes_en: selected.commonMistakes ?? null,
      common_mistakes_ar: selected.commonMistakesAr ?? null,
      reps_range: selected.repsRange ?? null,
      sets_range: selected.setsRange ?? null,
      rest_seconds: selected.restSeconds ?? null,
      tempo: selected.tempo ?? null,
      video_url: selected.videoUrl ?? null,
      media: selectedMedia,
      catalog_status: isActive ? 'active' : 'legacy-compatibility',
      record_scope: 'canonical',
      active: isActive,
      selectable: isActive,
    };
    const legacyMedia = legacySource ? mediaForExercise(legacySource) : null;
    return envelope({
      seedKeyValue: seedKey('exercise', selected.slug || selected.name, identity),
      sourceId: selected.sourceId ?? null,
      sourceIdKind: selected.sourceId != null ? 'top-gym-catalog-sourceId-provenance-only' : 'legacy-source-id-not-present',
      sourcePath: selected._source_kind === 'active' ? SOURCE_PATHS.activeExercises : SOURCE_PATHS.legacyExercises,
      destinationTable: DESTINATIONS.exercises,
      relationships: {
        primary_muscle_seed_key: targetMuscle.seed_key,
        secondary_muscle_seed_keys: secondaryMuscles.map((item) => item.muscle_seed_key),
        equipment_seed_keys: equipmentSeedKeys,
        category_seed_key: categorySeedKey,
        level_seed_key: levelSeedKey,
      },
      record,
      provenance: {
        source_records: sourceSummaries,
        legacy_duplicate_merged_into_active: Boolean(isActive && legacySource),
        legacy_media_link: legacyMedia,
        source_identity_fields: identity,
        source_slug: slugInfo.sourceSlug,
        canonical_slug: slugInfo.canonicalSlug,
      },
    });
  });

  const anatomyEntries = Object.entries(anatomyManifest.mappings || {}).sort(([left], [right]) => left.localeCompare(right));
  const mappedMuscleIds = new Set();
  const anatomyRecords = anatomyEntries.map(([mappingSourceKey, mapping]) => {
    const sourceMuscleId = Number(mapping.muscleId);
    const muscle = muscleBySourceId.get(sourceMuscleId);
    if (!muscle) throw new Error(`Anatomy mapping muscle does not resolve: ${mappingSourceKey}`);
    mappedMuscleIds.add(sourceMuscleId);
    const assetKey = mapping.bodyParts3dElementId ? `bodyparts3d:${mapping.bodyParts3dElementId}` : null;
    if (!assetKey) throw new Error(`Anatomy mapping lacks approved asset key: ${mappingSourceKey}`);
    const bodyRegion = muscle.record.body_part;
    const identity = {
      muscle_seed_key: muscle.seed_key,
      body_region: normalizeText(bodyRegion),
      view: 'bodyparts3d-element',
      asset_key: assetKey,
    };
    return envelope({
      seedKeyValue: seedKey('anatomy-mapping', `${slugify(muscle.record.name_en)}-${mapping.bodyParts3dElementId}`, identity),
      sourceId: mappingSourceKey,
      sourceIdKind: 'top-gym-anatomy-manifest-key-provenance-only',
      sourcePath: SOURCE_PATHS.anatomy,
      destinationTable: DESTINATIONS['anatomy-mappings'],
      relationships: { muscle_seed_key: muscle.seed_key },
      record: {
        muscle_seed_key: muscle.seed_key,
        body_region: bodyRegion,
        view: 'bodyparts3d-element',
        view_semantics: 'source is a BodyParts3D model element; no front/back/side orientation is claimed',
        asset_key: assetKey,
        source_anatomy_element_id: mapping.bodyParts3dElementId,
        source_concept_ids: mapping.bodyParts3dConceptIds || [],
        source_representation_ids: mapping.representationIds || [],
        source_file: mapping.sourceFile || null,
        mapping_method: mapping.mappingMethod,
        confidence: mapping.confidence,
        mapping_status: 'mapped',
        active: true,
        record_scope: 'canonical',
      },
      provenance: {
        source_mapping_key: mappingSourceKey,
        system_name: mapping.systemName,
        system_name_ar: mapping.systemNameAr,
        source_manifest_schema_version: anatomyManifest.schemaVersion,
      },
    });
  });
  const anatomyUnresolved = sourceMuscles
    .map((sourceRecord, index) => ({ sourceRecord, sourceId: index + 1 }))
    .filter(({ sourceId }) => !mappedMuscleIds.has(sourceId))
    .map(({ sourceRecord, sourceId }) => {
      const muscle = muscleBySourceId.get(sourceId);
      return {
        seed_key: `anatomy-unresolved.${slugify(muscle.record.name_en)}.${sha256(stableJson({ muscle_seed_key: muscle.seed_key, status: 'unsupported' })).slice(0, 12)}`,
        source: 'top-gym',
        source_id: sourceId,
        source_id_kind: 'top-gym-runtime-array-index-provenance-only',
        source_path: SOURCE_PATHS.anatomy,
        destination_table: DESTINATIONS['anatomy-mappings'],
        version: SEED_VERSION,
        source_version: SOURCE_VERSION,
        relationships: { muscle_seed_key: muscle.seed_key },
        validation: { status: 'unsupported', reason: 'no verified anatomy mapping entry in the audited manifest' },
        record: {
          muscle_seed_key: muscle.seed_key,
          body_region: muscle.record.body_part,
          mapping_status: 'unsupported',
          asset_key: null,
          active: false,
          record_scope: 'canonical-unresolved',
        },
      };
    });

  const foodCategoryValues = sourceFoods.map((record) => ({ value: record.category, scope: 'foods.json' }));
  const foodCategoryRecords = makeLookupRecords({
    values: foodCategoryValues,
    domain: 'food-category',
    destinationTable: DESTINATIONS['food-categories'],
    sourcePaths: [SOURCE_PATHS.foods],
    sourceValueLabel: 'category',
  });
  const foodCategoryByIdentity = new Map(
    foodCategoryRecords.map((record) => [normalizeText(record.record.name_en), record.seed_key]),
  );

  const observedFoodUnitCounts = {};
  for (const food of sourceFoods) observedFoodUnitCounts[food.servingUnit] = (observedFoodUnitCounts[food.servingUnit] || 0) + 1;
  const unitDefinitions = [
    { code: 'gram', sourceAliases: ['gram'], nameEn: 'gram', nameAr: null, dimension: 'mass', source: 'top-gym', sourcePaths: [SOURCE_PATHS.foods] },
    { code: 'kilogram', sourceAliases: [], nameEn: 'kilogram', nameAr: null, dimension: 'mass', source: 'logicfit-product-contract', sourcePaths: ['Master Bible/04_SEED_AND_CANONICAL_DATA/41_FOOD_SEED_SPEC.md'] },
    { code: 'milliliter', sourceAliases: ['ml'], nameEn: 'milliliter', nameAr: null, dimension: 'volume', source: 'top-gym-and-logicfit-product-contract', sourcePaths: [SOURCE_PATHS.foods, 'Master Bible/04_SEED_AND_CANONICAL_DATA/41_FOOD_SEED_SPEC.md'] },
    { code: 'liter', sourceAliases: [], nameEn: 'liter', nameAr: null, dimension: 'volume', source: 'logicfit-product-contract', sourcePaths: ['Master Bible/04_SEED_AND_CANONICAL_DATA/41_FOOD_SEED_SPEC.md'] },
    { code: 'piece', sourceAliases: [], nameEn: 'piece', nameAr: null, dimension: 'count', source: 'logicfit-product-contract', sourcePaths: ['Master Bible/04_SEED_AND_CANONICAL_DATA/41_FOOD_SEED_SPEC.md'] },
    { code: 'serving', sourceAliases: [], nameEn: 'serving', nameAr: null, dimension: 'serving', source: 'logicfit-product-contract', sourcePaths: ['Master Bible/04_SEED_AND_CANONICAL_DATA/41_FOOD_SEED_SPEC.md'] },
  ];
  const unitRecords = unitDefinitions.map((definition) => {
    const identity = { unit_code: definition.code, dimension: definition.dimension };
    const observedCount = definition.sourceAliases.reduce((sum, alias) => sum + (observedFoodUnitCounts[alias] || 0), 0);
    return envelope({
      source: definition.source.startsWith('logicfit') ? 'logicfit-product-contract' : 'top-gym',
      seedKeyValue: seedKey('food-unit', definition.code, identity),
      sourcePath: definition.sourcePaths,
      destinationTable: DESTINATIONS.units,
      relationships: {},
      record: {
        code: definition.code,
        name_en: definition.nameEn,
        name_ar: definition.nameAr,
        dimension: definition.dimension,
        base_quantity: 1,
        base_unit_code: definition.code,
        source_aliases: definition.sourceAliases,
        observed_food_count: observedCount,
        active: true,
        record_scope: 'canonical',
        localized_label_status: definition.nameAr ? 'source-evidenced' : 'unavailable-in-verified-source',
        conversion_scope: 'identity-only-unless-explicit-food-unit-metadata-exists',
      },
      provenance: {
        source_kind: definition.source,
        approved_unit: true,
      },
    });
  });
  const unitBySourceValue = new Map([
    ['gram', unitRecords.find((record) => record.record.code === 'gram')],
    ['ml', unitRecords.find((record) => record.record.code === 'milliliter')],
  ].map(([sourceValue, record]) => [sourceValue, record.seed_key]));
  const unitByCode = new Map(unitRecords.map((record) => [record.record.code, record]));

  const foodRecords = sourceFoods.map((sourceRecord, index) => {
    const categorySeedKey = foodCategoryByIdentity.get(normalizeText(sourceRecord.category));
    const unitSeedKey = unitBySourceValue.get(normalizeText(sourceRecord.servingUnit));
    if (!categorySeedKey) throw new Error(`Food category does not resolve: ${sourceRecord.nameEn}`);
    if (!unitSeedKey) throw new Error(`Food unit does not resolve: ${sourceRecord.nameEn} -> ${sourceRecord.servingUnit}`);
    const identity = {
      name_en: normalizeText(sourceRecord.nameEn),
      serving_unit: normalizeText(sourceRecord.servingUnit),
      category: normalizeText(sourceRecord.category),
      name_ar: normalizeText(sourceRecord.nameAr),
    };
    const nutrients = {
      calories: sourceRecord.calories,
      protein: sourceRecord.protein,
      carbs: sourceRecord.carbs,
      fat: sourceRecord.fat,
      fiber: sourceRecord.fiber,
      sugar: sourceRecord.sugar,
      sodium: sourceRecord.sodium,
    };
    return envelope({
      sourceId: index + 1,
      sourceIdKind: 'top-gym-runtime-array-index-provenance-only',
      sourcePath: SOURCE_PATHS.foods,
      destinationTable: DESTINATIONS.foods,
      seedKeyValue: seedKey('food', sourceRecord.nameEn, identity),
      relationships: { food_category_seed_key: categorySeedKey, serving_unit_seed_key: unitSeedKey },
      record: {
        name_en: sourceRecord.nameEn,
        name_ar: sourceRecord.nameAr,
        slug: slugify(sourceRecord.nameEn),
        category_seed_key: categorySeedKey,
        serving_quantity: sourceRecord.servingSize,
        serving_unit_key: unitSeedKey,
        calculation_quantity: sourceRecord.servingSize,
        calculation_unit_key: unitSeedKey,
        nutrition_basis: {
          quantity: sourceRecord.servingSize,
          unit: sourceRecord.servingUnit,
          source_fields: ['servingSize', 'servingUnit'],
          conversion_status: 'not-required-source-basis-preserved',
        },
        ...nutrients,
        active: true,
        record_scope: 'canonical',
      },
      provenance: {
        source_record_ordinal: index + 1,
        source_id_is_not_identity: true,
        source_field_mapping: {
          name_ar: 'nameAr',
          name_en: 'nameEn',
          category: 'category',
          serving_quantity: 'servingSize',
          serving_unit: 'servingUnit',
          calories: 'calories',
          protein: 'protein',
          carbs: 'carbs',
          fat: 'fat',
          fiber: 'fiber',
          sugar: 'sugar',
          sodium: 'sodium',
        },
      },
    });
  });

  const conversionRecords = unitRecords.map((unit) => {
    const identity = {
      source_unit_seed_key: unit.seed_key,
      destination_unit_seed_key: unit.seed_key,
      conversion_kind: 'identity',
    };
    return envelope({
      source: 'logicfit-product-contract',
      seedKeyValue: seedKey('food-conversion', `${unit.record.code}-to-${unit.record.code}`, identity),
      sourcePath: ['DOCS/PHASE_2/decisions/21_FOOD_UNITS_CONTRACT_DECISION.md', 'DOCS/PHASE_2/10_SEED_CONTRACT.md'],
      destinationTable: null,
      relationships: { source_unit_seed_key: unit.seed_key, destination_unit_seed_key: unit.seed_key },
      record: {
        source_unit_seed_key: unit.seed_key,
        destination_unit_seed_key: unit.seed_key,
        source_unit_code: unit.record.code,
        destination_unit_code: unit.record.code,
        conversion_kind: 'identity',
        factor: 1,
        precision_scale: null,
        rounding_rule: 'exact',
        status: 'supported',
        install_status: 'contract-only-no-destination-table-in-phase-2',
      },
      provenance: { reason: 'same-unit identity is deterministic and does not infer density or serving equivalence' },
    });
  });

  const datasets = [
    dataset({
      name: 'muscle-groups',
      destinationTable: DESTINATIONS['muscle-groups'],
      dependencies: [],
      sourcePaths: [SOURCE_PATHS.muscles],
      records: muscleGroupRecords,
      sourcePolicy: 'Derived only from distinct non-empty TOP GYM muscles.json.bodyPart values; no Arabic group label was invented.',
      metadata: { source_record_count: sourceMuscles.length, derived_unique_count: muscleGroupRecords.length },
    }),
    dataset({
      name: 'muscles',
      destinationTable: DESTINATIONS.muscles,
      dependencies: ['muscle-groups'],
      sourcePaths: [SOURCE_PATHS.muscles, SOURCE_PATHS.muscleAssets],
      records: muscleRecords,
      sourcePolicy: 'All 297 TOP GYM muscle records are preserved; runtime array IDs are provenance only.',
      metadata: { source_record_count: sourceMuscles.length, media_manifest_record_count: muscleAssetsManifest.records?.length || 0 },
    }),
    dataset({
      name: 'equipment',
      destinationTable: DESTINATIONS.equipment,
      dependencies: [],
      sourcePaths: [SOURCE_PATHS.activeExercises, SOURCE_PATHS.legacyExercises],
      records: equipmentRecords,
      sourcePolicy: 'Distinct normalized source labels are preserved; semantic synonyms are not merged without evidence.',
      metadata: { source_value_count: equipmentValues.length, derived_unique_count: equipmentRecords.length },
    }),
    dataset({
      name: 'exercise-categories',
      destinationTable: DESTINATIONS['exercise-categories'],
      dependencies: [],
      sourcePaths: [SOURCE_PATHS.activeExercises, SOURCE_PATHS.legacyExercises],
      records: categoryRecords,
      sourcePolicy: 'Distinct normalized source labels are preserved; active/legacy case variants normalize to one lookup record.',
      metadata: { source_value_count: categoryValues.length, derived_unique_count: categoryRecords.length },
    }),
    dataset({
      name: 'levels',
      destinationTable: DESTINATIONS.levels,
      dependencies: [],
      sourcePaths: [SOURCE_PATHS.activeExercises, SOURCE_PATHS.legacyExercises, SOURCE_PATHS.planLevelUi, SOURCE_PATHS.planLevelService],
      records: levelRecords,
      sourcePolicy: 'ExerciseDifficulty and PlanLevel remain separate; legacy Advanced maps to exercise-difficulty expert only.',
      metadata: { exercise_difficulty_count: 3, plan_level_count: 3 },
    }),
    dataset({
      name: 'exercises',
      destinationTable: DESTINATIONS.exercises,
      dependencies: ['muscles', 'equipment', 'exercise-categories', 'levels'],
      sourcePaths: [SOURCE_PATHS.activeExercises, SOURCE_PATHS.duplicateExerciseDataset, SOURCE_PATHS.legacyExercises, SOURCE_PATHS.exerciseAssets],
      records: canonicalExerciseRecords,
      sourcePolicy: '873 active source records plus 265 legacy source records are preserved by provenance. Five exact active/legacy semantic duplicates are represented once as canonical records with both source references.',
      metadata: {
        source_record_count: active.length + legacy.length,
        active_source_record_count: active.length,
        legacy_source_record_count: legacy.length,
        canonical_record_count: canonicalExerciseRecords.length,
        legacy_compatibility_only_record_count: canonicalExerciseRecords.filter((record) => record.record.catalog_status === 'legacy-compatibility').length,
        merged_legacy_duplicate_source_record_count: canonicalExerciseRecords.filter((record) => record.provenance.legacy_duplicate_merged_into_active).length,
        source_slug_collision_group_count: [...slugGroups.values()].filter((groups) => groups.length > 1).length,
        source_slug_collision_record_count: [...slugGroups.values()].filter((groups) => groups.length > 1).reduce((sum, groups) => sum + groups.length, 0),
        canonical_slug_collision_policy: 'append-first-8-sha256-identity-hex-to-colliding-source-slug',
        duplicate_evidence_verified: true,
      },
    }),
    dataset({
      name: 'anatomy-mappings',
      destinationTable: DESTINATIONS['anatomy-mappings'],
      dependencies: ['muscles'],
      sourcePaths: [SOURCE_PATHS.anatomy],
      records: anatomyRecords,
      unresolved: anatomyUnresolved,
      sourcePolicy: 'Only the 194 verified manifest mappings are installable. The 165 muscles without a verified mapping are explicit unsupported/unresolved metadata and are not inserted.',
      metadata: {
        source_manifest_entry_count: anatomyEntries.length,
        mapped_muscle_count: mappedMuscleIds.size,
        unsupported_muscle_count: anatomyUnresolved.length,
        source_audit_reported_ambiguous_count: 9,
        record_level_ambiguous_count_in_verified_manifest: anatomyEntries.filter(([, value]) => value.confidence !== 'documented').length,
      },
    }),
    dataset({
      name: 'food-categories',
      destinationTable: DESTINATIONS['food-categories'],
      dependencies: [],
      sourcePaths: [SOURCE_PATHS.foods],
      records: foodCategoryRecords,
      sourcePolicy: 'Distinct normalized TOP GYM foods.json.category values; no Arabic category label was invented.',
      metadata: { source_record_count: sourceFoods.length, derived_unique_count: foodCategoryRecords.length },
    }),
    dataset({
      name: 'units',
      destinationTable: DESTINATIONS.units,
      dependencies: [],
      sourcePaths: [SOURCE_PATHS.foods, 'Master Bible/04_SEED_AND_CANONICAL_DATA/41_FOOD_SEED_SPEC.md'],
      records: unitRecords,
      sourcePolicy: 'Canonical approved units are six. TOP GYM evidence covers gram and ml; remaining approved units are metadata-only contract records with no inferred conversions.',
      metadata: { approved_unit_count: unitRecords.length, top_gym_observed_unit_counts: observedFoodUnitCounts },
    }),
    dataset({
      name: 'foods',
      destinationTable: DESTINATIONS.foods,
      dependencies: ['food-categories', 'units'],
      sourcePaths: [SOURCE_PATHS.foods],
      records: foodRecords,
      sourcePolicy: 'All 367 TOP GYM foods are preserved with embedded nutrition values and the exact source serving basis. No separate nutrition-values dataset is created.',
      metadata: { source_record_count: sourceFoods.length, canonical_record_count: foodRecords.length, observed_unit_counts: observedFoodUnitCounts },
    }),
    dataset({
      name: 'food-conversions',
      destinationTable: null,
      dependencies: ['units'],
      sourcePaths: ['DOCS/PHASE_2/decisions/21_FOOD_UNITS_CONTRACT_DECISION.md', 'DOCS/PHASE_2/10_SEED_CONTRACT.md'],
      records: conversionRecords,
      sourcePolicy: 'Only same-unit identity conversions are represented. Cross-unit, serving, piece, and density conversions are explicitly unsupported until source metadata is approved.',
      metadata: {
        supported_identity_conversion_count: conversionRecords.length,
        unsupported_cross_unit_policy: 'validation-error-no-implicit-conversion',
        installed: false,
      },
    }),
  ];

  ensureDir(SEED_ROOT);
  ensureDir(DATASET_ROOT);
  for (const item of datasets) writeJson(path.join(DATASET_ROOT, `${item.dataset}.json`), item);

  const manifestDatasets = datasets.map((item) => {
    const relativeFile = `v1/${item.dataset}.json`;
    const absoluteFile = path.join(SEED_ROOT, relativeFile);
    return {
      dataset: item.dataset,
      file: relativeFile,
      seed_version: SEED_VERSION,
      schema_version: item.schema_version,
      destination_table: item.destination_table,
      dependencies: item.dependencies,
      record_count: item.records.length,
      unresolved_count: item.unresolved?.length || 0,
      source_record_count: item.metadata?.source_record_count ?? item.metadata?.source_value_count ?? item.metadata?.source_manifest_entry_count ?? item.records.length,
      source_paths: item.source_paths,
      checksum_sha256: sha256(fs.readFileSync(absoluteFile, 'utf8')),
      validation_status: 'generated-pending-validator',
    };
  });
  writeJson(path.join(SEED_ROOT, 'manifest.json'), {
    schema_version: 'logicfit.seed.manifest.v1',
    seed_version: SEED_VERSION,
    generated_at_utc: GENERATED_AT,
    validator_version: 'logicfit-seed-validator-1.0.0',
    source: {
      system: 'TOP GYM',
      audit_version: SOURCE_VERSION,
      source_root_alias: 'TOP_GYM_ROOT',
      source_root: TOP_GYM_ROOT,
      operational_data_excluded: true,
    },
    install_order: datasets.map((item) => item.dataset),
    datasets: manifestDatasets,
    identity_policy: {
      algorithm: 'NFKC + locale-independent lowercase + normalized semantic identity + SHA-256 first 12 hex + human slug',
      numeric_source_ids_are_provenance_only: true,
      json_order_is_not_identity: true,
      generation_date_is_not_identity: true,
    },
  });

  return { datasets, manifest: manifestDatasets };
}

try {
  const result = buildSeedPackage();
  console.log(JSON.stringify({
    status: 'GREEN',
    seed_version: SEED_VERSION,
    datasets: result.manifest.map((item) => ({ dataset: item.dataset, record_count: item.record_count, unresolved_count: item.unresolved_count })),
    output: path.relative(PROJECT_ROOT, SEED_ROOT),
  }, null, 2));
} catch (error) {
  console.error(`SEED_GENERATION_FAILED: ${error.message}`);
  process.exitCode = 1;
}
