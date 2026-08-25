const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..', '..');
const phaseRoot = path.join(root, 'DOCS', 'PHASE_4');
const docs = [
  '00_PHASE_4_SCOPE.md', '01_REPOSITORY_ARCHITECTURE.md', '02_BACKEND_FOUNDATION.md',
  '03_DATABASE_FOUNDATION.md', '04_MIGRATION_FOUNDATION.md', '05_SEED_FOUNDATION.md',
  '06_WEB_FOUNDATION.md', '07_FLUTTER_FOUNDATION.md', '08_AUTH_FOUNDATION.md',
  '09_SECURITY_FOUNDATION.md', '10_OBSERVABILITY_FOUNDATION.md', '11_LOCAL_DEVELOPMENT.md',
  '12_TESTING_FOUNDATION.md', '13_ENVIRONMENT_CONFIGURATION.md', 'PHASE_4_STATUS_REPORT.md',
];
const errors = [];
for (const doc of docs) if (!fs.existsSync(path.join(phaseRoot, doc))) errors.push(`Missing Phase 4 document: ${doc}`);
const text = docs.map((doc) => fs.existsSync(path.join(phaseRoot, doc)) ? fs.readFileSync(path.join(phaseRoot, doc), 'utf8') : '').join('\n');
if (!text.includes('GREEN')) errors.push('Phase 4 documentation does not record GREEN.');
if (!fs.existsSync(path.join(root, '.env.example'))) errors.push('Missing .env.example.');
for (const required of [
  'LogicFit.sln',
  'src/LogicFit.Api/LogicFit.Api.csproj',
  'src/LogicFit.Application/LogicFit.Application.csproj',
  'src/LogicFit.Domain/LogicFit.Domain.csproj',
  'src/LogicFit.Infrastructure/LogicFit.Infrastructure.csproj',
  'src/LogicFit.Shared/LogicFit.Shared.csproj',
  'src/LogicFit.Infrastructure/Persistence/Migrations/ControlPlane/20260825144155_InitialControlPlaneFoundation.cs',
  'src/LogicFit.Infrastructure/Persistence/Migrations/Gym/20260825144011_InitialGymFoundation.cs',
  'apps/web/src/App.tsx', 'apps/mobile/lib/main.dart', 'database/seeds/manifest.json',
]) if (!fs.existsSync(path.join(root, required))) errors.push(`Missing foundation artifact: ${required}`);
if (!text.includes('Phase 5') || !text.includes('not started')) errors.push('Phase 4 stop condition is not documented.');
if (text.includes('BLOCKED: SPECIFICATION GAP')) errors.push('Phase 4 docs contain a specification blocker.');
const result = { status: errors.length === 0 ? 'GREEN' : 'RED', error_count: errors.length, errors };
console.log(JSON.stringify(result, null, 2));
if (errors.length) process.exitCode = 1;
