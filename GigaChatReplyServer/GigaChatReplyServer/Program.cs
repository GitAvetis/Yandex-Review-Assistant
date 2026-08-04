using GigaChatReplyServer;
using GigaChatReplyServer.Application;
using GigaChatReplyServer.Endpoints;
using GigaChatReplyServer.Infrastructure;
using GigaChatReplyServer.Middlewares;
using GigaChatReplyServer.Options;

public class Program
{
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogAndShowException(e.ExceptionObject as Exception);

        try
        {
            var app = BuildWebApp();
            _ = app.RunAsync();
            new TrayHost(app).Start();
        }
        catch (Exception ex)
        {
            LogAndShowException(ex);
        }

        Application.Run();
    }

    private static WebApplication BuildWebApp()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddCors();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.Configure<GigaChatOptions>(builder.Configuration.GetSection(GigaChatOptions.SectionName));

        builder.Services.AddHttpClient<IGigaChatClient, GigaChatClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // см. пояснение про доверие к сертификату НУЦ Минцифры ниже
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            });

        builder.Services.AddScoped<IReviewReplyService, ReviewReplyService>();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.Use(async (context, next) =>
        {
            if (context.Request.Method == "OPTIONS")
                context.Response.Headers.Append("Access-Control-Allow-Private-Network", "true");
            await next();
        });
        app.UseCors(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.MapReviewEndpoints();
        app.Urls.Add("http://localhost:5005");

        return app;
    }

    private static void LogAndShowException(Exception? ex)
    {
        try
        {
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "crash.log"), $"{DateTime.Now}: {ex}\n\n");
        }
        catch { }

        MessageBox.Show(ex?.ToString() ?? "Неизвестная ошибка", "Ошибка запуска",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}