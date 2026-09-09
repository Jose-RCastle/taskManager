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
    private readonly Dictionary<string, Button> _navigation = [];

    public MainForm(AppState state)
    {
        _state = state; BuildUI(); ConfigureLayout(); WireEvents(); ApplyTheme(); ShowView("Inicio");
    }
    private void BuildUI()
    {
        Text = "TaskManagerOS 0.1.1"; MinimumSize = new(1180, 720); WindowState = FormWindowState.Maximized; AutoScaleMode = AutoScaleMode.Dpi;
        var side = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 246, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new(16, 22, 16, 12), BackColor = AppTheme.Surface };
        side.Controls.Add(new Label { Text = "TaskManager\nOS", AutoSize = false, Size = new(210, 76), ForeColor = AppTheme.Text, Font = new("Segoe UI Semibold", 19), TextAlign = ContentAlignment.MiddleLeft, Padding = new(6, 0, 0, 0) });
        foreach (var name in new[] { "Inicio", "Programas", "Lista de ejecución", "Configuración SO", "Emular MMU" })
        { var b = AppTheme.Button(name, AppTheme.Surface2); b.Width = 210; b.Height = 44; b.TextAlign = ContentAlignment.MiddleLeft; b.Tag = name; b.Margin = new(2, 5, 2, 5); _navigation[name] = b; side.Controls.Add(b); }
        var exit = AppTheme.Button("Salir", AppTheme.Error); exit.Width = 210; exit.Margin = new(2, 20, 2, 2); exit.Click += (_, _) => Close(); side.Controls.Add(exit);
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
        foreach (var (key, button) in _navigation) { var active = key == name; button.BackColor = active ? AppTheme.Accent : AppTheme.Surface2; button.ForeColor = active ? Color.White : AppTheme.Muted; button.Font = new("Segoe UI Semibold", active ? 10f : 9.5f); }
        Control view = name switch { "Programas" => new ProgramsView(_state), "Lista de ejecución" => new ExecutionListView(_state), "Configuración SO" => new ConfigurationView(_state), "Emular MMU" => new SimulationView(_state), _ => new HomeView(_state) };
        view.Dock = DockStyle.Fill; _content.Controls.Add(view); _status.Text = $"Módulo: {name}";
    }
}
