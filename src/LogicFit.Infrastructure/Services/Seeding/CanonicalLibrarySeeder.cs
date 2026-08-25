using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Services.Seeding;

public sealed class CanonicalLibrarySeeder(
    GymDbContext db,
    CanonicalSeedManifestReader manifestReader)
{
    private static readonly IReadOnlyDictionary<string, string> DatasetFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["muscle-groups"] = "muscle-groups.json",
        ["muscles"] = "muscles.json",
        ["equipment"] = "equipment.json",
        ["exercise-categories"] = "exercise-categories.json",
        ["levels"] = "levels.json",
        ["exercises"] = "exercises.json",
        ["anatomy-mappings"] = "anatomy-mappings.json",
        ["food-categories"] = "food-categories.json",
        ["units"] = "units.json",
        ["foods"] = "foods.json",
        ["food-conversions"] = "food-conversions.json"
    };

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        var manifest = manifestReader.Read();
        var documents = LoadDocuments(manifest);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await ApplyMuscleGroupsAsync(documents["muscle-groups"], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        var muscleGroups = await db.MuscleGroups.AsNoTracking().ToDictionaryAsync(x => x.SeedKey, x => x.MuscleGroupId, StringComparer.Ordinal, cancellationToken);

        await ApplyMusclesAsync(documents["muscles"], muscleGroups, cancellationToken);
        await ApplyEquipmentAsync(documents["equipment"], cancellationToken);
        await ApplyExerciseCategoriesAsync(documents["exercise-categories"], cancellationToken);
        await ApplyLevelsAsync(documents["levels"], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var muscles = await db.Muscles.AsNoTracking().ToDictionaryAsync(x => x.SeedKey, x => x.MuscleId, StringComparer.Ordinal, cancellationToken);
        var equipment = await db.Equipment.AsNoTracking().ToDictionaryAsync(x => x.SeedKey, x => x.EquipmentId, StringComparer.Ordinal, cancellationToken);
        var categories = await db.ExerciseCategories.AsNoTracking().ToDictionaryAsync(x => x.SeedKey, x => x.CategoryId, StringComparer.Ordinal, cancellationToken);
        var levels = await db.Levels.AsNoTracking().ToDictionaryAsync(x => x.SeedKey, x => x.LevelId, StringComparer.Ordinal, cancellationToken);

        await ApplyExercisesAsync(documents["exercises"], muscles, equipment, categories, levels, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await ApplyAnatomyMappingsAsync(documents["anatomy-mappings"], muscles, cancellationToken);
        await ApplyFoodCategoriesAsync(documents["food-categories"], cancellationToken);
        await ApplyFoodUnitsAsync(documents["units"], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var foodCategories = await db.FoodCategories.AsNoTracking().ToDictionaryAsync(x => x.SeedKey, x => x.FoodCategoryId, StringComparer.Ordinal, cancellationToken);
        var units = await db.FoodUnits.AsNoTracking().ToDictionaryAsync(x => x.SeedKey, x => x.FoodUnitId, StringComparer.Ordinal, cancellationToken);
        await ApplyFoodsAsync(documents["foods"], foodCategories, units, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await ApplyInstallationsAsync(manifest, documents, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ApplyMuscleGroupsAsync(IReadOnlyList<JsonElement> documents, CancellationToken cancellationToken)
    {
        var existing = await db.MuscleGroups.ToDictionaryAsync(x => x.SeedKey, StringComparer.Ordinal, cancellationToken);
        foreach (var item in documents)
        {
            var key = Required(item, "seed_key");
            var record = RequiredObject(item, "record");
            var entity = existing.GetValueOrDefault(key);
            if (entity is null)
            {
                entity = new MuscleGroupEntity { MuscleGroupId = StableSeedGuid.For(key) };
                db.MuscleGroups.Add(entity);
                existing[key] = entity;
            }

            ApplyReference(entity, item, record);
        }
    }

    private async Task ApplyMusclesAsync(IReadOnlyList<JsonElement> documents, IReadOnlyDictionary<string, Guid> groups, CancellationToken cancellationToken)
    {
        var existing = await db.Muscles.ToDictionaryAsync(x => x.SeedKey, StringComparer.Ordinal, cancellationToken);
        foreach (var item in documents)
        {
            var key = Required(item, "seed_key");
            var record = RequiredObject(item, "record");
            var groupKey = Required(record, "muscle_group_seed_key");
            if (!groups.TryGetValue(groupKey, out var groupId))
            {
                throw new InvalidDataException($"Muscle {key} references missing muscle group {groupKey}.");
            }

            var entity = existing.GetValueOrDefault(key);
            if (entity is null)
            {
                entity = new MuscleEntity { MuscleId = StableSeedGuid.For(key) };
                db.Muscles.Add(entity);
                existing[key] = entity;
            }

            ApplyReference(entity, item, record);
            entity.MuscleGroupId = groupId;
            entity.NameAr = Optional(record, "name_ar");
        }
    }

    private async Task ApplyEquipmentAsync(IReadOnlyList<JsonElement> documents, CancellationToken cancellationToken)
    {
        var existing = await db.Equipment.ToDictionaryAsync(x => x.SeedKey, StringComparer.Ordinal, cancellationToken);
        foreach (var item in documents)
        {
            var key = Required(item, "seed_key");
            var record = RequiredObject(item, "record");
            var entity = existing.GetValueOrDefault(key);
            if (entity is null)
            {
                entity = new EquipmentEntity { EquipmentId = StableSeedGuid.For(key) };
                db.Equipment.Add(entity);
                existing[key] = entity;
            }

            ApplyReference(entity, item, record);
        }
    }

    private async Task ApplyExerciseCategoriesAsync(IReadOnlyList<JsonElement> documents, CancellationToken cancellationToken)
    {
        var existing = await db.ExerciseCategories.ToDictionaryAsync(x => x.SeedKey, StringComparer.Ordinal, cancellationToken);
        foreach (var item in documents)
        {
            var key = Required(item, "seed_key");
            var record = RequiredObject(item, "record");
            var entity = existing.GetValueOrDefault(key);
            if (entity is null)
            {
                entity = new ExerciseCategoryEntity { CategoryId = StableSeedGuid.For(key) };
                db.ExerciseCategories.Add(entity);
                existing[key] = entity;
            }

            ApplyReference(entity, item, record);
        }
    }

    private async Task ApplyLevelsAsync(IReadOnlyList<JsonElement> documents, CancellationToken cancellationToken)
    {
        var existing = await db.Levels.ToDictionaryAsync(x => x.SeedKey, StringComparer.Ordinal, cancellationToken);
        foreach (var item in documents)
        {
            var key = Required(item, "seed_key");
            var record = RequiredObject(item, "record");
            var entity = existing.GetValueOrDefault(key);
            if (entity is null)
            {
                entity = new LevelEntity { LevelId = StableSeedGuid.For(key) };
                db.Levels.Add(entity);
                existing[key] = entity;
            }

            ApplyReference(entity, item, record);
            entity.Code = Required(record, "code");
            entity.LevelType = Required(record, "level_type");
        }
    }

    private async Task ApplyExercisesAsync(
        IReadOnlyList<JsonElement> documents,
        IReadOnlyDictionary<string, Guid> muscles,
        IReadOnlyDictionary<string, Guid> equipment,
        IReadOnlyDictionary<string, Guid> categories,
        IReadOnlyDictionary<string, Guid> levels,
        CancellationToken cancellationToken)
    {
        var existing = await db.Exercises.ToDictionaryAsync(x => x.SeedKey, StringComparer.Ordinal, cancellationToken);
        foreach (var item in documents)
        {
            var key = Required(item, "seed_key");
            var record = RequiredObject(item, "record");
            var muscleKey = Required(record, "primary_muscle_seed_key");
            var categoryKey = Required(record, "category_seed_key");
            var levelKey = Required(record, "level_seed_key");
            if (!muscles.TryGetValue(muscleKey, out var muscleId) || !categories.TryGetValue(categoryKey, out var categoryId) || !levels.TryGetValue(levelKey, out var levelId))
            {
                throw new InvalidDataException($"Exercise {key} has an unresolved reference.");
            }

            var entity = existing.GetValueOrDefault(key);
            if (entity is null)
            {
                entity = new ExerciseEntity { ExerciseId = StableSeedGuid.For(key) };
                db.Exercises.Add(entity);
                existing[key] = entity;
            }

            ApplyReference(entity, item, record);
            entity.Slug = Required(record, "slug");
            entity.Description = Optional(record, "description_en");
            entity.PrimaryMuscleId = muscleId;
            entity.DifficultyCode = Required(record, "difficulty_code");
            entity.CategoryId = categoryId;
            entity.LevelId = levelId;
            entity.MovementPattern = Optional(record, "movement_pattern");
            entity.InstructionsAr = JoinStrings(record, "instructions_ar");
            entity.InstructionsEn = JoinStrings(record, "instructions_en");
            entity.TipsAr = JoinStrings(record, "tips_ar");
            entity.CommonMistakesAr = JoinStrings(record, "common_mistakes_ar");
            entity.MediaJson = RawOrNull(record, "media");
            entity.LegacyStatus = Optional(record, "catalog_status");

            var secondary = ArrayObjects(record, "secondary_muscles");
            var sortOrder = 0;
            foreach (var association in secondary)
            {
                var secondaryKey = Required(association, "muscle_seed_key");
                if (!muscles.TryGetValue(secondaryKey, out var secondaryId))
                {
                    throw new InvalidDataException($"Exercise {key} references missing secondary muscle {secondaryKey}.");
                }

                var role = Optional(association, "role") ?? "secondary";
                var exists = await db.ExerciseMuscles.AnyAsync(x => x.ExerciseId == entity.ExerciseId && x.MuscleId == secondaryId && x.Role == role, cancellationToken);
                if (!exists)
                {
                    db.ExerciseMuscles.Add(new ExerciseMuscleEntity
                    {
                        ExerciseMuscleId = StableSeedGuid.For($"{key}:muscle:{secondaryKey}:{role}"),
                        ExerciseId = entity.ExerciseId,
                        MuscleId = secondaryId,
                        Role = role,
                        SortOrder = sortOrder++,
                        PayloadJson = association.GetRawText()
                    });
                }
            }

            foreach (var equipmentKey in ArrayStrings(record, "equipment_seed_keys"))
            {
                if (!equipment.TryGetValue(equipmentKey, out var equipmentId))
                {
                    throw new InvalidDataException($"Exercise {key} references missing equipment {equipmentKey}.");
                }

                var exists = await db.ExerciseEquipment.AnyAsync(x => x.ExerciseId == entity.ExerciseId && x.EquipmentId == equipmentId, cancellationToken);
                if (!exists)
                {
                    db.ExerciseEquipment.Add(new ExerciseEquipmentEntity
                    {
                        ExerciseEquipmentId = StableSeedGuid.For($"{key}:equipment:{equipmentKey}"),
                        ExerciseId = entity.ExerciseId,
                        EquipmentId = equipmentId,
                        PayloadJson = JsonSerializer.Serialize(new { equipment_seed_key = equipmentKey })
                    });
                }
            }
        }
    }

    private async Task ApplyAnatomyMappingsAsync(IReadOnlyList<JsonElement> documents, IReadOnlyDictionary<string, Guid> muscles, CancellationToken cancellationToken)
    {
        var existing = await db.AnatomyMappings.ToDictionaryAsync(x => x.SeedKey, StringComparer.Ordinal, cancellationToken);
        foreach (var item in documents)
        {
            var key = Required(item, "seed_key");
            var record = RequiredObject(item, "record");
            var muscleKey = Required(record, "muscle_seed_key");
            if (!muscles.TryGetValue(muscleKey, out var muscleId))
            {
                throw new InvalidDataException($"Anatomy mapping {key} references missing muscle {muscleKey}.");
            }

            var entity = existing.GetValueOrDefault(key);
            if (entity is null)
            {
                entity = new AnatomyMappingEntity { AnatomyMappingId = StableSeedGuid.For(key) };
                db.AnatomyMappings.Add(entity);
                existing[key] = entity;
            }

            ApplyReference(entity, item, record);
            entity.NameEn = NestedRequired(item, "provenance", "system_name");
            entity.NameAr = NestedOptional(item, "provenance", "system_name_ar");
            entity.MuscleId = muscleId;
            entity.BodyRegion = Required(record, "body_region");
            entity.ViewCode = Required(record, "view");
            entity.AssetKey = Required(record, "asset_key");
        }
    }

    private async Task ApplyFoodCategoriesAsync(IReadOnlyList<JsonElement> documents, CancellationToken cancellationToken)
    {
        var existing = await db.FoodCategories.ToDictionaryAsync(x => x.SeedKey, StringComparer.Ordinal, cancellationToken);
        foreach (var item in documents)
        {
            var key = Required(item, "seed_key");
            var record = RequiredObject(item, "record");
            var entity = existing.GetValueOrDefault(key);
            if (entity is null)
            {
                entity = new FoodCategoryEntity { FoodCategoryId = StableSeedGuid.For(key) };
                db.FoodCategories.Add(entity);
                existing[key] = entity;
            }

            ApplyReference(entity, item, record);
        }
    }

    private async Task ApplyFoodUnitsAsync(IReadOnlyList<JsonElement> documents, CancellationToken cancellationToken)
    {
        var existing = await db.FoodUnits.ToDictionaryAsync(x => x.SeedKey, StringComparer.Ordinal, cancellationToken);
        foreach (var item in documents)
        {
            var key = Required(item, "seed_key");
            var record = RequiredObject(item, "record");
            var entity = existing.GetValueOrDefault(key);
            if (entity is null)
            {
                entity = new FoodUnitEntity { FoodUnitId = StableSeedGuid.For(key) };
                db.FoodUnits.Add(entity);
                existing[key] = entity;
            }

            ApplyReference(entity, item, record);
            entity.Code = Required(record, "code");
            entity.Dimension = Required(record, "dimension");
            entity.BaseQuantity = Decimal(record, "base_quantity");
            entity.BaseUnitCode = Required(record, "base_unit_code");
        }
    }

    private async Task ApplyFoodsAsync(IReadOnlyList<JsonElement> documents, IReadOnlyDictionary<string, Guid> categories, IReadOnlyDictionary<string, Guid> units, CancellationToken cancellationToken)
    {
        var existing = await db.Foods.ToDictionaryAsync(x => x.SeedKey, StringComparer.Ordinal, cancellationToken);
        foreach (var item in documents)
        {
            var key = Required(item, "seed_key");
            var record = RequiredObject(item, "record");
            var categoryKey = Required(record, "category_seed_key");
            var servingUnitKey = Required(record, "serving_unit_key");
            var calculationUnitKey = Required(record, "calculation_unit_key");
            if (!categories.TryGetValue(categoryKey, out var categoryId) || !units.TryGetValue(servingUnitKey, out var servingUnitId) || !units.TryGetValue(calculationUnitKey, out var calculationUnitId))
            {
                throw new InvalidDataException($"Food {key} has an unresolved category or unit reference.");
            }

            var entity = existing.GetValueOrDefault(key);
            if (entity is null)
            {
                entity = new FoodEntity { FoodId = StableSeedGuid.For(key) };
                db.Foods.Add(entity);
                existing[key] = entity;
            }

            ApplyReference(entity, item, record);
            entity.FoodCategoryId = categoryId;
            entity.Slug = Required(record, "slug");
            entity.ServingQuantity = Decimal(record, "serving_quantity");
            entity.ServingUnitId = servingUnitId;
            entity.CalculationQuantity = Decimal(record, "calculation_quantity");
            entity.CalculationUnitId = calculationUnitId;
            entity.Calories = Decimal(record, "calories");
            entity.Protein = Decimal(record, "protein");
            entity.Carbs = Decimal(record, "carbs");
            entity.Fat = Decimal(record, "fat");
            entity.Fiber = OptionalDecimal(record, "fiber");
            entity.Sugar = OptionalDecimal(record, "sugar");
            entity.Sodium = OptionalDecimal(record, "sodium");
        }
    }

    private async Task ApplyInstallationsAsync(CanonicalSeedManifest manifest, IReadOnlyDictionary<string, IReadOnlyList<JsonElement>> documents, CancellationToken cancellationToken)
    {
        var existing = await db.SeedInstallations.ToDictionaryAsync(x => $"{x.SeedDomain}:{x.SeedVersion}", StringComparer.Ordinal, cancellationToken);
        foreach (var dataset in manifest.Datasets)
        {
            var key = $"{dataset.Dataset}:{dataset.SeedVersionOrDefault()}";
            var entity = existing.GetValueOrDefault(key);
            if (entity is null)
            {
                entity = new SeedInstallationEntity { InstallationId = StableSeedGuid.For($"installation:{key}") };
                db.SeedInstallations.Add(entity);
                existing[key] = entity;
            }

            var records = documents[dataset.Dataset];
            var path = Path.Combine(manifestReader.GetSeedRoot(), DatasetFiles[dataset.Dataset]);
            entity.SeedDomain = dataset.Dataset;
            entity.SeedVersion = dataset.SeedVersionOrDefault();
            entity.SourceVersion = records.Count == 0 ? "logicfit-canonical-seed" : Optional(records[0], "source_version") ?? "logicfit-canonical-seed";
            entity.ChecksumSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
            entity.RecordCount = dataset.RecordCount;
            entity.Status = dataset.Dataset.Equals("food-conversions", StringComparison.OrdinalIgnoreCase) ? "contract-only" : "installed";
            entity.UpdatedAtUtc = DateTime.UtcNow;
            if (entity.InstalledAtUtc == default)
            {
                entity.InstalledAtUtc = DateTime.UtcNow;
            }
        }
    }

    private IReadOnlyDictionary<string, IReadOnlyList<JsonElement>> LoadDocuments(CanonicalSeedManifest manifest)
    {
        var root = manifestReader.GetSeedRoot();
        var documents = new Dictionary<string, IReadOnlyList<JsonElement>>(StringComparer.OrdinalIgnoreCase);
        foreach (var dataset in manifest.Datasets)
        {
            if (!DatasetFiles.TryGetValue(dataset.Dataset, out var fileName))
            {
                throw new InvalidDataException($"No .NET seed mapping exists for dataset {dataset.Dataset}.");
            }

            var path = Path.Combine(root, fileName);
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            var records = document.RootElement.GetProperty("records").EnumerateArray().Select(x => x.Clone()).ToArray();
            if (records.Length != dataset.RecordCount)
            {
                throw new InvalidDataException($"Dataset {dataset.Dataset} manifest count {dataset.RecordCount} does not match file count {records.Length}.");
            }

            documents.Add(dataset.Dataset, records);
        }

        return documents;
    }

    private static void ApplyReference(ILibraryReference entity, JsonElement item, JsonElement record)
    {
        entity.SeedKey = Required(item, "seed_key");
        entity.NameAr = Optional(record, "name_ar");
        // Anatomy mappings carry their display name in provenance.system_name;
        // the canonical source does not duplicate it as record.name_en.
        entity.NameEn = Optional(record, "name_en")
            ?? NestedOptional(item, "provenance", "system_name")
            ?? throw new InvalidDataException("Required seed property name_en (or provenance.system_name) is missing.");
        entity.Active = Boolean(record, "active");
        entity.RecordScope = Required(record, "record_scope");
        entity.Source = Optional(item, "source") ?? "top-gym";
        entity.SourceVersion = Optional(item, "source_version") ?? "top-gym-audit-2026-08-25";
        entity.PayloadJson = item.GetRawText();
    }

    private static JsonElement RequiredObject(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Object
            ? value
            : throw new InvalidDataException($"Required object {property} is missing.");

    private static string Required(JsonElement element, string property)
        => Optional(element, property) ?? throw new InvalidDataException($"Required seed property {property} is missing.");

    private static string? Optional(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static string NestedRequired(JsonElement element, string objectProperty, string property)
        => NestedOptional(element, objectProperty, property) ?? throw new InvalidDataException($"Required nested seed property {objectProperty}.{property} is missing.");

    private static string? NestedOptional(JsonElement element, string objectProperty, string property)
        => element.TryGetProperty(objectProperty, out var nested) && nested.ValueKind == JsonValueKind.Object ? Optional(nested, property) : null;

    private static bool Boolean(JsonElement element, string property)
        => element.TryGetProperty(property, out var value)
            && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False && value.GetBoolean());

    private static decimal Decimal(JsonElement element, string property)
        => OptionalDecimal(element, property) ?? throw new InvalidDataException($"Required numeric seed property {property} is missing.");

    private static decimal? OptionalDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        return decimal.TryParse(value.ToString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static string? JoinStrings(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
            ? string.Join("\n", value.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)))
            : Optional(element, property);

    private static string? RawOrNull(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined ? value.GetRawText() : null;

    private static IReadOnlyList<string> ArrayStrings(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray()
            : [];

    private static IReadOnlyList<JsonElement> ArrayObjects(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object).Select(x => x.Clone()).ToArray()
            : [];
}

internal static class CanonicalSeedDatasetExtensions
{
    public static string SeedVersionOrDefault(this CanonicalSeedDataset dataset) => "v1";
}

internal static class StableSeedGuid
{
    public static Guid For(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"logicfit:seed:{value}"));
        var guidBytes = bytes[..16];
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }
}
