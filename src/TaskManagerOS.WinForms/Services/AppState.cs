using TaskManagerOS.Core.Models;
using TaskManagerOS.Core.Persistence;
using TaskManagerOS.Core.Simulation;

namespace TaskManagerOS.WinForms.Services;

public sealed class AppState
{
    private readonly ProjectStore _store = new();
    public string FilePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskManagerOS", "project.json");
    public ProjectData Project { get; private set; }
    public event EventHandler? Changed;
    public long Revision { get; private set; }

    public AppState()
    {
        try { Project = File.Exists(FilePath) ? _store.Load(FilePath) : DemoDataFactory.Create(); }
        catch { Project = DemoDataFactory.Create(); }
        Save();
    }
    public void Save() { _store.Save(FilePath, Project); Revision++; Changed?.Invoke(this, EventArgs.Empty); }
    public void RestoreDemo() { Project = DemoDataFactory.Create(); Save(); }
}
