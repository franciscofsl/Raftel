namespace Raftel.Blazor;

public static class ColorPalette
{
    public static ApplicationColors Application => new();

    public class ApplicationColors
    {
        public string Main { get; set; }
        public string Hover { get; set; }
    }
}