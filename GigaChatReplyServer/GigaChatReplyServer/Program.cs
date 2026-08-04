using System.Reflection;

namespace GigaChatReplyServer
{

    public record ReviewRequest(string Text);

    public class Program
    {
        private static NotifyIcon? _trayIcon;
        private static WebApplication? _webApp;

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogAndShowException(e.ExceptionObject as Exception);
            };

            try
            {
                StartWebServer();
                StartTrayIcon();
            }
            catch (Exception ex)
            {
                LogAndShowException(ex);
            }

            Application.Run(); // Держим приложение живым, пока висит трей-иконка
        }

        private static void LogAndShowException(Exception? ex)
        {
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
                File.AppendAllText(logPath, $"{DateTime.Now}: {ex}\n\n");
            }
            catch { /* если даже запись в файл не удалась — игнорируем */ }

            MessageBox.Show(
                ex?.ToString() ?? "Неизвестная ошибка",
                "Ошибка запуска",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void StartWebServer()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddCors();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();
            app.Use(async (context, next) =>
            {
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.Headers.Append("Access-Control-Allow-Private-Network", "true");
                }
                await next();
            });
            app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            var configPath = Path.Combine(AppContext.BaseDirectory, "config.txt");
            var chatContextPath = Path.Combine(AppContext.BaseDirectory, "chatContext.txt");
            var chatContext = File.ReadAllText(chatContextPath);
            var authKey = File.ReadAllText(configPath).Trim();
            var client = new GigaChatClient(authKey);

            app.MapPost("/reply", async (ReviewRequest req) =>
            {
                var reply = await client.GenerateReplyAsync("GigaChat-3-Ultra", req.Text, chatContext);
                return Results.Ok(new { reply });
            });

            app.Urls.Add("http://localhost:5005");

            _webApp = app;
            _ = app.RunAsync(); // Не блокируем поток - сервер работает в фоне
        }

        private static void StartTrayIcon()
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add("открыть Swagger", null, (s, e) =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "http://localhost:5005/swagger",
                    UseShellExecute = true
                });
            });

            menu.Items.Add("Выход", null, async (s, e) =>
            {
                if (_trayIcon != null) _trayIcon.Visible = false;
                if (_webApp != null) await _webApp.StopAsync();
                Application.Exit();
            });

            _trayIcon = new NotifyIcon
            {
                Icon = LoadEmbeddedIcon(),
                Visible = true,
                Text = "GigaChat Reply Server",
                ContextMenuStrip = menu
            };
        }

        private static System.Drawing.Icon LoadEmbeddedIcon()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("app.ico");

            if (stream == null)
            {
                // Возвращаем иконку по умолчанию, если что-то пошло не так при загрузке ресурса
                return System.Drawing.SystemIcons.Application;
            }

            return new System.Drawing.Icon(stream);
        }
    }
}