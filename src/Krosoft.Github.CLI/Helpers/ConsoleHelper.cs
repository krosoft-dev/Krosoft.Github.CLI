namespace Krosoft.Github.CLI.Helpers;

internal static class ConsoleHelper
{
    internal const int Width = 140;

    internal static void DisplayHeader(string title)
    {
        var paddingWidth = Math.Max(0, (Width - title.Length) / 2);
        var padding = new string(' ', paddingWidth);
        var border = new string('═', Width);

        WriteColoredLine(ConsoleColor.Green, $"╔{border}╗");
        WriteColoredLine(ConsoleColor.Green, title.Length % 2 != Width % 2
                             ? $"║{padding}{title}{padding} ║"
                             : $"║{padding}{title}{padding}║");
        WriteColoredLine(ConsoleColor.Green, $"╚{border}╝");
        Console.WriteLine();
    }

    internal static void WriteColoredLine(ConsoleColor color, string message)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = previous;
    }

    internal static int HandleError(string message)
    {
        WriteColoredLine(ConsoleColor.Red, message);
        return -1;
    }

    internal static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength
            ? value
            : string.Concat(value.AsSpan(0, maxLength - 1), "…");
}
