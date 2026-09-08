using TaskManagerOS.Core.Memory;
using TaskManagerOS.Core.Models;
using TaskManagerOS.Core.Scheduling;
using TaskManagerOS.Core.Validation;

namespace TaskManagerOS.Core.Simulation;

public sealed class SimulationEngine
{
    public SimulationResult Run(IEnumerable<ProcessInstance> input, OperatingSystemConfiguration configuration)
    {
        var processes = input.Where(p => p.IsActive).Select(p => p.Copy()).ToList();
        var errors = ProjectValidator.Validate(configuration, processes);
        if (processes.Count == 0) errors.Add("Agregue al menos un proceso activo a la lista.");
        if (errors.Count > 0) throw new ArgumentException(string.Join(Environment.NewLine, errors));

        var ticks = new CpuTimelineGenerator().Generate(processes, configuration);
        var patterns = processes.ToDictionary(p => p.Id, p => PageAccessParser.Parse(p.PageReferencePattern));
        var accessIndexes = processes.ToDictionary(p => p.Id, _ => 0);
        var references = ticks.Where(t => t.ProcessId.HasValue).Select(t =>
        {
            var id = t.ProcessId!.Value; var access = patterns[id][accessIndexes[id]++ % patterns[id].Count];
            return new MemoryReference(new(id, access.PageNumber), t.ProcessName, access.Type);
        }).ToList();
        accessIndexes.Keys.ToList().ForEach(id => accessIndexes[id] = 0);

        var mmu = new MemoryManager(configuration);
        var result = new SimulationResult();
        var referenceIndex = 0;
        var executed = processes.ToDictionary(p => p.Id, _ => 0);
        foreach (var cpu in ticks)
        {
            var step = new SimulationStep
            {
                Tick = cpu.Tick, CurrentProcessId = cpu.ProcessId, CurrentProcessName = cpu.ProcessName,
                ProcessStates = cpu.States.ToDictionary(x => x.Key, x => x.Value), ReadyQueue = cpu.ReadyQueue.ToList(),
                BlockedProcesses = cpu.BlockedProcesses.ToList()
            };
            if (cpu.ProcessId.HasValue)
            {
                executed[cpu.ProcessId.Value]++;
                var reference = references[referenceIndex];
                var access = patterns[cpu.ProcessId.Value][accessIndexes[cpu.ProcessId.Value]++ % patterns[cpu.ProcessId.Value].Count];
                var outcome = mmu.Access(reference, references, referenceIndex, cpu.Tick);
                step.MemoryAccess = access; step.PageHit = outcome.Hit; step.EvictedPage = outcome.Evicted; step.LoadedPage = outcome.Loaded;
                step.Explanation = $"Tick {cpu.Tick}: {cpu.ProcessName} accede a página {access.PageNumber} en modo " +
                    $"{(access.Type == MemoryAccessType.Read ? "lectura" : "escritura")}; {outcome.Decision}.";
                referenceIndex++;
                if (executed[cpu.ProcessId.Value] == processes.First(p => p.Id == cpu.ProcessId.Value).CpuBurst)
                    step.ProcessStates[cpu.ProcessId.Value] = ProcessStatus.Finished;
            }
            else step.Explanation = $"Tick {cpu.Tick}: CPU inactiva; se esperan llegadas o finalización de E/S.";
            step.Frames = mmu.Snapshot(); step.PageTable = mmu.PageTable(); result.Steps.Add(step);
        }

        var metrics = new SimulationMetrics
        {
            TotalTicks = ticks.Count, CompletedProcesses = processes.Count, TotalReferences = references.Count,
            Hits = result.Steps.Count(s => s.PageHit == true), PageFaults = result.Steps.Count(s => s.PageHit == false)
        };
        foreach (var p in processes)
        {
            if (p.CpuBurst == 0) { metrics.TurnaroundTimeByProcess[p.Id] = 0; metrics.WaitingTimeByProcess[p.Id] = 0; continue; }
            var last = ticks.Last(t => t.ProcessId == p.Id).Tick + 1;
            var turnaround = last - p.ArrivalTime;
            metrics.TurnaroundTimeByProcess[p.Id] = turnaround;
            metrics.WaitingTimeByProcess[p.Id] = Math.Max(0, turnaround - p.CpuBurst - (p.BlockAfterCpuTicks > 0 ? p.BlockDurationTicks : 0));
        }
        result.Metrics = metrics; result.Completed = true;
        return result;
    }
}
