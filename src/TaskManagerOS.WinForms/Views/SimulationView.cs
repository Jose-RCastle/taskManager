using TaskManagerOS.Core.Models;
using TaskManagerOS.Core.Simulation;
using TaskManagerOS.WinForms.Services;
using TaskManagerOS.WinForms.Theme;

namespace TaskManagerOS.WinForms.Views;

public sealed class SimulationView : UserControl
{
    private readonly AppState _state;
    private SimulationResult? _result;
    private long _resultRevision = -1;
    private int _index = -1;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 600 };
    private readonly TrackBar _speed = new() { Minimum = 1, Maximum = 10, Value = 5, Width = 130 };
    private readonly Label _tick = Value("Paso —"), _current = Value("CPU: —"), _ready = Value("Listos: 0"), _blocked = Value("Bloqueados: 0"), _access = Value("Acceso: —");
    private readonly DataGridView _processes = new(), _pages = new();
    private readonly FlowLayoutPanel _frames = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true }, _gantt = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight };
    private readonly TextBox _metrics = new() { ReadOnly = true, Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, WordWrap = true }, _log = new() { ReadOnly = true, Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, WordWrap = true };

    public SimulationView(AppState state) { _state = state; BuildUI(); WireEvents(); ApplyTheme(); }

    private void BuildUI()
    {
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 54, Padding = new(2), WrapContents = false, AutoScroll = true };
        actions.Controls.AddRange([AppTheme.Button("Iniciar"), AppTheme.Button("Pausar", AppTheme.Surface2), AppTheme.Button("Paso siguiente", AppTheme.Purple), AppTheme.Button("Reiniciar", AppTheme.Error), new Label { Text = "Velocidad", AutoSize = true, ForeColor = AppTheme.Muted, Margin = new(14, 12, 2, 2) }, _speed]);
        var info = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, AutoScroll = true, WrapContents = false }; info.Controls.AddRange([_tick, _current, _access, _ready, _blocked]);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, Panel1MinSize = 460, Panel2MinSize = 430 };
        split.Resize += (_, _) => { if (split.Width > 900) split.SplitterDistance = Math.Clamp((int)(split.Width * .56), split.Panel1MinSize, split.Width - split.Panel2MinSize - split.SplitterWidth); };
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        left.RowStyles.Add(new(SizeType.Percent, 44)); left.RowStyles.Add(new(SizeType.Absolute, 118)); left.RowStyles.Add(new(SizeType.Percent, 56));
        left.Controls.Add(Group("Estados de procesos", _processes), 0, 0); left.Controls.Add(Group("Línea de tiempo / Gantt", _gantt), 0, 1); left.Controls.Add(Group("Historial explicativo", _log), 0, 2);
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        right.RowStyles.Add(new(SizeType.Absolute, 190)); right.RowStyles.Add(new(SizeType.Percent, 52)); right.RowStyles.Add(new(SizeType.Percent, 48));
        right.Controls.Add(Group("Marcos físicos", _frames), 0, 0); right.Controls.Add(Group("Tabla de páginas", _pages), 0, 1); right.Controls.Add(Group("Métricas", _metrics), 0, 2);
        split.Panel1.Controls.Add(left); split.Panel2.Controls.Add(right); Controls.Add(split); Controls.Add(info); Controls.Add(actions);
    }

    private void WireEvents()
    {
        Find("Iniciar").Click += (_, _) => { if (Prepare()) { UpdateButtons(true); _timer.Start(); } };
        Find("Pausar").Click += (_, _) => { _timer.Stop(); UpdateButtons(false); };
        Find("Paso siguiente").Click += (_, _) => { if (Prepare()) Next(); };
        Find("Reiniciar").Click += (_, _) => Reset(true);
        _timer.Tick += (_, _) => Next();
        _speed.ValueChanged += (_, _) => _timer.Interval = 1100 - _speed.Value * 100;
        _state.Changed += StateChanged;
        Disposed += (_, _) => { _timer.Stop(); _state.Changed -= StateChanged; };
        UpdateButtons(false);
    }

    private void StateChanged(object? sender, EventArgs e) => Reset(false);

    private bool Prepare()
    {
        if (_result is not null && _resultRevision == _state.Revision) return _index < _result.Steps.Count - 1;
        try
        {
            _result = new SimulationEngine().Run(_state.Project.ExecutionList, _state.Project.Configuration);
            _resultRevision = _state.Revision; _index = -1; _log.Clear(); _metrics.Clear(); BuildGantt();
            return _result.Steps.Count > 0;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se puede simular", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
    }

    private void Next()
    {
        if (_result is null || ++_index >= _result.Steps.Count) { Finish(); return; }
        Render(_result.Steps[_index]);
        if (_index == _result.Steps.Count - 1) Finish();
    }

    private void Render(SimulationStep step)
    {
        _tick.Text = $"Paso {_index + 1} de {_result!.Steps.Count}"; _current.Text = $"CPU: {step.CurrentProcessName}";
        _ready.Text = $"Listos: {step.ReadyQueue.Count}"; _blocked.Text = $"Bloqueados: {step.BlockedProcesses.Count}";
        _access.Text = step.MemoryAccess is null ? "Acceso: —" : $"Acceso: {step.CurrentProcessName} — Página {step.MemoryAccess.PageNumber} ({(step.MemoryAccess.Type == MemoryAccessType.Read ? "lectura" : "escritura")})";
        _log.AppendText(step.Explanation + Environment.NewLine); _log.SelectionStart = _log.TextLength; _log.ScrollToCaret();
        _processes.Rows.Clear();
        foreach (var p in _state.Project.ExecutionList) { var state = step.ProcessStates.GetValueOrDefault(p.Id, ProcessStatus.Finished); var row = _processes.Rows.Add(p.Name, Spanish(state)); _processes.Rows[row].DefaultCellStyle.ForeColor = StateColor(state); }
        _pages.DataSource = step.PageTable.Select(e => new { Proceso = e.ProcessName, Página = e.Page.PageNumber, Marco = e.FrameNumber, R = B(e.Referenced), M = B(e.Modified) }).ToList();
        _frames.Controls.Clear();
        foreach (var frame in step.Frames)
        {
            var loaded = step.LoadedPage == frame.Page, evicted = step.EvictedPage.HasValue && loaded;
            var hit = step.PageHit == true && step.CurrentProcessId == frame.Page?.ProcessInstanceId && step.MemoryAccess?.PageNumber == frame.Page?.PageNumber;
            _frames.Controls.Add(new Label { Text = frame.Page is null ? $"Marco {frame.FrameNumber}\nLIBRE" : $"Marco {frame.FrameNumber} · OCUPADO\n{frame.ProcessName}\nPágina {frame.Page.Value.PageNumber}   R: {B(frame.Referenced)}   M: {B(frame.Modified)}", Size = new(190, 76), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, BackColor = evicted ? AppTheme.Blocked : loaded ? AppTheme.Success : hit ? AppTheme.Purple : frame.Page is null ? AppTheme.Surface2 : AppTheme.Accent, Margin = new(5) });
        }
        HighlightGantt(step.Tick);
    }

    private void BuildGantt()
    {
        _gantt.Controls.Clear(); if (_result is null) return;
        foreach (var segment in _result.Steps.GroupConsecutiveBy(s => s.CurrentProcessId))
        {
            var first = segment.First(); var last = segment.Last(); var idle = first.CurrentProcessId is null;
            var card = new Label { Tag = (first.Tick, last.Tick), Text = $"{(idle ? "CPU inactiva" : Short(first.CurrentProcessName))}\n{(first.Tick == last.Tick ? $"Tick {first.Tick + 1}" : $"Ticks {first.Tick + 1}–{last.Tick + 1}")}", AutoSize = false, Size = new(Math.Max(108, segment.Count() * 42), 65), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, BackColor = idle ? AppTheme.Surface2 : ProcessColor(first.CurrentProcessId!.Value), Margin = new(4) };
            _gantt.Controls.Add(card);
        }
    }

    private void HighlightGantt(int tick)
    {
        foreach (var card in _gantt.Controls.OfType<Label>()) { var range = ((int Start, int End))card.Tag!; card.BorderStyle = tick >= range.Start && tick <= range.End ? BorderStyle.Fixed3D : BorderStyle.None; card.Font = new("Segoe UI Semibold", tick >= range.Start && tick <= range.End ? 10f : 9f); }
    }

    private void Finish()
    {
        _timer.Stop(); if (_result is null) return;
        _tick.Text = $"Simulación finalizada · Paso {_result.Steps.Count} de {_result.Steps.Count}"; _current.Text = "CPU: —"; _access.Text = "Acceso: —"; _ready.Text = "Listos: 0"; _blocked.Text = "Bloqueados: 0";
        ShowMetrics(_result.Metrics); UpdateButtons(false, true);
    }

    private void ShowMetrics(SimulationMetrics m) => _metrics.Text = $"Ticks totales: {m.TotalTicks}{Environment.NewLine}Procesos finalizados: {m.CompletedProcesses} de {_result?.InputProcessCount}{Environment.NewLine}Referencias: {m.TotalReferences}   Aciertos: {m.Hits}   Fallos: {m.PageFaults}{Environment.NewLine}Tasa de aciertos: {m.HitRate:P1}   Tasa de fallos: {m.FaultRate:P1}{Environment.NewLine}Rendimiento MMU: {m.MmuPerformance:P1}{Environment.NewLine}{Environment.NewLine}Espera por proceso:{Environment.NewLine}{string.Join(Environment.NewLine, _state.Project.ExecutionList.Select(p => $"• {p.Name}: {m.WaitingTimeByProcess.GetValueOrDefault(p.Id)} ticks"))}";

    private void Reset(bool keepCurrentSimulation)
    {
        _timer.Stop(); if (!keepCurrentSimulation || _resultRevision != _state.Revision) { _result = null; _resultRevision = -1; }
        _index = -1; _tick.Text = "Paso —"; _current.Text = "CPU: —"; _access.Text = "Acceso: —"; _ready.Text = "Listos: 0"; _blocked.Text = "Bloqueados: 0"; _metrics.Text = "Métricas: —"; _frames.Controls.Clear(); _processes.Rows.Clear(); _pages.DataSource = null; _log.Clear();
        if (keepCurrentSimulation && _result is not null) BuildGantt(); else _gantt.Controls.Clear(); UpdateButtons(false);
    }

    private void UpdateButtons(bool running, bool completed = false) { Find("Iniciar").Enabled = !running && !completed; Find("Pausar").Enabled = running; Find("Paso siguiente").Enabled = !running && !completed; Find("Reiniciar").Enabled = _result is not null; }
    private static GroupBox Group(string title, Control content) { var box = new GroupBox { Text = title, Dock = DockStyle.Fill, ForeColor = AppTheme.Muted, Padding = new(8) }; box.Controls.Add(content); return box; }
    private Button Find(string text) => Desc(this).OfType<Button>().First(b => b.Text == text);
    private static IEnumerable<Control> Desc(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Desc(c)));
    private static Label Value(string text) => new() { Text = text, AutoSize = true, ForeColor = AppTheme.Text, BackColor = AppTheme.Surface2, Padding = new(8, 5, 8, 5), Margin = new(3) };
    private static string Short(string value) => value.Length > 18 ? value[..18] + "…" : value;
    private static string B(bool value) => value ? "1" : "0";
    private static string Spanish(ProcessStatus value) => value switch { ProcessStatus.New => "Nuevo", ProcessStatus.Ready => "Listo", ProcessStatus.Running => "Ejecución", ProcessStatus.Blocked => "Bloqueado", _ => "Finalizado" };
    private static Color StateColor(ProcessStatus value) => value switch { ProcessStatus.Ready => AppTheme.Success, ProcessStatus.Running => AppTheme.Running, ProcessStatus.Blocked => AppTheme.Blocked, ProcessStatus.Finished => AppTheme.Muted, _ => Color.Gray };
    private static Color ProcessColor(Guid id) { var palette = new[] { AppTheme.Accent, AppTheme.Purple, AppTheme.Success, AppTheme.Blocked, AppTheme.Running }; return palette[(int)((uint)id.GetHashCode() % palette.Length)]; }
    private void ApplyTheme()
    {
        BackColor = AppTheme.Background; ForeColor = AppTheme.Text; AppTheme.Grid(_processes); AppTheme.Grid(_pages);
        _processes.Columns.Add("Name", "Proceso"); _processes.Columns.Add("State", "Estado");
        _pages.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _pages.RowHeadersVisible = false;
        foreach (var text in new[] { _log, _metrics }) { text.BackColor = AppTheme.Surface; text.ForeColor = AppTheme.Text; text.BorderStyle = BorderStyle.FixedSingle; }
    }
}

internal static class EnumerableSegments
{
    public static IEnumerable<List<T>> GroupConsecutiveBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var segment = new List<T>(); var comparer = EqualityComparer<TKey>.Default; var hasKey = false; TKey? key = default;
        foreach (var item in source) { var next = keySelector(item); if (hasKey && !comparer.Equals(key!, next)) { yield return segment; segment = []; } segment.Add(item); key = next; hasKey = true; }
        if (segment.Count > 0) yield return segment;
    }
}
