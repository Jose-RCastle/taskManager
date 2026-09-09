using TaskManagerOS.WinForms.Services;
using TaskManagerOS.WinForms.Theme;
using TaskManagerOS.Core.Presentation;

namespace TaskManagerOS.WinForms.Views;

public sealed class HomeView : UserControl
{
    private readonly AppState _state;
    public HomeView(AppState state) { _state = state; BuildUI(); ApplyTheme(); }
    private void BuildUI()
    {
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3, Padding = new(8) };
        for (var i = 0; i < 3; i++) grid.ColumnStyles.Add(new(SizeType.Percent, 33.33f));
        grid.RowStyles.Add(new(SizeType.Percent, 42)); grid.RowStyles.Add(new(SizeType.Percent, 42)); grid.RowStyles.Add(new(SizeType.Percent, 16));
        var p = _state.Project;
        var cards = new[] { ("Programas", p.Programs.Count.ToString()), ("Instancias", p.ExecutionList.Count.ToString()),
            ("Planificación", DisplayText.Scheduler(p.Configuration.SchedulingAlgorithm)), ("MMU", DisplayText.Replacement(p.Configuration.PageReplacementAlgorithm)),
            ("Marcos", p.Configuration.PhysicalFrameCount.ToString()), ("Estado", "Listo para simular") };
        foreach (var (title, value) in cards)
        {
            var panel = new Panel { Height = 125, Dock = DockStyle.Fill, Margin = new(8), BackColor = AppTheme.Surface2, Padding = new(18) };
            panel.Controls.Add(new Label { Text = value, Dock = DockStyle.Fill, ForeColor = AppTheme.Text, Font = new("Segoe UI Semibold", 17), TextAlign = ContentAlignment.MiddleLeft });
            panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 28, ForeColor = AppTheme.Muted, Font = new("Segoe UI", 10) }); grid.Controls.Add(panel);
        }
        var hint = new Label { Text = "Siguiente paso recomendado: revise la lista, configure los algoritmos y abra Emular MMU.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = AppTheme.Muted };
        grid.Controls.Add(hint, 0, 2); grid.SetColumnSpan(hint, 3); Controls.Add(grid);
    }
    private void ApplyTheme() { BackColor = AppTheme.Background; }
}
