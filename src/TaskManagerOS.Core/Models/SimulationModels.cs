namespace TaskManagerOS.Core.Models;

public sealed class PageFrame
{
    public int FrameNumber { get; set; }
    public PageIdentity? Page { get; set; }
    public string? ProcessName { get; set; }
    public bool Referenced { get; set; }
    public bool Modified { get; set; }
    public int LoadedAt { get; set; }
    public PageFrame Copy() => (PageFrame)MemberwiseClone();
}

public sealed class PageTableEntry
{
    public PageIdentity Page { get; set; }
    public string ProcessName { get; set; } = "";
    public int FrameNumber { get; set; }
    public bool Present { get; set; }
    public bool Referenced { get; set; }
    public bool Modified { get; set; }
}

public sealed class SimulationStep
{
    public int Tick { get; set; }
    public Guid? CurrentProcessId { get; set; }
    public string CurrentProcessName { get; set; } = "CPU inactiva";
    public Dictionary<Guid, ProcessStatus> ProcessStates { get; set; } = [];
    public PageAccess? MemoryAccess { get; set; }
    public List<PageFrame> Frames { get; set; } = [];
    public List<PageTableEntry> PageTable { get; set; } = [];
    public bool? PageHit { get; set; }
    public PageIdentity? EvictedPage { get; set; }
    public PageIdentity? LoadedPage { get; set; }
    public string Explanation { get; set; } = "";
    public List<string> ReadyQueue { get; set; } = [];
    public List<string> BlockedProcesses { get; set; } = [];
}

public sealed class SimulationMetrics
{
    public int TotalTicks { get; set; }
    public int CompletedProcesses { get; set; }
    public int TotalReferences { get; set; }
    public int Hits { get; set; }
    public int PageFaults { get; set; }
    public Dictionary<Guid, int> WaitingTimeByProcess { get; set; } = [];
    public Dictionary<Guid, int> TurnaroundTimeByProcess { get; set; } = [];
    public double HitRate => TotalReferences == 0 ? 0 : (double)Hits / TotalReferences;
    public double FaultRate => TotalReferences == 0 ? 0 : (double)PageFaults / TotalReferences;
    public double MmuPerformance => 1 - FaultRate;
}

public sealed class SimulationResult
{
    public List<SimulationStep> Steps { get; set; } = [];
    public SimulationMetrics Metrics { get; set; } = new();
    public bool Completed { get; set; }
    public int InputProcessCount { get; set; }
}

public sealed record CpuTick(int Tick, Guid? ProcessId, string ProcessName,
    IReadOnlyDictionary<Guid, ProcessStatus> States, IReadOnlyList<string> ReadyQueue,
    IReadOnlyList<string> BlockedProcesses);
