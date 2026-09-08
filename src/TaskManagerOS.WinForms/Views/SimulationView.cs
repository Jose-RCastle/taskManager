using TaskManagerOS.Core.Models;
using TaskManagerOS.Core.Simulation;
using TaskManagerOS.WinForms.Services;
using TaskManagerOS.WinForms.Theme;

namespace TaskManagerOS.WinForms.Views;

public sealed class SimulationView : UserControl
{
    private readonly AppState _state; private SimulationResult? _result; private int _index = -1;
    private readonly System.Windows.Forms.Timer _timer = new(); private readonly TrackBar _speed = new() { Minimum = 1, Maximum = 10, Value = 5, Width = 130 };
    private readonly Label _tick = Value("Tick: —"), _current = Value("CPU: —"), _ready = Value("Listos: —"), _blocked = Value("Bloqueados: —"), _access = Value("Acceso: —"), _metrics = Value("Métricas: —");
    private readonly DataGridView _processes = new(), _pages = new(); private readonly FlowLayoutPanel _frames = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly TextBox _gantt = new() { ReadOnly = true, Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Horizontal, WordWrap = false }, _log = new() { ReadOnly = true, Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
    public SimulationView(AppState state) { _state = state; BuildUI(); ConfigureLayout(); WireEvents(); ApplyTheme(); }

    private void BuildUI()
    {
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new(2) };
        actions.Controls.AddRange([AppTheme.Button("Iniciar"), AppTheme.Button("Pausar", AppTheme.Surface2), AppTheme.Button("Paso siguiente", AppTheme.Purple), AppTheme.Button("Reiniciar", AppTheme.Error), new Label { Text = "Velocidad", AutoSize = true, ForeColor = AppTheme.Muted, Margin = new(14, 12, 2, 2) }, _speed]);
        var info = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, AutoScroll = true }; info.Controls.AddRange([_tick, _current, _access, _ready, _blocked]);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 560 };
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 }; left.RowStyles.Add(new(SizeType.Percent, 50)); left.RowStyles.Add(new(SizeType.Absolute, 74)); left.RowStyles.Add(new(SizeType.Percent, 50));
        left.Controls.Add(Group("Estados de procesos", _processes), 0, 0); left.Controls.Add(Group("Línea de tiempo / Gantt", _gantt), 0, 1); left.Controls.Add(Group("Historial explicativo", _log), 0, 2);
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 }; right.RowStyles.Add(new(SizeType.Absolute, 125)); right.RowStyles.Add(new(SizeType.Percent, 55)); right.RowStyles.Add(new(SizeType.Percent, 45));
        right.Controls.Add(Group("Marcos físicos", _frames), 0, 0); right.Controls.Add(Group("Tabla de páginas", _pages), 0, 1); right.Controls.Add(Group("Métricas finales", _metrics), 0, 2);
        split.Panel1.Controls.Add(left); split.Panel2.Controls.Add(right); Controls.Add(split); Controls.Add(info); Controls.Add(actions);
    }
    private void ConfigureLayout() { _processes.Dock = DockStyle.Fill; _pages.Dock = DockStyle.Fill; _timer.Interval = 600; }
    private void WireEvents()
    {
        Find("Iniciar").Click += (_, _) => { if (!Prepare()) return; _timer.Start(); };
        Find("Pausar").Click += (_, _) => _timer.Stop(); Find("Paso siguiente").Click += (_, _) => { if (Prepare()) Next(); };
        Find("Reiniciar").Click += (_, _) => Reset(); _timer.Tick += (_, _) => Next(); _speed.ValueChanged += (_, _) => _timer.Interval = 1100 - _speed.Value * 100;
    }
    private bool Prepare()
    {
        if (_result is not null) return _index < _result.Steps.Count - 1;
        try { _result = new SimulationEngine().Run(_state.Project.ExecutionList, _state.Project.Configuration); _index = -1; _log.Clear(); _gantt.Clear(); return true; }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se puede simular", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
    }
    private void Next()
    {
        if (_result is null || ++_index >= _result.Steps.Count) { _timer.Stop(); return; }
        Render(_result.Steps[_index]); if (_index == _result.Steps.Count - 1) { _timer.Stop(); ShowMetrics(_result.Metrics); }
    }
    private void Render(SimulationStep s)
    {
        _tick.Text = $"Tick: {s.Tick}"; _current.Text = $"CPU: {s.CurrentProcessName}"; _ready.Text = $"Listos: {string.Join(", ", s.ReadyQueue)}"; _blocked.Text = $"Bloqueados: {string.Join(", ", s.BlockedProcesses)}"; _access.Text = $"Acceso: {s.MemoryAccess?.ToString() ?? "—"}";
        _gantt.AppendText($"[{s.Tick}:{Short(s.CurrentProcessName)}] "); _log.AppendText(s.Explanation + Environment.NewLine); _log.SelectionStart = _log.TextLength; _log.ScrollToCaret();
        _processes.Rows.Clear(); _processes.Columns.Clear(); _processes.Columns.Add("Name", "Proceso"); _processes.Columns.Add("State", "Estado");
        foreach (var p in _state.Project.ExecutionList) { var state = s.ProcessStates.GetValueOrDefault(p.Id, ProcessStatus.Finished); var r = _processes.Rows.Add(p.Name, Spanish(state)); _processes.Rows[r].DefaultCellStyle.ForeColor = StateColor(state); }
        _pages.DataSource = s.PageTable.Select(e => new { Proceso = e.ProcessName, Página = e.Page.PageNumber, Marco = e.FrameNumber, R = e.Referenced, M = e.Modified }).ToList();
        _frames.Controls.Clear(); foreach (var f in s.Frames) { var recent = s.LoadedPage == f.Page; var hit = s.PageHit == true && s.CurrentProcessId == f.Page?.ProcessInstanceId && s.MemoryAccess?.PageNumber == f.Page?.PageNumber; _frames.Controls.Add(new Label { Text = f.Page is null ? $"Marco {f.FrameNumber}\nLibre" : $"Marco {f.FrameNumber}\n{f.ProcessName}\nPág. {f.Page.Value.PageNumber}  R:{B(f.Referenced)} M:{B(f.Modified)}", Size = new(150, 78), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, BackColor = recent ? AppTheme.Success : hit ? AppTheme.Purple : f.Page is null ? AppTheme.Surface2 : AppTheme.Accent, Margin = new(5) }); }
    }
    private void ShowMetrics(SimulationMetrics m) => _metrics.Text = $"Ticks totales: {m.TotalTicks}     Finalizados: {m.CompletedProcesses}\nReferencias: {m.TotalReferences}     Aciertos: {m.Hits}     Fallos: {m.PageFaults}\nTasa de aciertos: {m.HitRate:P1}     Tasa de fallos: {m.FaultRate:P1}\nRendimiento MMU: {m.MmuPerformance:P1}\nEspera por proceso: {string.Join(" · ", _state.Project.ExecutionList.Select(p => $"{Short(p.Name)}={m.WaitingTimeByProcess.GetValueOrDefault(p.Id)}"))}";
    private void Reset() { _timer.Stop(); _result = null; _index = -1; _tick.Text = "Tick: —"; _current.Text = "CPU: —"; _access.Text = "Acceso: —"; _ready.Text = "Listos: —"; _blocked.Text = "Bloqueados: —"; _metrics.Text = "Métricas: —"; _frames.Controls.Clear(); _processes.DataSource = null; _processes.Rows.Clear(); _pages.DataSource = null; _log.Clear(); _gantt.Clear(); }
    private static GroupBox Group(string title, Control content) { var g = new GroupBox { Text = title, Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, Padding = new(8) }; g.Controls.Add(content); return g; }
    private Button Find(string t) => Desc(this).OfType<Button>().First(b => b.Text == t); private static IEnumerable<Control> Desc(Control r) => r.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Desc(c)));
    private static Label Value(string t) => new() { Text = t, AutoSize = true, ForeColor = AppTheme.Text, BackColor = AppTheme.Surface2, Padding = new(8, 5, 8, 5), Margin = new(3) };
    private static string Short(string s) => s.Length > 14 ? s[..14] : s; private static string B(bool b) => b ? "1" : "0";
    private static string Spanish(ProcessStatus s) => s switch { ProcessStatus.New => "Nuevo", ProcessStatus.Ready => "Listo", ProcessStatus.Running => "Ejecución", ProcessStatus.Blocked => "Bloqueado", _ => "Finalizado" };
    private static Color StateColor(ProcessStatus s) => s switch { ProcessStatus.Ready => AppTheme.Success, ProcessStatus.Running => AppTheme.Running, ProcessStatus.Blocked => AppTheme.Blocked, ProcessStatus.Finished => AppTheme.Muted, _ => Color.Gray };
    private void ApplyTheme() { BackColor = AppTheme.Background; ForeColor = AppTheme.Text; AppTheme.Grid(_processes); AppTheme.Grid(_pages); foreach (var t in new[] { _log, _gantt }) { t.BackColor = AppTheme.Surface; t.ForeColor = AppTheme.Text; t.BorderStyle = BorderStyle.FixedSingle; } }
}
