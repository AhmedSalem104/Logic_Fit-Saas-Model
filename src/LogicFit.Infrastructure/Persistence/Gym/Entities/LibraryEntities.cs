namespace LogicFit.Infrastructure.Persistence.Entities;

public sealed class SeedInstallationEntity
{
    public Guid InstallationId { get; set; }
    public string SeedDomain { get; set; } = string.Empty;
    public string SeedVersion { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public string ChecksumSha256 { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public string Status { get; set; } = "contract-only";
    public DateTime InstalledAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public interface ILibraryReference
{
    string SeedKey { get; set; }
    string? NameAr { get; set; }
    string NameEn { get; set; }
    bool Active { get; set; }
    string RecordScope { get; set; }
    string Source { get; set; }
    string SourceVersion { get; set; }
    string PayloadJson { get; set; }
}

public sealed class MuscleGroupEntity : ILibraryReference
{
    public Guid MuscleGroupId { get; set; }
    public string SeedKey { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string RecordScope { get; set; } = "canonical";
    public string Source { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";

    public ICollection<MuscleEntity> Muscles { get; } = new List<MuscleEntity>();
}

public sealed class MuscleEntity : ILibraryReference
{
    public Guid MuscleId { get; set; }
    public Guid MuscleGroupId { get; set; }
    public string SeedKey { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string RecordScope { get; set; } = "canonical";
    public string Source { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";

    public MuscleGroupEntity? MuscleGroup { get; set; }
}

public sealed class EquipmentEntity : ILibraryReference
{
    public Guid EquipmentId { get; set; }
    public string SeedKey { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string RecordScope { get; set; } = "canonical";
    public string Source { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
}

public sealed class ExerciseCategoryEntity : ILibraryReference
{
    public Guid CategoryId { get; set; }
    public string SeedKey { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string RecordScope { get; set; } = "canonical";
    public string Source { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
}

public sealed class LevelEntity : ILibraryReference
{
    public Guid LevelId { get; set; }
    public string SeedKey { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string LevelType { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string RecordScope { get; set; } = "canonical";
    public string Source { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
}

public sealed class ExerciseEntity : ILibraryReference
{
    public Guid ExerciseId { get; set; }
    public string SeedKey { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string RecordScope { get; set; } = "canonical";
    public string Source { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid PrimaryMuscleId { get; set; }
    public string DifficultyCode { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid LevelId { get; set; }
    public string? MovementPattern { get; set; }
    public string? InstructionsAr { get; set; }
    public string? InstructionsEn { get; set; }
    public string? TipsAr { get; set; }
    public string? CommonMistakesAr { get; set; }
    public string? MediaJson { get; set; }
    public string? LegacyStatus { get; set; }

    public MuscleEntity? PrimaryMuscle { get; set; }
    public ExerciseCategoryEntity? Category { get; set; }
    public LevelEntity? Level { get; set; }
    public ICollection<ExerciseMuscleEntity> Muscles { get; } = new List<ExerciseMuscleEntity>();
    public ICollection<ExerciseEquipmentEntity> Equipment { get; } = new List<ExerciseEquipmentEntity>();
}

public sealed class ExerciseMuscleEntity
{
    public Guid ExerciseMuscleId { get; set; }
    public Guid ExerciseId { get; set; }
    public Guid MuscleId { get; set; }
    public string Role { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string PayloadJson { get; set; } = "{}";

    public ExerciseEntity? Exercise { get; set; }
    public MuscleEntity? Muscle { get; set; }
}

public sealed class ExerciseEquipmentEntity
{
    public Guid ExerciseEquipmentId { get; set; }
    public Guid ExerciseId { get; set; }
    public Guid EquipmentId { get; set; }
    public bool? IsRequired { get; set; }
    public string PayloadJson { get; set; } = "{}";

    public ExerciseEntity? Exercise { get; set; }
    public EquipmentEntity? Equipment { get; set; }
}

public sealed class AnatomyMappingEntity : ILibraryReference
{
    public Guid AnatomyMappingId { get; set; }
    public string SeedKey { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string RecordScope { get; set; } = "canonical";
    public string Source { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public Guid MuscleId { get; set; }
    public string BodyRegion { get; set; } = string.Empty;
    public string ViewCode { get; set; } = string.Empty;
    public string AssetKey { get; set; } = string.Empty;

    public MuscleEntity? Muscle { get; set; }
}

public sealed class FoodCategoryEntity : ILibraryReference
{
    public Guid FoodCategoryId { get; set; }
    public string SeedKey { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string RecordScope { get; set; } = "canonical";
    public string Source { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
}

public sealed class FoodUnitEntity : ILibraryReference
{
    public Guid FoodUnitId { get; set; }
    public string SeedKey { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public decimal BaseQuantity { get; set; }
    public string BaseUnitCode { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string RecordScope { get; set; } = "canonical";
    public string Source { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
}

public sealed class FoodEntity : ILibraryReference
{
    public Guid FoodId { get; set; }
    public string SeedKey { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string RecordScope { get; set; } = "canonical";
    public string Source { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public Guid FoodCategoryId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public decimal ServingQuantity { get; set; }
    public Guid ServingUnitId { get; set; }
    public decimal CalculationQuantity { get; set; }
    public Guid CalculationUnitId { get; set; }
    public decimal Calories { get; set; }
    public decimal Protein { get; set; }
    public decimal Carbs { get; set; }
    public decimal Fat { get; set; }
    public decimal? Fiber { get; set; }
    public decimal? Sugar { get; set; }
    public decimal? Sodium { get; set; }

    public FoodCategoryEntity? Category { get; set; }
    public FoodUnitEntity? ServingUnit { get; set; }
    public FoodUnitEntity? CalculationUnit { get; set; }
}
