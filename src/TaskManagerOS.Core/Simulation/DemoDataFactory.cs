using TaskManagerOS.Core.Models;

namespace TaskManagerOS.Core.Simulation;

public static class DemoDataFactory
{
    public static ProjectData Create()
    {
        var definitions = new List<ProcessDefinition>
        {
            New("Editor de texto", 6, 2, 5, "0R,1W,2R,0R,3W"),
            New("Navegador", 8, 3, 7, "0R,1R,2W,3R,4W,1R", 3, 2),
            New("Reproductor multimedia", 5, 1, 6, "0R,1R,2R,3W"),
            New("Compilador", 7, 2, 8, "0R,1W,2W,4R,5W,0R"),
            New("Antivirus", 4, 4, 5, "0R,2R,4W,1R")
        };
        var arrivals = new[] { 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        var order = new[] { 0, 1, 2, 3, 4, 0, 1, 3, 2, 0 };
        var instances = order.Select((d, i) =>
        {
            var p = ProcessInstance.From(definitions[d], arrivals[i]); p.Name += $" #{i + 1}"; return p;
        }).ToList();
        return new ProjectData { Programs = definitions, ExecutionList = instances, Configuration = new() };
    }

    private static ProcessDefinition New(string name, int burst, int priority, int pages, string pattern, int blockAfter = 0, int blockFor = 0) =>
        new() { Name = name, CpuBurst = burst, Priority = priority, PageCount = pages, PageReferencePattern = pattern,
            BlockAfterCpuTicks = blockAfter, BlockDurationTicks = blockFor };
}
