namespace LCTT.Server.Models;

public struct DCUCRequest
{
    public string Difficulty { get; set; }
    public string Category { get; set; }
    public string URL { get; set; }
    public string? Content { get; set; }
}