using HtmlAgilityPack;
using LCTT.Server.Models;
using LCTT.Server.Services.Parsers;
using System.Globalization;
using System.ServiceModel.Syndication;
using System.Web;
using System.Xml;

namespace LCTT.Server.Services;

public static class CollectorService
{
    public static (Parser, Article, string) Parse(string url)
    {
        var web = new HtmlWeb();
        var doc = web.Load(url).DocumentNode;
        var parser = Parser.GetParser(url);
        var rules = parser.LoadRules();
        var template = parser.LoadTemplate();
        var article = parser.Parse(doc);

        return (parser, article, template);
    }

    public static string Generate(Article article, string template)
    {
        int urlIndex = 0;
        var urlList = article.Urls.Select(url => $"[{++urlIndex}]: {url}");
        var content = HttpUtility.HtmlDecode
        (
            template.Replace("{Title}", article.Title)
                    .Replace("{URL}", article.URL)
                    .Replace("{Author.Name}", article.Author.Name)
                    .Replace("{Author.URL}", article.Author.URL)
                    .Replace("{Content}", string.Join("\n\n", article.Texts))
                    .Replace("{URL.List}", string.Join("\n", urlList))
        );
        return content;
    }

    public static Result Collect(string difficulty, string category, string url, string? content)
    {
        if (SQLiteService.FindURL(url))
            return Result.Error($"Article Exists: {url}");
        
        var (parser, article, template) = Parse(url);
        article.Difficulty = difficulty;
        article.Category = category;

        parser.LoadCounter();

        var branchUrl = GitHubService.CreateBranch(article.Branch);
        if (content is null)
            content = Generate(article, template);
        var fileUrl = GitHubService.CreateFile(article.Branch, category, article.Filename, content);
        var prUrl = GitHubService.CreatePR(article.Branch, category, article.Filename);

        parser.IncreaseCounter(article.FormattedDate);
        parser.PersistCounter();
        SQLiteService.AddURL(url);

        return Result.Success(prUrl);
    }

    public static Result Feed(string startDate, string? endDate, bool? groupBy)
    {
        endDate ??= DateTime.Today.ToString("yyyyMMdd");
        DateTime _startDate, _endDate;
        var startDateLegal = DateTime.TryParseExact(startDate, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None, out _startDate);
        if (startDateLegal is false)
            return Result.Error($"{startDate} is NOT a legal date.");
        var endDateLegal = DateTime.TryParseExact(endDate, "yyyyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None, out _endDate);
        if (endDateLegal is false)
            return Result.Error($"{endDate} is NOT a legal date.");
        if (_startDate > _endDate)
            return Result.Error($"{startDate} is later than {endDate}.");
        _endDate = _endDate.AddDays(1);
        
        var parser = new LCTTParser();
        var urls = parser.LoadRules().Select(rule => $"https://{rule.Key}/{rule.Value.Feed}");
        var feeds = new List<Feed>();
        string[] unwantedTags = {"a", "i", "em", "b", "strong", "code"};
        
        List<Task> tasks = new();
        foreach (var url in urls)
            tasks.Add(Task.Run(() => LoadFeeds(url)));
        Task.WaitAll(tasks.ToArray());
        feeds = feeds
            .Where(f => f.PubDate >= _startDate && f.PubDate <= _endDate)
            .Where(f => SQLiteService.FindURL(f.URL) is false)
            .OrderBy(f => f.PubDate)
            .ToList();

        if (groupBy ?? false)
        {
            var groupedFeeds = feeds
                .GroupBy(f => f.PubDate.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => g.ToList());
            return Result.Success(groupedFeeds);
        }
        return Result.Success(feeds);
        
        void LoadFeeds(string url)
        {
            try {
                using var reader = XmlReader.Create(url);
                var feed = SyndicationFeed.Load(reader);
                foreach (var item in feed.Items)
                {
                    feeds.Add(new Feed
                    {
                        Title = item.Title.Text,
                        URL = item.Links.First().Uri.AbsoluteUri,
                        Summary = CleanSummary(item.Summary.Text, unwantedTags),
                        PubDate = item.PublishDate.ToLocalTime().DateTime
                    });
                }
            }
            catch {}
        }

        string CleanSummary(string summary, string[] unwantedTags)
        {
            if (summary.Contains("</p>"))
            {
                var startIndex = summary.IndexOf("<p>") + "<p>".Length;
                var endIndex = summary.IndexOf("</p>");
                summary = summary.Substring(startIndex, endIndex - startIndex);
            }

            foreach (var tag in unwantedTags)
                summary = RemoveTag(tag);

            summary = HttpUtility.HtmlDecode(summary);
            return summary.ReplaceLineEndings("").Trim();

            string RemoveTag(string tag)
            {
                var openTag = "<" + tag;
                if (summary.Contains(openTag))
                {
                    while (summary.Contains(openTag))
                    {
                        var startIndex = summary.IndexOf(openTag);
                        var endIndex = summary.IndexOf('>', startIndex + openTag.Length);
                        summary = summary.Remove(startIndex, endIndex - startIndex + 1);
                    }
                    summary = summary.Replace($"</{tag}>", "");
                }
                return summary;
            }
        }
    }

    public static Result Clean()
    {
        var openPRs = GitHubService.ListOpenPRs();
        return openPRs.Any() 
            ? Result.Error(string.Join('\n', openPRs))
            : Result.Success(GitHubService.Refork());
    }
}