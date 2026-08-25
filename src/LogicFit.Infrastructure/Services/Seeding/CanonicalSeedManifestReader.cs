using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace LogicFit.Infrastructure.Services.Seeding;

public sealed record CanonicalSeedDataset(string Dataset, int RecordCount, int UnresolvedCount);
public sealed record CanonicalSeedManifest(string SeedVersion, IReadOnlyList<CanonicalSeedDataset> Datasets)
{
    public int TotalRecordCount => Datasets.Sum(x => x.RecordCount);
}

public sealed class CanonicalSeedManifestReader(IHostEnvironment hostEnvironment)
{
    public string GetSeedRoot()
    {
        var manifestPath = ResolveManifestPath();
        var directory = Path.GetDirectoryName(manifestPath)
            ?? throw new InvalidDataException("The canonical seed manifest path has no directory.");
        var v1Directory = Path.Combine(directory, "v1");
        return Directory.Exists(v1Directory) ? v1Directory : directory;
    }

    public CanonicalSeedManifest Read()
    {
        var path = ResolveManifestPath();

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var version = root.TryGetProperty("seed_version", out var seedVersion)
            ? seedVersion.GetString()
            : root.TryGetProperty("seedVersion", out var camelVersion) ? camelVersion.GetString() : null;

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidDataException("The canonical seed manifest has no seed version.");
        }

        var datasets = new List<CanonicalSeedDataset>();
        foreach (var item in root.GetProperty("datasets").EnumerateArray())
        {
            var dataset = item.GetProperty("dataset").GetString()
                ?? throw new InvalidDataException("A canonical seed dataset has no name.");
            var recordCount = item.TryGetProperty("record_count", out var count)
                ? count.GetInt32()
                : item.TryGetProperty("recordCount", out var camelCount) ? camelCount.GetInt32() : 0;
            var unresolved = item.TryGetProperty("unresolved_count", out var unresolvedCount)
                ? unresolvedCount.GetInt32()
                : item.TryGetProperty("unresolvedCount", out var camelUnresolved) ? camelUnresolved.GetInt32() : 0;
            datasets.Add(new CanonicalSeedDataset(dataset, recordCount, unresolved));
        }

        return new CanonicalSeedManifest(version, datasets);
    }

    private string ResolveManifestPath()
    {
        var roots = new[]
        {
            hostEnvironment.ContentRootPath,
            Directory.GetParent(hostEnvironment.ContentRootPath)?.FullName,
            Directory.GetParent(hostEnvironment.ContentRootPath)?.Parent?.FullName,
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
            Directory.GetParent(AppContext.BaseDirectory)?.FullName,
            Directory.GetParent(AppContext.BaseDirectory)?.Parent?.FullName,
            Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.FullName
        }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var candidates = roots.SelectMany(root => new[]
        {
            Path.Combine(root!, "database", "seeds", "manifest.json"),
            Path.Combine(root!, "database", "seeds", "v1", "manifest.json")
        }).ToArray();

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("The Phase 3 canonical seed manifest was not found.", Path.Combine(hostEnvironment.ContentRootPath, "database", "seeds", "manifest.json"));
    }
}
