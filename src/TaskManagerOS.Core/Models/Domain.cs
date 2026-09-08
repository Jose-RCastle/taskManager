namespace TaskManagerOS.Core.Models;

public enum ProcessStatus { New, Ready, Running, Blocked, Finished }
public enum SchedulingAlgorithmType { RoundRobin, ShortestJobFirst, Priority }
public enum PageReplacementAlgorithmType { Optimal, Nru }
public enum MemoryAccessType { Read, Write }

public sealed record PageAccess(int PageNumber, MemoryAccessType Type)
{
    public override string ToString() => $"{PageNumber}{(Type == MemoryAccessType.Read ? 'R' : 'W')}";
}

public readonly record struct PageIdentity(Guid ProcessInstanceId, int PageNumber)
{
    public override string ToString() => $"{ProcessInstanceId.ToString()[..8]}:P{PageNumber}";
}

public sealed class ProcessDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Nuevo programa";
    public int CpuBurst { get; set; } = 1;
    public int Priority { get; set; } = 1;
    public int PageCount { get; set; } = 4;
    public string PageReferencePattern { get; set; } = "0R";
    public int QueueNumber { get; set; }
    public int LotteryTickets { get; set; } = 1;
    public decimal GuaranteedPercentage { get; set; }
    public int BlockAfterCpuTicks { get; set; }
    public int BlockDurationTicks { get; set; }
}

public sealed class ProcessInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DefinitionId { get; set; }
    public string Name { get; set; } = "Proceso";
    public ProcessStatus Status { get; set; } = ProcessStatus.New;
    public int ArrivalTime { get; set; }
    public int CpuBurst { get; set; } = 1;
    public int RemainingTime { get; set; } = 1;
    public int Priority { get; set; } = 1;
    public int PageCount { get; set; } = 4;
    public string PageReferencePattern { get; set; } = "0R";
    public bool IsActive { get; set; } = true;
    public bool IsFinished { get; set; }
    public int QueueNumber { get; set; }
    public int LotteryTickets { get; set; } = 1;
    public decimal GuaranteedPercentage { get; set; }
    public int BlockAfterCpuTicks { get; set; }
    public int BlockDurationTicks { get; set; }
    public int CpuConsumed { get; set; }
    public int? CompletionTime { get; set; }

    public static ProcessInstance From(ProcessDefinition d, int arrival = 0) => new()
    {
        DefinitionId = d.Id, Name = d.Name, ArrivalTime = arrival, CpuBurst = d.CpuBurst,
        RemainingTime = d.CpuBurst, Priority = d.Priority, PageCount = d.PageCount,
        PageReferencePattern = d.PageReferencePattern, QueueNumber = d.QueueNumber,
        LotteryTickets = d.LotteryTickets, GuaranteedPercentage = d.GuaranteedPercentage,
        BlockAfterCpuTicks = d.BlockAfterCpuTicks, BlockDurationTicks = d.BlockDurationTicks
    };

    public ProcessInstance Copy() => (ProcessInstance)MemberwiseClone();
}

public sealed class OperatingSystemConfiguration
{
    public int Quantum { get; set; } = 2;
    public int PhysicalFrameCount { get; set; } = 4;
    public int VirtualPageCount { get; set; } = 8;
    public int PageSizeKb { get; set; } = 4;
    public int NruReferenceResetInterval { get; set; } = 4;
    public SchedulingAlgorithmType SchedulingAlgorithm { get; set; } = SchedulingAlgorithmType.RoundRobin;
    public PageReplacementAlgorithmType PageReplacementAlgorithm { get; set; } = PageReplacementAlgorithmType.Optimal;
}

public sealed class ProjectData
{
    public List<ProcessDefinition> Programs { get; set; } = [];
    public List<ProcessInstance> ExecutionList { get; set; } = [];
    public OperatingSystemConfiguration Configuration { get; set; } = new();
}
