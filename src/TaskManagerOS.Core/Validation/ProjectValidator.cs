using System.Text.RegularExpressions;
using TaskManagerOS.Core.Models;

namespace TaskManagerOS.Core.Validation;

public static partial class PageAccessParser
{
    [GeneratedRegex(@"^\s*(\d+)\s*([RrWw])\s*$")]
    private static partial Regex TokenRegex();

    public static IReadOnlyList<PageAccess> Parse(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("El patrón de páginas no puede estar vacío.");
        var result = new List<PageAccess>();
        foreach (var token in pattern.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = TokenRegex().Match(token);
            if (!match.Success) throw new ArgumentException($"Referencia '{token.Trim()}' inválida. Use el formato 0R,1W.");
            result.Add(new(int.Parse(match.Groups[1].Value),
                char.ToUpperInvariant(match.Groups[2].Value[0]) == 'R' ? MemoryAccessType.Read : MemoryAccessType.Write));
        }
        if (result.Count == 0) throw new ArgumentException("Debe existir al menos una referencia de página.");
        return result;
    }
}

public static class ProjectValidator
{
    public static List<string> Validate(OperatingSystemConfiguration c, IEnumerable<ProcessInstance> processes)
    {
        var errors = new List<string>();
        if (c.Quantum <= 0) errors.Add("El quantum debe ser mayor que cero.");
        if (c.PhysicalFrameCount is < 2 or > 32) errors.Add("Los marcos físicos deben estar entre 2 y 32.");
        if (c.VirtualPageCount < c.PhysicalFrameCount) errors.Add("Las páginas virtuales deben ser al menos los marcos físicos.");
        if (c.PageSizeKb <= 0) errors.Add("El tamaño de página debe ser mayor que cero.");
        if (c.NruReferenceResetInterval <= 0) errors.Add("El intervalo NRU debe ser mayor que cero.");
        foreach (var p in processes)
        {
            if (p.CpuBurst < 0) errors.Add($"{p.Name}: la ráfaga no puede ser negativa.");
            if (p.ArrivalTime < 0) errors.Add($"{p.Name}: la llegada no puede ser negativa.");
            if (p.PageCount <= 0 || p.PageCount > c.VirtualPageCount) errors.Add($"{p.Name}: cantidad de páginas fuera del espacio virtual.");
            try
            {
                if (PageAccessParser.Parse(p.PageReferencePattern).Any(a => a.PageNumber < 0 || a.PageNumber >= p.PageCount))
                    errors.Add($"{p.Name}: una referencia excede sus {p.PageCount} páginas.");
            }
            catch (ArgumentException ex) { errors.Add($"{p.Name}: {ex.Message}"); }
            if ((p.BlockAfterCpuTicks == 0) != (p.BlockDurationTicks == 0)) errors.Add($"{p.Name}: configure ambos valores de E/S o déjelos en cero.");
        }
        return errors;
    }
}
