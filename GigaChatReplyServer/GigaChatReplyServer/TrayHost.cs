using System.Diagnostics;
using System.Reflection;

namespace GigaChatReplyServer
{
    public class TrayHost
    {
        private NotifyIcon? _trayIcon;
        private readonly WebApplication _webApp;

        public TrayHost(WebApplication webApp) => _webApp = webApp;

        public void Start()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Открыть Swagger", null, (_, _) =>
                Process.Start(new ProcessStartInfo { FileName = "http://localhost:5005/swagger", UseShellExecute = true }));
            menu.Items.Add("Выход", null, async (_, _) =>
            {
                if (_trayIcon != null) _trayIcon.Visible = false;
                await _webApp.StopAsync();
                System.Windows.Forms.Application.Exit();
            });

            _trayIcon = new NotifyIcon
            {
                Icon = LoadEmbeddedIcon(),
                Visible = true,
                Text = "GigaChat Reply Server",
                ContextMenuStrip = menu
            };
        }

        private static Icon LoadEmbeddedIcon()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("app.ico");
            return stream is null ? SystemIcons.Application : new Icon(stream);
        }
    }
}
