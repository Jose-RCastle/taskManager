namespace TaskManagerOS.WinForms.Theme;

public static class AppTheme
{
    public static readonly Color Background = Color.FromArgb(10, 18, 32);
    public static readonly Color Surface = Color.FromArgb(20, 31, 50);
    public static readonly Color Surface2 = Color.FromArgb(29, 43, 67);
    public static readonly Color Accent = Color.FromArgb(39, 139, 255);
    public static readonly Color Success = Color.FromArgb(48, 196, 141);
    public static readonly Color Running = Color.FromArgb(245, 190, 55);
    public static readonly Color Blocked = Color.FromArgb(244, 126, 68);
    public static readonly Color Error = Color.FromArgb(235, 80, 91);
    public static readonly Color Purple = Color.FromArgb(139, 117, 255);
    public static readonly Color Muted = Color.FromArgb(145, 158, 180);
    public static readonly Color Text = Color.FromArgb(237, 243, 252);

    public static Button Button(string text, Color? color = null) => new()
    {
        Text = text, AutoSize = true, MinimumSize = new(110, 38), Padding = new(12, 5, 12, 5),
        FlatStyle = FlatStyle.Flat, BackColor = color ?? Accent, ForeColor = Color.White,
        Font = new("Segoe UI Semibold", 9.5f), Margin = new(4)
    };
    public static Label Heading(string text, float size = 18) => new() { Text = text, AutoSize = true, ForeColor = Text, Font = new("Segoe UI Semibold", size), Margin = new(4, 4, 4, 12) };
    public static void Grid(DataGridView grid)
    {
        grid.BackgroundColor = Surface; grid.BorderStyle = BorderStyle.None; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false; grid.ReadOnly = true; grid.AllowUserToAddRows = false;
        grid.EnableHeadersVisualStyles = false; grid.ColumnHeadersDefaultCellStyle.BackColor = Surface2; grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.BackColor = Surface; grid.DefaultCellStyle.ForeColor = Text; grid.DefaultCellStyle.SelectionBackColor = Accent;
    }
}
