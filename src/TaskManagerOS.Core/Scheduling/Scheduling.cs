using TaskManagerOS.Core.Models;

namespace TaskManagerOS.Core.Scheduling;

public interface ISchedulingAlgorithm
{
    string Name { get; }
    bool IsPreemptive { get; }
    ProcessInstance? Select(IReadOnlyList<ProcessInstance> ready);
    int TimeSlice(OperatingSystemConfiguration configuration);
}

public sealed class RoundRobinScheduling : ISchedulingAlgorithm
{
    public string Name => "Round Robin";
    public bool IsPreemptive => true;
    public ProcessInstance? Select(IReadOnlyList<ProcessInstance> ready) => ready.FirstOrDefault();
    public int TimeSlice(OperatingSystemConfiguration configuration) => configuration.Quantum;
}

public sealed class SjfScheduling : ISchedulingAlgorithm
{
    public string Name => "SJF no expropiativo";
    public bool IsPreemptive => false;
    public ProcessInstance? Select(IReadOnlyList<ProcessInstance> ready) => ready
        .OrderBy(p => p.RemainingTime).ThenBy(p => p.ArrivalTime).ThenBy(p => p.Id).FirstOrDefault();
    public int TimeSlice(OperatingSystemConfiguration configuration) => int.MaxValue;
}

public sealed class PriorityScheduling : ISchedulingAlgorithm
{
    public string Name => "Prioridad no expropiativa";
    public bool IsPreemptive => false;
    public ProcessInstance? Select(IReadOnlyList<ProcessInstance> ready) => ready
        .OrderBy(p => p.Priority).ThenBy(p => p.ArrivalTime).ThenBy(p => p.Id).FirstOrDefault();
    public int TimeSlice(OperatingSystemConfiguration configuration) => int.MaxValue;
}

public static class SchedulingFactory
{
    public static ISchedulingAlgorithm Create(SchedulingAlgorithmType type) => type switch
    {
        SchedulingAlgorithmType.RoundRobin => new RoundRobinScheduling(),
        SchedulingAlgorithmType.ShortestJobFirst => new SjfScheduling(),
        SchedulingAlgorithmType.Priority => new PriorityScheduling(),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}

public sealed class CpuTimelineGenerator
{
    public IReadOnlyList<CpuTick> Generate(IEnumerable<ProcessInstance> source, OperatingSystemConfiguration config)
    {
        var processes = source.Where(p => p.IsActive && p.CpuBurst > 0).Select(p => p.Copy()).ToList();
        foreach (var p in processes) { p.Status = ProcessStatus.New; p.RemainingTime = p.CpuBurst; p.CpuConsumed = 0; p.IsFinished = false; p.CompletionTime = null; }
        var ready = new List<ProcessInstance>();
        var blockedUntil = new Dictionary<Guid, int>();
        var ioPerformed = new HashSet<Guid>();
        var output = new List<CpuTick>();
        var algorithm = SchedulingFactory.Create(config.SchedulingAlgorithm);
        ProcessInstance? running = null;
        var sliceUsed = 0;
        var tick = 0;
        var guard = Math.Max(1000, processes.Sum(p => p.CpuBurst + p.BlockDurationTicks) * 10 + 100);

        while (processes.Any(p => !p.IsFinished) && tick < guard)
        {
            foreach (var p in processes.Where(p => p.Status == ProcessStatus.New && p.ArrivalTime <= tick)
                         .OrderBy(p => p.ArrivalTime).ThenBy(p => p.Id)) { p.Status = ProcessStatus.Ready; ready.Add(p); }
            foreach (var p in processes.Where(p => p.Status == ProcessStatus.Blocked && blockedUntil[p.Id] <= tick)
                         .OrderBy(p => blockedUntil[p.Id]).ThenBy(p => p.Id)) { p.Status = ProcessStatus.Ready; ready.Add(p); }

            if (running is null)
            {
                running = algorithm.Select(ready);
                if (running is not null) { ready.Remove(running); running.Status = ProcessStatus.Running; sliceUsed = 0; }
            }

            var states = processes.ToDictionary(p => p.Id, p => p.Status);
            output.Add(new(tick, running?.Id, running?.Name ?? "CPU inactiva", states,
                ready.Select(p => p.Name).ToList(), processes.Where(p => p.Status == ProcessStatus.Blocked).Select(p => p.Name).ToList()));

            if (running is not null)
            {
                running.RemainingTime--; running.CpuConsumed++; sliceUsed++;
                if (running.RemainingTime == 0)
                {
                    running.Status = ProcessStatus.Finished; running.IsFinished = true; running.CompletionTime = tick + 1; running = null;
                }
                else if (!ioPerformed.Contains(running.Id) && running.BlockAfterCpuTicks > 0 && running.CpuConsumed >= running.BlockAfterCpuTicks)
                {
                    ioPerformed.Add(running.Id); running.Status = ProcessStatus.Blocked;
                    blockedUntil[running.Id] = tick + 1 + running.BlockDurationTicks; running = null;
                }
                else if (algorithm.IsPreemptive && sliceUsed >= algorithm.TimeSlice(config))
                {
                    running.Status = ProcessStatus.Ready; ready.Add(running); running = null;
                }
            }
            tick++;
        }
        if (tick >= guard) throw new InvalidOperationException("La simulación excedió el límite de seguridad.");
        return output;
    }
}
