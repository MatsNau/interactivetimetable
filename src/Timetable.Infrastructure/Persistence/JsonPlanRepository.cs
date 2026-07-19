using System.IO;
using System.Text.Json;
using Timetable.Application.Persistence;
using Timetable.Domain.Planning;

namespace Timetable.Infrastructure.Persistence;

/// <summary>
/// Persistiert den Plan als eine gemeinsame JSON-Datei.
/// Konflikterkennung über den Schreibzeitstempel: Wer mit einer veralteten
/// Version speichert, bekommt eine <see cref="PlanConflictException"/> und muss
/// das Überschreiben explizit bestätigen. Vor jedem Überschreiben wird ein
/// rotierendes Backup angelegt; geschrieben wird atomar über eine Temp-Datei,
/// damit nie eine halb geschriebene Plandatei auf dem Share liegt.
/// </summary>
public sealed class JsonPlanRepository(PlanFileOptions options) : IPlanRepository
{
    public Task<bool> ExistsAsync(CancellationToken ct = default) =>
        Task.FromResult(File.Exists(options.FilePath));

    public async Task<PlanDocument> LoadAsync(CancellationToken ct = default)
    {
        var path = options.FilePath;
        var version = new PlanVersionToken(File.GetLastWriteTimeUtc(path));

        await using var stream = File.OpenRead(path);
        var plan = await JsonSerializer.DeserializeAsync<ProjectPlan>(stream, PlanJson.Options, ct)
                   ?? throw new InvalidDataException($"Die Plandatei \"{path}\" enthält keinen gültigen Plan.");

        return new PlanDocument(plan, version);
    }

    public async Task<PlanVersionToken> SaveAsync(
        ProjectPlan plan,
        PlanVersionToken expectedVersion,
        bool overwrite = false,
        CancellationToken ct = default)
    {
        var path = options.FilePath;

        if (File.Exists(path))
        {
            var actual = new PlanVersionToken(File.GetLastWriteTimeUtc(path));
            if (!overwrite && actual != expectedVersion)
                throw new PlanConflictException(expectedVersion, actual);

            RotateBackups(path);
        }

        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, plan, PlanJson.Options, ct);
        }

        File.Move(tempPath, path, overwrite: true);
        return new PlanVersionToken(File.GetLastWriteTimeUtc(path));
    }

    private void RotateBackups(string path)
    {
        if (options.BackupCount <= 0)
            return;

        var oldest = BackupPath(path, options.BackupCount);
        if (File.Exists(oldest))
            File.Delete(oldest);

        for (var i = options.BackupCount - 1; i >= 1; i--)
        {
            var source = BackupPath(path, i);
            if (File.Exists(source))
                File.Move(source, BackupPath(path, i + 1));
        }

        File.Copy(path, BackupPath(path, 1));
    }

    private static string BackupPath(string path, int index) => $"{path}.backup-{index}";
}
