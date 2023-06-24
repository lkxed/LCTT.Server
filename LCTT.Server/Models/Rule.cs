namespace LCTT.Server.Models;

public class Rule
{
    public string Feed { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Cover { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Exclusions { get; set; } = new();
}