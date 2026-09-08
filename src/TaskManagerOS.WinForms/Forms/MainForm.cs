using TaskManagerOS.WinForms.Services;
using TaskManagerOS.WinForms.Theme;
using TaskManagerOS.WinForms.Views;

namespace TaskManagerOS.WinForms.Forms;

public sealed class MainForm : Form
{
    private readonly AppState _state;
    private readonly Panel _content = new() { Dock = DockStyle.Fill, Padding = new(18), BackColor = AppTheme.Background };
    private readonly Label _title = AppTheme.Heading("Inicio", 20);
    private readonly ToolStripStatusLabel _status = new("Proyecto listo");

    public MainForm(AppState state)
    {
        _state = state; BuildUI(); ConfigureLayout(); WireEvents(); ApplyTheme(); ShowView("Inicio");
    }
    private void BuildUI()
    {
        Text = "TaskManagerOS 0.1"; MinimumSize = new(1180, 720); WindowState = FormWindowState.Maximized; AutoScaleMode = AutoScaleMode.Dpi;
        var side = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 220, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new(12, 24, 12, 12), BackColor = AppTheme.Surface };
        side.Controls.Add(AppTheme.Heading("TaskManagerOS", 18));
        foreach (var name in new[] { "Inicio", "Programas", "Lista de ejecución", "Configuración SO", "Emular MMU" })
        { var b = AppTheme.Button(name, AppTheme.Surface2); b.Width = 190; b.TextAlign = ContentAlignment.MiddleLeft; b.Tag = name; side.Controls.Add(b); }
        var exit = AppTheme.Button("Salir", AppTheme.Error); exit.Width = 190; exit.Click += (_, _) => Close(); side.Controls.Add(exit);
        var header = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = AppTheme.Surface2, Padding = new(20, 15, 0, 0) }; header.Controls.Add(_title);
        var status = new StatusStrip { BackColor = AppTheme.Surface, ForeColor = AppTheme.Muted }; status.Items.Add(_status);
        Controls.Add(_content); Controls.Add(header); Controls.Add(side); Controls.Add(status);
    }
    private void ConfigureLayout() { }
    private void WireEvents()
    {
        foreach (var b in Controls.OfType<FlowLayoutPanel>().SelectMany(p => p.Controls.OfType<Button>()).Where(b => b.Tag is not null))
            b.Click += (_, _) => ShowView((string)b.Tag!);
        _state.Changed += (_, _) => _status.Text = $"Guardado: {_state.FilePath}";
    }
    private void ApplyTheme() { BackColor = AppTheme.Background; ForeColor = AppTheme.Text; Font = new("Segoe UI", 9.5f); }
    private void ShowView(string name)
    {
        _title.Text = name; _content.Controls.Clear();
        Control view = name switch { "Programas" => new ProgramsView(_state), "Lista de ejecución" => new ExecutionListView(_state), "Configuración SO" => new ConfigurationView(_state), "Emular MMU" => new SimulationView(_state), _ => new HomeView(_state) };
        view.Dock = DockStyle.Fill; _content.Controls.Add(view); _status.Text = $"Módulo: {name}";
    }
}
