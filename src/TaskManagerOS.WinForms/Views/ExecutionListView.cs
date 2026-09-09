using TaskManagerOS.Core.Models;
using TaskManagerOS.WinForms.Services;
using TaskManagerOS.WinForms.Theme;

namespace TaskManagerOS.WinForms.Views;

public sealed class ExecutionListView : UserControl
{
    private readonly AppState _state; private readonly ComboBox _programs = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill }; private readonly NumericUpDown _arrival = Num(0, 999), _burst = Num(1, 999), _priority = Num(0, 99);
    public ExecutionListView(AppState state) { _state = state; BuildUI(); WireEvents(); ApplyTheme(); RefreshAll(); }
    private void BuildUI()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 }; layout.RowStyles.Add(new(SizeType.Absolute, 100)); layout.RowStyles.Add(new(SizeType.Percent, 100)); layout.RowStyles.Add(new(SizeType.Absolute, 66));
        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new(4), WrapContents = true, AutoScroll = true }; top.Controls.AddRange([_programs, AppTheme.Button("Agregar"), AppTheme.Button("Repetir"), AppTheme.Button("Eliminar", AppTheme.Error), AppTheme.Button("Subir", AppTheme.Surface2), AppTheme.Button("Bajar", AppTheme.Surface2), AppTheme.Button("Vaciar", AppTheme.Error)]);
        var edit = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new(4), WrapContents = true, AutoScroll = true };
        edit.Controls.AddRange([Label("Llegada"), _arrival, Label("Ráfaga"), _burst, Label("Prioridad"), _priority, AppTheme.Button("Aplicar cambios")]);
        layout.Controls.Add(top, 0, 0); layout.Controls.Add(_grid, 0, 1); layout.Controls.Add(edit, 0, 2); Controls.Add(layout);
    }
    private void WireEvents()
    {
        Button("Agregar").Click += (_, _) => Add(false); Button("Repetir").Click += (_, _) => Add(true); Button("Eliminar").Click += (_, _) => Remove(); Button("Vaciar").Click += (_, _) => { if (_state.Project.ExecutionList.Count > 0 && MessageBox.Show("¿Vaciar toda la lista de ejecución?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _state.Project.ExecutionList.Clear(); Commit(); } };
        Button("Subir").Click += (_, _) => Move(-1); Button("Bajar").Click += (_, _) => Move(1); Button("Aplicar cambios").Click += (_, _) => Apply(); _grid.SelectionChanged += (_, _) => LoadSelection();
    }
    private void Add(bool repeat)
    {
        ProcessInstance? p = repeat && Selected() is { } selected ? selected.Copy() : _programs.SelectedItem is ProcessDefinition d ? ProcessInstance.From(d) : null;
        if (p is null) return; p.Id = Guid.NewGuid(); p.Name = $"{p.Name.Split('#')[0].Trim()} #{_state.Project.ExecutionList.Count + 1}"; p.Status = ProcessStatus.New; p.IsFinished = false; _state.Project.ExecutionList.Add(p); Commit();
    }
    private void Remove() { var p = Selected(); if (p is null) return; _state.Project.ExecutionList.Remove(p); Commit(); }
    private void Move(int delta) { var p = Selected(); if (p is null) return; var i = _state.Project.ExecutionList.IndexOf(p); var n = i + delta; if (n < 0 || n >= _state.Project.ExecutionList.Count) return; (_state.Project.ExecutionList[i], _state.Project.ExecutionList[n]) = (_state.Project.ExecutionList[n], _state.Project.ExecutionList[i]); Commit(); _grid.Rows[n].Selected = true; }
    private void Apply() { var p = Selected(); if (p is null) return; p.ArrivalTime = (int)_arrival.Value; p.CpuBurst = p.RemainingTime = (int)_burst.Value; p.Priority = (int)_priority.Value; Commit(); }
    private void LoadSelection() { var p = Selected(); var selected = p is not null; foreach (var text in new[] { "Repetir", "Eliminar", "Subir", "Bajar", "Aplicar cambios" }) Button(text).Enabled = selected; if (p is null) return; _arrival.Value = p.ArrivalTime; _burst.Value = p.CpuBurst; _priority.Value = p.Priority; var index = _state.Project.ExecutionList.IndexOf(p); Button("Subir").Enabled = index > 0; Button("Bajar").Enabled = index < _state.Project.ExecutionList.Count - 1; }
    private ProcessInstance? Selected() => _grid.CurrentRow?.Tag is Guid id ? _state.Project.ExecutionList.FirstOrDefault(p => p.Id == id) : null;
    private void Commit() { _state.Save(); RefreshGrid(); }
    private void RefreshAll() { _programs.DataSource = null; _programs.DataSource = _state.Project.Programs; _programs.DisplayMember = nameof(ProcessDefinition.Name); RefreshGrid(); }
    private void RefreshGrid() { _grid.Rows.Clear(); _grid.Columns.Clear(); foreach (var c in new[] { ("Order", "Orden"), ("Name", "Instancia"), ("Arrival", "Llegada"), ("Burst", "CPU"), ("Priority", "Prioridad"), ("Pages", "Páginas") }) _grid.Columns.Add(c.Item1, c.Item2); foreach (var (p, i) in _state.Project.ExecutionList.Select((p, i) => (p, i))) { var r = _grid.Rows.Add(i + 1, p.Name, p.ArrivalTime, p.CpuBurst, p.Priority, p.PageCount); _grid.Rows[r].Tag = p.Id; } _grid.ClearSelection(); LoadSelection(); Button("Vaciar").Enabled = _state.Project.ExecutionList.Count > 0; }
    private Button Button(string text) => Desc(this).OfType<Button>().First(b => b.Text == text); private static IEnumerable<Control> Desc(Control r) => r.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Desc(c)));
    private static Label Label(string text) => new() { Text = text, AutoSize = true, ForeColor = AppTheme.Muted, Margin = new(10, 12, 2, 2) }; private static NumericUpDown Num(int min, int max) => new() { Minimum = min, Maximum = max, Width = 70, Margin = new(2, 7, 4, 2) };
    private void ApplyTheme() { BackColor = AppTheme.Background; AppTheme.Grid(_grid); }
}
