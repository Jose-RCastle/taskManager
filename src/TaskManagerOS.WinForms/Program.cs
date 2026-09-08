using TaskManagerOS.WinForms.Forms;
using TaskManagerOS.WinForms.Services;

Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.Run(new MainForm(new AppState()));
