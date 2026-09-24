using System.Reflection;
using System.Text;
using Spectre.Console;
using ZOVserver.DistFiles.FileServer.Services;

namespace ZOVserver.DistFiles.FileServer;

public static class Program
{
    private static void ShowLogo()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var table = new Table
        {
            ShowHeaders = false,
            Border = TableBorder.None,
            Expand = true
        };

        table.AddColumn(new TableColumn(""));

        var flagRow = new string('█', 64);

        for (var i = 0; i <= 4; i++)
            table.AddRow(new Markup($"[bold white]{flagRow}[/]"));

        for (var i = 0; i <= 4; i++)
            table.AddRow(new Markup($"[bold blue]{flagRow}[/]"));

        for (var i = 0; i <= 4; i++)
            table.AddRow(new Markup($"[bold red]{flagRow}[/]"));

        AnsiConsole.Write(table);
        Console.WriteLine("\n\n");
    }

    public static void Main(string[] args)
    {
        ShowLogo();

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = 256 * 1024 * 1024;
            options.MaxSendMessageSize = 256 * 1024 * 1024;
        });

        var app = builder.Build();

        app.MapGrpcService<FileServerGrpcService>();

        app.MapGet("/",
            () => "FileServerService: a service for storing, administering, and retrieving files");

        var filesPath = Path.Combine(Directory.GetCurrentDirectory(), "Files");
        if (!Directory.Exists(filesPath))
            Directory.CreateDirectory(filesPath);

        Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name + " started!");
        app.Run();
    }
}