using TaskManagerOS.Core.Models;

namespace TaskManagerOS.Core.Presentation;

/// <summary>Centraliza los textos destinados a personas sin alterar los valores persistidos.</summary>
public static class DisplayText
{
    public static string Scheduler(SchedulingAlgorithmType value) => value switch
    {
        SchedulingAlgorithmType.RoundRobin => "Round Robin",
        SchedulingAlgorithmType.ShortestJobFirst => "Proceso más corto (SJF)",
        SchedulingAlgorithmType.Priority => "Prioridad",
        _ => value.ToString()
    };

    public static string Replacement(PageReplacementAlgorithmType value) => value switch
    {
        PageReplacementAlgorithmType.Optimal => "Óptimo",
        PageReplacementAlgorithmType.Nru => "NRU",
        _ => value.ToString()
    };

    public static string Page(string processName, int pageNumber) => $"{processName} — Página {pageNumber}";
}
