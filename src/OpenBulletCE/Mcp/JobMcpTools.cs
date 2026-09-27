using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using OpenBulletCE.ViewModels;
using RuriLib.Models;
using RuriLib.Runner;

namespace OpenBulletCE.Mcp;

/// <summary>
/// MCP tools for managing runner jobs: create, configure, start/stop, stats,
/// hits and logs.
/// </summary>
[McpServerToolType]
public sealed class JobMcpTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    // Per-runner rolling log captured from MessageArrived events
    private static readonly ConcurrentDictionary<int, ConcurrentQueue<(DateTime t, string level, string msg)>> _jobLogs = new();
    private static readonly ConcurrentDictionary<int, byte> _wired = new();

    private static RunnerInstance? GetRunner(int id)
    {
        try { return OB.RunnerManager.Get(id); } catch { return null; }
    }

    private static void WireLog(RunnerInstance ri)
    {
        if (_wired.ContainsKey(ri.Id)) return;
        if (_wired.TryAdd(ri.Id, 1))
        {
            var q = _jobLogs.GetOrAdd(ri.Id, _ => new ConcurrentQueue<(DateTime, string, string)>());
            ri.ViewModel.MessageArrived += (sender, level, message, prompt, timeout) =>
            {
                q.Enqueue((DateTime.Now, level.ToString(), message));
                while (q.Count > 500) q.TryDequeue(out _);
            };
        }
    }

    private static object JobShape(RunnerInstance ri)
    {
        var vm = ri.ViewModel;
        return new
        {
            id = ri.Id,
            status = vm.WorkerStatus.ToString(),
            busy = vm.Busy,
            config = vm.ConfigName,
            wordlist = vm.WordlistName,
            cookieList = vm.CookieName,
            bots = vm.BotsAmount,
            startingPoint = vm.StartingPoint,
            proxyMode = vm.ProxyMode.ToString(),
            progress = new { tested = vm.TestedCount, total = vm.ListSize, percent = vm.Progress },
            cpm = vm.CPM,
            hits = vm.HitCount,
            custom = vm.CustomCount,
            toCheck = vm.ToCheckCount,
            fails = vm.FailCount,
            retries = vm.RetryCount,
            proxies = new { total = vm.TotalProxiesCount, alive = vm.AliveProxiesCount, banned = vm.BannedProxiesCount, bad = vm.BadProxiesCount },
            elapsed = $"{vm.TimerDays}d {vm.TimerHours}:{vm.TimerMinutes}:{vm.TimerSeconds}",
            timeLeft = vm.TimeLeft
        };
    }

    /// <summary>Lists all runner jobs with status and stats.</summary>
    [McpServerTool(Name = "list_jobs"),
     Description("Lists all runner jobs (id, config, status, progress, CPM, hit counts).")]
    public string ListJobs()
    {
        var jobs = OB.RunnerManager.RunnersCollection.Select(JobShape);
        return JsonSerializer.Serialize(jobs, JsonOpts);
    }

    /// <summary>Gets one job's full status.</summary>
    [McpServerTool(Name = "get_job"),
     Description("Gets a job's full status by id: status, progress, CPM, hit/fail counts, proxy pool stats.")]
    public string GetJob([Description("Job id")] int id)
    {
        var ri = GetRunner(id);
        if (ri == null) return $"Job {id} not found. Use list_jobs.";
        return JsonSerializer.Serialize(JobShape(ri), JsonOpts);
    }

    /// <summary>Creates and configures a new job.</summary>
    [McpServerTool(Name = "create_job"),
     Description("Creates a new runner job bound to a config and a wordlist or cookie list. Returns the job id; then use start_job. A config's wordlist type must match the data source.")]
    public string CreateJob(
        [Description("Config name")] string configName,
        [Description("Wordlist name (for text combos)")] string wordlistName = "",
        [Description("Cookie list name (for cookie lists — CE configs)")] string cookieListName = "",
        [Description("Bot count (0 = keep suggested)")] int bots = 0,
        [Description("Starting point in the list (0-based skip)")] int startingPoint = 0,
        [Description("Proxy mode: Default, On, Off")] string proxyMode = "Default",
        [Description("Category")] string category = "")
    {
        var cfgVm = McpConfigHelper.Find(configName, category);
        if (cfgVm == null) return $"Config not found: {configName}";

        var instance = McpUi.Run(() => OB.RunnerManager.Create());
        var ri = McpUi.Run(() => OB.RunnerManager.RunnersCollection.Last());
        var vm = (RunnerViewModel)instance;

        vm.SetConfig(cfgVm.Config, bots == 0);
        if (bots > 0) vm.BotsAmount = bots;
        vm.StartingPoint = startingPoint;
        vm.ProxyMode = Enum.TryParse<ProxyMode>(proxyMode, true, out var pm) ? pm : ProxyMode.Default;

        if (!string.IsNullOrWhiteSpace(cookieListName))
        {
            var cookie = OB.CookieManager.Cookies.FirstOrDefault(c =>
                c.Name.Equals(cookieListName, StringComparison.OrdinalIgnoreCase));
            if (cookie == null) return $"Cookie list not found: {cookieListName}. Use list_cookie_lists.";
            vm.SetCookielist(cookie);
        }
        else if (!string.IsNullOrWhiteSpace(wordlistName))
        {
            var wl = OB.WordlistManager.Wordlists.FirstOrDefault(w =>
                w.Name.Equals(wordlistName, StringComparison.OrdinalIgnoreCase));
            if (wl == null) return $"Wordlist not found: {wordlistName}. Use list_wordlists.";
            vm.SetWordlist(wl);
        }
        else
        {
            return "You must provide wordlistName or cookieListName.";
        }

        WireLog(ri);
        return $"Job created with id {ri.Id} ({vm.ConfigName} — {vm.ListSize} lines)";
    }

    /// <summary>Starts a job.</summary>
    [McpServerTool(Name = "start_job"),
     Description("Starts a job's master worker (graceful).")]
    public string StartJob([Description("Job id")] int id)
    {
        var ri = GetRunner(id);
        if (ri == null) return $"Job {id} not found.";
        if (ri.ViewModel.Busy) return $"Job {id} is already running.";
        McpUi.Run(() => ri.ViewModel.Start());
        return $"Job {id} started.";
    }

    /// <summary>Gracefully stops a job.</summary>
    [McpServerTool(Name = "stop_job"),
     Description("Sends a graceful stop — bots finish their current check then stop.")]
    public string StopJob([Description("Job id")] int id)
    {
        var ri = GetRunner(id);
        if (ri == null) return $"Job {id} not found.";
        McpUi.Run(() => ri.ViewModel.Stop());
        return $"Job {id} stopping.";
    }

    /// <summary>Hard-aborts a job.</summary>
    [McpServerTool(Name = "abort_job"),
     Description("Force-aborts a job immediately (hard abort).")]
    public string AbortJob([Description("Job id")] int id)
    {
        var ri = GetRunner(id);
        if (ri == null) return $"Job {id} not found.";
        McpUi.Run(() => ri.ViewModel.ForceStop());
        return $"Job {id} aborted.";
    }

    /// <summary>Pause is not supported by this engine.</summary>
    [McpServerTool(Name = "pause_job"),
     Description("Pauses a job. NOT SUPPORTED by the Cookie Edition engine — returns an explanatory message.")]
    public string PauseJob([Description("Job id")] int id)
        => "Pause is not supported by the OpenBullet CE runner engine. Use stop_job (graceful) or abort_job (immediate).";

    /// <summary>Resume is not supported by this engine.</summary>
    [McpServerTool(Name = "resume_job"),
     Description("Resumes a paused job. NOT SUPPORTED by the Cookie Edition engine — returns an explanatory message.")]
    public string ResumeJob([Description("Job id")] int id)
        => "Resume is not supported by the OpenBullet CE runner engine. Stop and start_job again to continue (startingPoint can skip ahead).";

    /// <summary>Changes the bot count of a job (live if running).</summary>
    [McpServerTool(Name = "set_bots_amount"),
     Description("Changes the number of bots for a job. When running, bots are added/removed live.")]
    public string SetBotsAmount(
        [Description("Job id")] int id,
        [Description("New bot count")] int bots)
    {
        var ri = GetRunner(id);
        if (ri == null) return $"Job {id} not found.";
        McpUi.Run(() => ri.ViewModel.BotsAmount = Math.Clamp(bots, 1, 200));
        return $"Job {id} bots set to {ri.ViewModel.BotsAmount}.";
    }

    /// <summary>Deletes a job (must be stopped first).</summary>
    [McpServerTool(Name = "delete_job"),
     Description("Removes a job. Stops it first if still running.")]
    public string DeleteJob([Description("Job id")] int id)
    {
        var ri = GetRunner(id);
        if (ri == null) return $"Job {id} not found.";
        if (ri.ViewModel.Busy) McpUi.Run(() => ri.ViewModel.ForceStop());
        McpUi.Run(() => OB.RunnerManager.Remove(id));
        _jobLogs.TryRemove(id, out _);
        _wired.TryRemove(id, out _);
        return $"Job {id} deleted.";
    }

    /// <summary>Returns the recent log lines of a job.</summary>
    [McpServerTool(Name = "get_job_log"),
     Description("Returns the recent log messages of a job (up to 500 lines retained).")]
    public string GetJobLog(
        [Description("Job id")] int id,
        [Description("Max lines to return (newest)")] int lines = 100)
    {
        var ri = GetRunner(id);
        if (ri == null) return $"Job {id} not found.";
        WireLog(ri);

        if (!_jobLogs.TryGetValue(id, out var q) || q.IsEmpty)
            return "(no log yet)";

        var entries = q.Reverse().Take(lines).Reverse()
            .Select(e => $"[{e.t:HH:mm:ss}] [{e.level}] {e.msg}");
        return string.Join("\n", entries);
    }

    /// <summary>Gets the hits/custom results a job produced.</summary>
    [McpServerTool(Name = "get_job_hits"),
     Description("Returns the hits (and optionally custom results) a job produced so far, newest first.")]
    public string GetJobHits(
        [Description("Job id")] int id,
        [Description("Result filter: Success, Custom, ToCheck, All")] string filter = "Success",
        [Description("Max entries")] int take = 100)
    {
        var ri = GetRunner(id);
        if (ri == null) return $"Job {id} not found.";
        var vm = ri.ViewModel;
        take = Math.Clamp(take, 1, 500);

        IEnumerable<ValidData> src = filter.ToLowerInvariant() switch
        {
            "custom" => vm.CustomList,
            "tocheck" => vm.ToCheckList,
            "all" => vm.Checked,
            _ => vm.HitsList
        };

        var hits = src.OrderByDescending(h => h.UnixDate).Take(take).Select(h => new
        {
            data = h.Data,
            proxy = h.Proxy,
            type = h.Type,
            result = h.Result.ToString(),
            captured = h.CapturedData,
            time = h.Time
        });
        return JsonSerializer.Serialize(hits, JsonOpts);
    }
}
