using System.Text;
using Spectre.Console;

namespace ZOVserver.Shared.TitanRemnants.Helper;

public static class LogoWriter
{
    public static void ShowLogo()
    {
        const string art = """
                           ███████╗░█████╗░██╗░░░██╗░░░░░░░██████╗███████╗██████╗░██╗░░░██╗███████╗██████╗░
                           ╚════██║██╔══██╗██║░░░██║░░░░░░██╔════╝██╔════╝██╔══██╗██║░░░██║██╔════╝██╔══██╗
                           ░░███╔═╝██║░░██║╚██╗░██╔╝█████╗╚█████╗░█████╗░░██████╔╝╚██╗░██╔╝█████╗░░██████╔╝
                           ██╔══╝░░██║░░██║░╚████╔╝░╚════╝░╚═══██╗██╔══╝░░██╔══██╗░╚████╔╝░██╔══╝░░██╔══██╗
                           ███████╗╚█████╔╝░░╚██╔╝░░░░░░░░██████╔╝███████╗██║░░██║░░╚██╔╝░░███████╗██║░░██║
                           ╚══════╝░╚════╝░░░░╚═╝░░░░░░░░░╚═════╝░╚══════╝╚═╝░░╚═╝░░░╚═╝░░░╚══════╝╚═╝░░╚═╝
                           """;

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

        Console.WriteLine("\n");
        AnsiConsole.Write(table);
        Console.WriteLine("\n");
        Console.WriteLine(art);
        Console.WriteLine("\n\n");
    }
}