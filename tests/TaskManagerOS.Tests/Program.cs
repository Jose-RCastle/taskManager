using TaskManagerOS.Core.Memory;
using TaskManagerOS.Core.Models;
using TaskManagerOS.Core.Persistence;
using TaskManagerOS.Core.Presentation;
using TaskManagerOS.Core.Scheduling;
using TaskManagerOS.Core.Simulation;
using TaskManagerOS.Core.Validation;

var tests = new (string Name, Action Run)[]
{
    ("Round Robin respeta orden y quantum", RoundRobin),
    ("SJF selecciona la ráfaga menor", Sjf),
    ("Prioridad selecciona el número menor", Priority),
    ("Desempates son deterministas", TieBreak),
    ("Transición RR ejecución/listo/finalizado", Transitions),
    ("Bloqueo y regreso a listo", Blocking),
    ("Óptimo elige página sin uso futuro", Optimal),
    ("NRU clasifica y selecciona", Nru),
    ("Bits R/M se actualizan", Bits),
    ("JSON conserva proyecto", Json),
    ("Referencia inválida se detecta", InvalidPage),
    ("Simulación demo finaliza", FullSimulation),
    ("Simulación usa la entrada actual", CurrentInput),
    ("Una entrada modificada produce otro resultado", ChangedInput),
    ("Formatters producen nombres amigables", FriendlyNames),
    ("Estado final no mantiene proceso activo", CleanFinalState)
};
var failed = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine($"[OK] {test.Name}"); }
    catch (Exception ex) { failed++; Console.Error.WriteLine($"[ERROR] {test.Name}: {ex.Message}"); }
}
Console.WriteLine($"{tests.Length - failed}/{tests.Length} pruebas superadas.");
return failed == 0 ? 0 : 1;

static ProcessInstance P(string name, int burst, int arrival = 0, int priority = 1) => new()
{ Id = Guid.NewGuid(), Name = name, CpuBurst = burst, RemainingTime = burst, ArrivalTime = arrival, Priority = priority, PageCount = 3, PageReferencePattern = "0R,1W" };
static OperatingSystemConfiguration C(SchedulingAlgorithmType type = SchedulingAlgorithmType.RoundRobin) => new() { SchedulingAlgorithm = type, Quantum = 2 };
static void Eq<T>(T expected, T actual) where T : notnull { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Esperado {expected}, obtenido {actual}"); }
static void True(bool value, string message) { if (!value) throw new Exception(message); }

static void RoundRobin()
{
    var ticks = new CpuTimelineGenerator().Generate([P("A", 3), P("B", 2)], C());
    Eq("A,A,B,B,A", string.Join(',', ticks.Select(t => t.ProcessName)));
}
static void Sjf() { var ticks = new CpuTimelineGenerator().Generate([P("Largo", 4), P("Corto", 1)], C(SchedulingAlgorithmType.ShortestJobFirst)); Eq("Corto", ticks[0].ProcessName); }
static void Priority() { var ticks = new CpuTimelineGenerator().Generate([P("Baja", 1, priority: 5), P("Alta", 1, priority: 1)], C(SchedulingAlgorithmType.Priority)); Eq("Alta", ticks[0].ProcessName); }
static void TieBreak()
{
    var a = P("Primero", 2); var b = P("Segundo", 2); a.Id = Guid.Parse("00000000-0000-0000-0000-000000000001"); b.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
    Eq(a.Id, new SjfScheduling().Select([b, a])!.Id); Eq(a.Id, new PriorityScheduling().Select([b, a])!.Id);
}
static void Transitions()
{
    var a = P("A", 3); var ticks = new CpuTimelineGenerator().Generate([a, P("B", 1)], C());
    Eq(ProcessStatus.Running, ticks[0].States[a.Id]); Eq(ProcessStatus.Ready, ticks[2].States[a.Id]); Eq(ProcessStatus.Running, ticks[3].States[a.Id]);
}
static void Blocking()
{
    var a = P("IO", 3); a.BlockAfterCpuTicks = 1; a.BlockDurationTicks = 2;
    var ticks = new CpuTimelineGenerator().Generate([a], C());
    True(ticks.Any(t => t.States[a.Id] == ProcessStatus.Blocked), "No se observó bloqueo");
    True(ticks.Count(t => t.ProcessId == a.Id) == 3, "No regresó y completó CPU");
}
static void Optimal()
{
    var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
    var frames = ids.Take(3).Select((id, i) => new PageFrame { FrameNumber = i, Page = new(id, 0) }).ToList();
    var refs = new List<MemoryReference> { new(new(ids[3], 0), "D", MemoryAccessType.Read), new(new(ids[0], 0), "A", MemoryAccessType.Read), new(new(ids[1], 0), "B", MemoryAccessType.Read) };
    Eq(2, new OptimalPageReplacement().SelectVictim(new(frames, refs, 0, 0)));
}
static void Nru()
{
    var frames = new List<PageFrame> { new() { FrameNumber = 0, Page = new(Guid.NewGuid(), 0), Referenced = true }, new() { FrameNumber = 1, Page = new(Guid.NewGuid(), 0), Modified = true }, new() { FrameNumber = 2, Page = new(Guid.NewGuid(), 0) } };
    Eq(0, NruPageReplacement.ClassOf(frames[2])); Eq(2, new NruPageReplacement().SelectVictim(new(frames, [], 0, 0)));
}
static void Bits()
{
    var c = C(); c.PageReplacementAlgorithm = PageReplacementAlgorithmType.Nru; var mmu = new MemoryManager(c); var id = Guid.NewGuid();
    var refs = new List<MemoryReference> { new(new(id, 0), "P", MemoryAccessType.Write) }; mmu.Access(refs[0], refs, 0, 0);
    True(mmu.Frames[0].Referenced && mmu.Frames[0].Modified, "Una escritura debe activar R y M");
}
static void Json()
{
    var path = Path.Combine(Path.GetTempPath(), $"tmos-{Guid.NewGuid()}.json"); var store = new ProjectStore(); store.Save(path, DemoDataFactory.Create()); var loaded = store.Load(path); File.Delete(path);
    Eq(5, loaded.Programs.Count); Eq(10, loaded.ExecutionList.Count); Eq(SchedulingAlgorithmType.RoundRobin, loaded.Configuration.SchedulingAlgorithm);
}
static void InvalidPage()
{
    var p = P("Inválido", 1); p.PageCount = 2; p.PageReferencePattern = "2R";
    True(ProjectValidator.Validate(C(), [p]).Any(e => e.Contains("excede")), "No detectó página fuera de rango");
}
static void FullSimulation()
{
    var demo = DemoDataFactory.Create(); var result = new SimulationEngine().Run(demo.ExecutionList, demo.Configuration);
    True(result.Completed, "No finalizó"); Eq(10, result.Metrics.CompletedProcesses); True(result.Steps.Count > 0 && result.Metrics.TotalReferences > 0, "Sin pasos");
}
static void CurrentInput()
{
    var input = new[] { P("A", 1), P("B", 2), P("C", 1) };
    var result = new SimulationEngine().Run(input, C());
    Eq(input.Length, result.InputProcessCount); Eq(input.Length, result.Metrics.CompletedProcesses);
}
static void ChangedInput()
{
    var input = new List<ProcessInstance> { P("A", 1), P("B", 1) };
    var engine = new SimulationEngine(); var previous = engine.Run(input, C()); input.RemoveAt(1); var current = engine.Run(input, C());
    Eq(2, previous.InputProcessCount); Eq(1, current.InputProcessCount); Eq(1, current.Metrics.CompletedProcesses);
}
static void FriendlyNames()
{
    Eq("Round Robin", DisplayText.Scheduler(SchedulingAlgorithmType.RoundRobin)); Eq("Proceso más corto (SJF)", DisplayText.Scheduler(SchedulingAlgorithmType.ShortestJobFirst));
    Eq("Prioridad", DisplayText.Scheduler(SchedulingAlgorithmType.Priority)); Eq("Óptimo", DisplayText.Replacement(PageReplacementAlgorithmType.Optimal)); Eq("NRU", DisplayText.Replacement(PageReplacementAlgorithmType.Nru));
    Eq("Compilador #8 — Página 3", DisplayText.Page("Compilador #8", 3));
}
static void CleanFinalState()
{
    var result = new SimulationEngine().Run([P("A", 1), P("B", 2)], C()); var final = result.Steps.Last();
    True(final.ProcessStates.Values.All(s => s == ProcessStatus.Finished), "Quedó un proceso activo en el estado final"); Eq(2, result.Metrics.CompletedProcesses);
}
