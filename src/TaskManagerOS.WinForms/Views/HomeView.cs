using TaskManagerOS.WinForms.Services;
using TaskManagerOS.WinForms.Theme;

namespace TaskManagerOS.WinForms.Views;

public sealed class HomeView : UserControl
{
    private readonly AppState _state;
    public HomeView(AppState state) { _state = state; BuildUI(); ApplyTheme(); }
    private void BuildUI()
    {
        var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, Padding = new(8) };
        for (var i = 0; i < 3; i++) grid.ColumnStyles.Add(new(SizeType.Percent, 33.33f));
        var p = _state.Project;
        var cards = new[] { ("Programas", p.Programs.Count.ToString()), ("Instancias", p.ExecutionList.Count.ToString()),
            ("Planificación", p.Configuration.SchedulingAlgorithm.ToString()), ("MMU", p.Configuration.PageReplacementAlgorithm.ToString()),
            ("Marcos", p.Configuration.PhysicalFrameCount.ToString()), ("Estado", "Listo para simular") };
        foreach (var (title, value) in cards)
        {
            var panel = new Panel { Height = 125, Dock = DockStyle.Fill, Margin = new(8), BackColor = AppTheme.Surface2, Padding = new(18) };
            panel.Controls.Add(new Label { Text = value, Dock = DockStyle.Fill, ForeColor = AppTheme.Text, Font = new("Segoe UI Semibold", 17), TextAlign = ContentAlignment.MiddleLeft });
            panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 28, ForeColor = AppTheme.Muted, Font = new("Segoe UI", 10) }); grid.Controls.Add(panel);
        }
        Controls.Add(grid); Controls.Add(new Label { Text = "Simulador académico seguro: solo utiliza procesos ficticios y nunca accede a procesos de Windows.", Dock = DockStyle.Bottom, Height = 45, ForeColor = AppTheme.Muted });
    }
    private void ApplyTheme() { BackColor = AppTheme.Background; }
}
