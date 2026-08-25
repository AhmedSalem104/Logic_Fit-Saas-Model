/* Documentation/manifest consistency gate for Phase 3. */

const fs = require('fs');
const path = require('path');

const PROJECT_ROOT = path.resolve(__dirname, '..', '..');
const DOC_ROOT = path.join(PROJECT_ROOT, 'DOCS', 'PHASE_3');
const SEED_ROOT = path.join(PROJECT_ROOT, 'database', 'seeds');
const requiredDocs = [
  '00_PHASE_3_SCOPE.md',
  '01_CANONICAL_DATA_SOURCES.md',
  '02_SEED_MANIFEST.md',
  '03_EXERCISE_SEED_MAPPING.md',
  '04_MUSCLE_SEED_MAPPING.md',
  '05_FOOD_SEED_MAPPING.md',
  '06_UNIT_CONVERSION_MAPPING.md',
  '07_ANATOMY_SEED_MAPPING.md',
  '08_SEED_VALIDATION.md',
  '09_SEED_RUNNER.md',
  '10_SEED_TEST_RESULTS.md',
  '11_SEED_GAPS.md',
  'PHASE_3_STATUS_REPORT.md',
];
const expectedDatasets = ['muscle-groups', 'muscles', 'equipment', 'exercise-categories', 'levels', 'exercises', 'anatomy-mappings', 'food-categories', 'units', 'foods', 'food-conversions'];
const errors = [];
const warnings = [];

function fail(message) { errors.push(message); }
for (const name of requiredDocs) if (!fs.existsSync(path.join(DOC_ROOT, name))) fail(`Missing required Phase 3 document: ${name}`);

const manifestPath = path.join(SEED_ROOT, 'manifest.json');
if (!fs.existsSync(manifestPath)) fail('Missing database/seeds/manifest.json');
else {
  const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
  if (manifest.validation?.status !== 'GREEN') fail('Manifest validation status is not GREEN.');
  if (JSON.stringify(manifest.install_order) !== JSON.stringify(expectedDatasets)) fail('Manifest install order is inconsistent with the approved Phase 3 order.');
  for (const item of manifest.datasets || []) {
    if (!fs.existsSync(path.join(SEED_ROOT, item.file))) fail(`Manifest dataset file missing: ${item.file}`);
    if (!expectedDatasets.includes(item.dataset)) fail(`Unexpected manifest dataset: ${item.dataset}`);
  }
}

const phase3Text = requiredDocs.map((name) => fs.existsSync(path.join(DOC_ROOT, name)) ? fs.readFileSync(path.join(DOC_ROOT, name), 'utf8') : '').join('\n');
if (phase3Text.includes('BLOCKED: DATA DECISION REQUIRED')) fail('Phase 3 documentation contains an unresolved data decision blocker.');
if (!phase3Text.includes('1,138') || !phase3Text.includes('297') || !phase3Text.includes('367')) fail('Phase 3 documentation does not contain the required source-count evidence.');
if (!phase3Text.includes('LogicFit_SeedValidation_v1_03')) warnings.push('Local test database name was not repeated in every Phase 3 document; status report coverage is sufficient.');

const statusReport = fs.readFileSync(path.join(DOC_ROOT, 'PHASE_3_STATUS_REPORT.md'), 'utf8');
if (!statusReport.includes('GREEN — DONE')) fail('Phase 3 status report is not GREEN — DONE.');
const seedContract = fs.readFileSync(path.join(PROJECT_ROOT, 'DOCS', 'PHASE_2', '10_SEED_CONTRACT.md'), 'utf8');
if (!seedContract.includes('realized by the Phase 3 package')) fail('Phase 2 seed contract status was not synchronized.');
const canonicalMapping = fs.readFileSync(path.join(PROJECT_ROOT, 'DOCS', 'TOP_GYM_LOGICFIT_CANONICAL_MAPPING.md'), 'utf8');
if (!canonicalMapping.includes('canonical seed package realized in Phase 3')) fail('Canonical mapping follow-on was not synchronized.');

const result = { status: errors.length === 0 ? 'GREEN' : 'RED', error_count: errors.length, warning_count: warnings.length, errors, warnings };
console.log(JSON.stringify(result, null, 2));
if (errors.length > 0) process.exitCode = 1;

