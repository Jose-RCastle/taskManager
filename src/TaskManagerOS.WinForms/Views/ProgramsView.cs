using TaskManagerOS.Core.Models;
using TaskManagerOS.Core.Validation;
using TaskManagerOS.WinForms.Services;
using TaskManagerOS.WinForms.Theme;

namespace TaskManagerOS.WinForms.Views;

public sealed class ProgramsView : UserControl
{
    private readonly AppState _state; private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly TextBox _name = new(), _pattern = new(); private readonly NumericUpDown _burst = Num(1, 999), _priority = Num(0, 99), _pages = Num(1, 128), _blockAfter = Num(0, 999), _blockFor = Num(0, 999);
    private Guid? _editing;
    public ProgramsView(AppState state) { _state = state; BuildUI(); WireEvents(); ApplyTheme(); RefreshGrid(); Clear(); }
    private void BuildUI()
    {
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, FixedPanel = FixedPanel.Panel2, Panel1MinSize = 220, Panel2MinSize = 285 };
        split.Resize += (_, _) => { if (split.Height > split.Panel1MinSize + split.Panel2MinSize + split.SplitterWidth) split.SplitterDistance = split.Height - split.Panel2MinSize - split.SplitterWidth; };
        split.Panel1.Controls.Add(_grid); var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new(14), AutoScroll = true };
        form.ColumnStyles.Add(new(SizeType.Absolute, 145)); form.ColumnStyles.Add(new(SizeType.Percent, 50)); form.ColumnStyles.Add(new(SizeType.Absolute, 145)); form.ColumnStyles.Add(new(SizeType.Percent, 50));
        Add(form, "Nombre", _name); Add(form, "Ráfaga CPU", _burst); Add(form, "Prioridad", _priority); Add(form, "Páginas", _pages); Add(form, "Referencias", _pattern); Add(form, "Bloquear tras CPU", _blockAfter); Add(form, "Duración E/S", _blockFor);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true }; foreach (var text in new[] { "Crear / guardar", "Eliminar", "Limpiar", "Restaurar ejemplos" }) actions.Controls.Add(AppTheme.Button(text, text == "Eliminar" ? AppTheme.Error : null));
        form.Controls.Add(actions, 0, form.RowCount); form.SetColumnSpan(actions, 4); split.Panel2.Controls.Add(form); Controls.Add(split);
    }
    private void WireEvents()
    {
        _grid.SelectionChanged += (_, _) => LoadSelected();
        Find("Crear / guardar").Click += (_, _) => Save(); Find("Eliminar").Click += (_, _) => Delete(); Find("Limpiar").Click += (_, _) => Clear();
        Find("Restaurar ejemplos").Click += (_, _) => { _state.RestoreDemo(); RefreshGrid(); Clear(); };
    }
    private void Save()
    {
        try
        {
            PageAccessParser.Parse(_pattern.Text); if (string.IsNullOrWhiteSpace(_name.Text)) throw new ArgumentException("Ingrese un nombre.");
            var d = _editing is null ? new ProcessDefinition() : _state.Project.Programs.First(x => x.Id == _editing);
            d.Name = _name.Text.Trim(); d.CpuBurst = (int)_burst.Value; d.Priority = (int)_priority.Value; d.PageCount = (int)_pages.Value; d.PageReferencePattern = _pattern.Text.Trim(); d.BlockAfterCpuTicks = (int)_blockAfter.Value; d.BlockDurationTicks = (int)_blockFor.Value;
            if (PageAccessParser.Parse(d.PageReferencePattern).Any(a => a.PageNumber >= d.PageCount)) throw new ArgumentException("Una referencia está fuera de la cantidad de páginas.");
            if (_editing is null) _state.Project.Programs.Add(d); _state.Save(); RefreshGrid(); Clear();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Datos inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private void Delete() { if (_editing is null) return; _state.Project.Programs.RemoveAll(x => x.Id == _editing); _state.Save(); RefreshGrid(); Clear(); }
    private void LoadSelected() { if (_grid.CurrentRow?.Tag is not Guid id) return; var d = _state.Project.Programs.First(x => x.Id == id); _editing = id; _name.Text = d.Name; _burst.Value = d.CpuBurst; _priority.Value = d.Priority; _pages.Value = d.PageCount; _pattern.Text = d.PageReferencePattern; _blockAfter.Value = d.BlockAfterCpuTicks; _blockFor.Value = d.BlockDurationTicks; }
    private void Clear() { _editing = null; _name.Text = ""; _pattern.Text = "0R,1W"; _burst.Value = 1; _priority.Value = 1; _pages.Value = 4; _blockAfter.Value = _blockFor.Value = 0; _grid.ClearSelection(); }
    private void RefreshGrid() { _grid.Rows.Clear(); _grid.Columns.Clear(); _grid.Columns.Add("Name", "Programa"); _grid.Columns.Add("Burst", "CPU"); _grid.Columns.Add("Priority", "Prioridad"); _grid.Columns.Add("Pages", "Páginas"); _grid.Columns.Add("Pattern", "Referencias"); foreach (var d in _state.Project.Programs) { var i = _grid.Rows.Add(d.Name, d.CpuBurst, d.Priority, d.PageCount, d.PageReferencePattern); _grid.Rows[i].Tag = d.Id; } }
    private Button Find(string text) => Descendants(this).OfType<Button>().First(b => b.Text == text);
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static NumericUpDown Num(int min, int max) => new() { Minimum = min, Maximum = max, Dock = DockStyle.Fill };
    private static void Add(TableLayoutPanel p, string label, Control c) { var item = p.Controls.Count / 2; var row = item / 2; var column = (item % 2) * 2; if (column == 0) { p.RowCount++; p.RowStyles.Add(new(SizeType.Absolute, 42)); } p.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = AppTheme.Muted, Margin = new(3, 9, 3, 3) }, column, row); c.Dock = DockStyle.Fill; c.Margin = new(3, 5, 12, 5); p.Controls.Add(c, column + 1, row); }
    private void ApplyTheme() { BackColor = AppTheme.Background; _grid.Dock = DockStyle.Fill; AppTheme.Grid(_grid); }
}
