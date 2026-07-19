using System.IO;
using Timetable.Application.Collaboration;

namespace Timetable.Infrastructure.Collaboration;

/// <summary>
/// Anwesenheitsanzeige über Sidecar-Dateien neben der Plandatei
/// (z. B. "plan.json.presence-mats"). Der Dateiinhalt ist der Anzeigename,
/// der Schreibzeitstempel dient als Heartbeat. Einträge, deren Heartbeat
/// älter als <see cref="PresenceOptions.StaleAfter"/> ist, gelten als verwaist
/// und werden ignoriert — so blockiert ein Absturz niemanden.
/// </summary>
public sealed class FilePresenceService(PresenceOptions options) : IPresenceService
{
    private readonly string _planPath = Path.GetFullPath(options.PlanFilePath);
    private CancellationTokenSource? _heartbeatCts;
    private Task? _heartbeatLoop;

    private string PresencePrefix => _planPath + ".presence-";

    private string OwnFilePath => PresencePrefix + SanitizeForFileName(options.UserName);

    public async Task AnnounceAsync(CancellationToken ct = default)
    {
        await WriteHeartbeatAsync(ct);

        if (_heartbeatLoop is not null)
            return;

        _heartbeatCts = new CancellationTokenSource();
        _heartbeatLoop = RunHeartbeatLoopAsync(_heartbeatCts.Token);
    }

    public Task<IReadOnlyList<PresenceInfo>> GetOtherUsersAsync(CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(_planPath)!;
        var others = new List<PresenceInfo>();

        if (!Directory.Exists(directory))
            return Task.FromResult<IReadOnlyList<PresenceInfo>>(others);

        var now = DateTimeOffset.UtcNow;
        foreach (var file in Directory.EnumerateFiles(directory, Path.GetFileName(PresencePrefix) + "*"))
        {
            ct.ThrowIfCancellationRequested();

            if (string.Equals(file, OwnFilePath, StringComparison.OrdinalIgnoreCase))
                continue;

            var heartbeat = new DateTimeOffset(File.GetLastWriteTimeUtc(file), TimeSpan.Zero);
            if (now - heartbeat > options.StaleAfter)
                continue;

            others.Add(new PresenceInfo(ReadUserName(file), heartbeat));
        }

        others.Sort((a, b) => string.Compare(a.UserName, b.UserName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<IReadOnlyList<PresenceInfo>>(others);
    }

    public async Task WithdrawAsync(CancellationToken ct = default)
    {
        if (_heartbeatCts is not null)
        {
            await _heartbeatCts.CancelAsync();
            if (_heartbeatLoop is not null)
                await _heartbeatLoop;
            _heartbeatCts.Dispose();
            _heartbeatCts = null;
            _heartbeatLoop = null;
        }

        if (File.Exists(OwnFilePath))
            File.Delete(OwnFilePath);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await WithdrawAsync();
        }
        catch (IOException)
        {
            // Aufräumen darf das Beenden nie verhindern; verwaiste Dateien
            // werden ohnehin über StaleAfter aussortiert.
        }
    }

    private async Task RunHeartbeatLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(options.HeartbeatInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await WriteHeartbeatAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // reguläres Ende über WithdrawAsync
        }
    }

    private Task WriteHeartbeatAsync(CancellationToken ct) =>
        File.WriteAllTextAsync(OwnFilePath, options.UserName, ct);

    private string ReadUserName(string presenceFile)
    {
        try
        {
            var content = File.ReadAllText(presenceFile).Trim();
            if (content.Length > 0)
                return content;
        }
        catch (IOException)
        {
            // Datei wird gerade von der anderen Instanz geschrieben —
            // dann eben der (sanitisierte) Name aus dem Dateinamen.
        }

        return presenceFile[PresencePrefix.Length..];
    }

    private static string SanitizeForFileName(string userName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(userName.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
