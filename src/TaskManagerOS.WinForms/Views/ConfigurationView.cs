using TaskManagerOS.Core.Models;
using TaskManagerOS.Core.Validation;
using TaskManagerOS.Core.Presentation;
using TaskManagerOS.WinForms.Services;
using TaskManagerOS.WinForms.Theme;

namespace TaskManagerOS.WinForms.Views;

public sealed class ConfigurationView : UserControl
{
    private readonly AppState _state; private readonly NumericUpDown _quantum = Num(1, 100), _frames = Num(2, 32), _pages = Num(2, 128), _size = Num(1, 1024), _reset = Num(1, 1000);
    private readonly ComboBox _scheduler = new() { DropDownStyle = ComboBoxStyle.DropDownList }, _replacement = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    public ConfigurationView(AppState state) { _state = state; BuildUI(); WireEvents(); ApplyTheme(); LoadValues(); }
    private void BuildUI()
    {
        var form = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new(28) }; form.ColumnStyles.Add(new(SizeType.Absolute, 330)); form.ColumnStyles.Add(new(SizeType.Percent, 100));
        Add(form, "Quantum (> 0)", _quantum); Add(form, "Marcos físicos (2–32)", _frames); Add(form, "Páginas virtuales", _pages); Add(form, "Tamaño de página (KB)", _size); Add(form, "Reinicio del bit R de NRU (ticks)", _reset); Add(form, "Planificador", _scheduler); Add(form, "Reemplazo de páginas", _replacement);
        var save = AppTheme.Button("Guardar configuración"); form.Controls.Add(save, 1, form.RowCount); Controls.Add(form);
    }
    private void WireEvents() => Desc(this).OfType<Button>().Single().Click += (_, _) => Save();
    private void LoadValues() { _scheduler.Format += (_, e) => e.Value = DisplayText.Scheduler((SchedulingAlgorithmType)e.ListItem!); _replacement.Format += (_, e) => e.Value = DisplayText.Replacement((PageReplacementAlgorithmType)e.ListItem!); _scheduler.DataSource = Enum.GetValues<SchedulingAlgorithmType>(); _replacement.DataSource = Enum.GetValues<PageReplacementAlgorithmType>(); var c = _state.Project.Configuration; _quantum.Value = c.Quantum; _frames.Value = c.PhysicalFrameCount; _pages.Value = c.VirtualPageCount; _size.Value = c.PageSizeKb; _reset.Value = c.NruReferenceResetInterval; _scheduler.SelectedItem = c.SchedulingAlgorithm; _replacement.SelectedItem = c.PageReplacementAlgorithm; }
    private void Save()
    {
        var c = new OperatingSystemConfiguration { Quantum = (int)_quantum.Value, PhysicalFrameCount = (int)_frames.Value, VirtualPageCount = (int)_pages.Value, PageSizeKb = (int)_size.Value, NruReferenceResetInterval = (int)_reset.Value, SchedulingAlgorithm = (SchedulingAlgorithmType)_scheduler.SelectedItem!, PageReplacementAlgorithm = (PageReplacementAlgorithmType)_replacement.SelectedItem! };
        var errors = ProjectValidator.Validate(c, _state.Project.ExecutionList); if (errors.Count > 0) { MessageBox.Show(string.Join(Environment.NewLine, errors), "Configuración inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        _state.Project.Configuration = c; _state.Save(); MessageBox.Show("Configuración guardada.", "TaskManagerOS", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    private static void Add(TableLayoutPanel p, string text, Control c) { var row = p.RowCount++; p.RowStyles.Add(new(SizeType.Absolute, 50)); p.Controls.Add(new Label { Text = text, AutoSize = true, ForeColor = AppTheme.Muted, Margin = new(3, 11, 3, 3) }, 0, row); c.Dock = DockStyle.Fill; c.Margin = new(3, 7, 3, 7); p.Controls.Add(c, 1, row); }
    private static NumericUpDown Num(int min, int max) => new() { Minimum = min, Maximum = max }; private static IEnumerable<Control> Desc(Control r) => r.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Desc(c)));
    private void ApplyTheme() { BackColor = AppTheme.Background; ForeColor = AppTheme.Text; }
}
