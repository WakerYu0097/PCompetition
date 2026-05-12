namespace Utility.Visual;

public static class ColorConsole
{
    public static void WriteColor(this string text, ConsoleColor color)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = originalColor;
    }
    public static void WriteLineColor(string text, ConsoleColor color)
    {
        WriteColor(text, color);
        Console.WriteLine();
    }
    public static void WriteColor(string text, ConsoleColor foreground, ConsoleColor background)
    {
        ConsoleColor originalForeground = Console.ForegroundColor;
        ConsoleColor originalBackground = Console.BackgroundColor;
        
        Console.ForegroundColor = foreground;
        Console.BackgroundColor = background;
        Console.Write(text);
        
        Console.ForegroundColor = originalForeground;
        Console.BackgroundColor = originalBackground;
    }

    public static void WriteLineColor(string text, ConsoleColor foreground, ConsoleColor background)
    {
        WriteColor(text, foreground, background);
        Console.WriteLine();
    }
}