using System.Text.RegularExpressions;

namespace LCTT.Server.Models;

public class Article
{
    public string Title { get; set; } = string.Empty;
    public string PathSafeTitle => Regex.Replace(Title, @"[<>:""/\\\|\?\*#]", "");
    public string BranchSafeTitle => Regex.Replace(PathSafeTitle, @"\W", "-");
    public Author Author { get; set; }
    public string URL { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string FormattedDate => Date.ToString("yyyyMMdd");
    public string Difficulty { get; set; } = "easy";
    public string Twinkle(string difficulty) => difficulty switch
    {
        "easy" => "⭐️",
        "medium" => "⭐️⭐️",
        "hard" => "⭐️⭐️⭐️",
        _ => "⭐️"
    };
    public string Category { get; set; } = "tech";
    public static Dictionary<String, int> counter = new();
    int Number => counter.GetValueOrDefault(FormattedDate);
    public string Filename => $"{FormattedDate}.{Number} {Twinkle(Difficulty)} {PathSafeTitle}.md";
    public string Branch => $"{FormattedDate}-{Number}-{BranchSafeTitle}";
    public List<string> Texts { get; set; } = new List<string>();
    public List<string> Urls { get; set; } = new List<string>();

    public static void DeserializeCounter(string counterConf) => counter = counterConf.Split('\n').Select(x => x.Split('=')).ToDictionary(x => x[0], x => int.Parse(x[1]));

    public static string SerializeCounter() => string.Join('\n', counter.Select(x => $"{x.Key}={x.Value}"));

    public static void IncreaseCounter(string date) 
    {
        if (counter.ContainsKey(date) is false)
            counter.Add(date, default);
        counter[date]++;
    }
}