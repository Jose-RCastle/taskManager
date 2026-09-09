using TaskManagerOS.Core.Models;
using TaskManagerOS.Core.Presentation;

namespace TaskManagerOS.Core.Memory;

public sealed record MemoryReference(PageIdentity Page, string ProcessName, MemoryAccessType Type);
public sealed record ReplacementContext(IReadOnlyList<PageFrame> Frames, IReadOnlyList<MemoryReference> References, int CurrentIndex, int Tick);

public interface IPageReplacementAlgorithm
{
    string Name { get; }
    int SelectVictim(ReplacementContext context);
}

public sealed class OptimalPageReplacement : IPageReplacementAlgorithm
{
    public string Name => "Óptimo";
    public int SelectVictim(ReplacementContext context) => context.Frames
        .Where(f => f.Page is not null)
        .Select(f => new { f.FrameNumber, Next = NextUse(context, f.Page!.Value) })
        .OrderByDescending(x => x.Next).ThenBy(x => x.FrameNumber).First().FrameNumber;

    private static int NextUse(ReplacementContext c, PageIdentity page)
    {
        for (var i = c.CurrentIndex + 1; i < c.References.Count; i++) if (c.References[i].Page == page) return i;
        return int.MaxValue;
    }
}

public sealed class NruPageReplacement : IPageReplacementAlgorithm
{
    public string Name => "NRU";
    public static int ClassOf(PageFrame f) => (f.Referenced ? 2 : 0) + (f.Modified ? 1 : 0);
    public int SelectVictim(ReplacementContext context) => context.Frames.Where(f => f.Page is not null)
        .OrderBy(ClassOf).ThenBy(f => f.FrameNumber).First().FrameNumber;
}

public sealed record MemoryOutcome(bool Hit, PageIdentity? Evicted, PageIdentity? Loaded, string Decision);

public sealed class MemoryManager
{
    private readonly List<PageFrame> _frames;
    private readonly IPageReplacementAlgorithm _algorithm;
    private readonly int _resetInterval;
    public IReadOnlyList<PageFrame> Frames => _frames;

    public MemoryManager(OperatingSystemConfiguration config)
    {
        _frames = Enumerable.Range(0, config.PhysicalFrameCount).Select(i => new PageFrame { FrameNumber = i }).ToList();
        _algorithm = config.PageReplacementAlgorithm == PageReplacementAlgorithmType.Optimal ? new OptimalPageReplacement() : new NruPageReplacement();
        _resetInterval = config.NruReferenceResetInterval;
    }

    public MemoryOutcome Access(MemoryReference reference, IReadOnlyList<MemoryReference> all, int index, int tick)
    {
        if (_algorithm is NruPageReplacement && tick > 0 && tick % _resetInterval == 0)
            foreach (var f in _frames) f.Referenced = false;
        var found = _frames.FirstOrDefault(f => f.Page == reference.Page);
        if (found is not null)
        {
            found.Referenced = true; if (reference.Type == MemoryAccessType.Write) found.Modified = true;
            return new(true, null, null, "la página ya estaba cargada (acierto)");
        }
        var target = _frames.FirstOrDefault(f => f.Page is null);
        PageIdentity? evicted = null;
        string reason;
        if (target is null)
        {
            target = _frames[_algorithm.SelectVictim(new(_frames, all, index, tick))];
            evicted = target.Page;
            reason = _algorithm is OptimalPageReplacement
                ? $"Óptimo retiró {DisplayText.Page(target.ProcessName ?? "Proceso", evicted!.Value.PageNumber)} por ser la referencia con uso futuro más lejano"
                : $"NRU retiró {DisplayText.Page(target.ProcessName ?? "Proceso", evicted!.Value.PageNumber)} de la clase {NruPageReplacement.ClassOf(target)}";
        }
        else reason = $"se asignó el marco libre {target.FrameNumber}";
        target.Page = reference.Page; target.ProcessName = reference.ProcessName; target.Referenced = true;
        target.Modified = reference.Type == MemoryAccessType.Write; target.LoadedAt = tick;
        return new(false, evicted, reference.Page, reason);
    }

    public List<PageFrame> Snapshot() => _frames.Select(f => f.Copy()).ToList();
    public List<PageTableEntry> PageTable() => _frames.Where(f => f.Page is not null).Select(f => new PageTableEntry
    { Page = f.Page!.Value, ProcessName = f.ProcessName!, FrameNumber = f.FrameNumber, Present = true, Referenced = f.Referenced, Modified = f.Modified }).ToList();
}
