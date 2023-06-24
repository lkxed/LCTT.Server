using System.Text;
using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using LCTT.Server.Models;

namespace LCTT.Server.Services.Parsers;

public abstract class Parser
{
    protected string url = string.Empty;
    protected string host = string.Empty;
    protected string baseUrl = string.Empty;
    protected int urlIndex;
    protected int itemIndex;
    protected string template = string.Empty;
    static Dictionary<string, Parser> parsers = new();
    protected static Dictionary<string, Rule> rules = new();
    protected static HashSet<string> loadedRulesPaths = new();
    protected virtual string RulesPath => "Configs/Rules";
    protected virtual string TemplatePath => "Configs/Templates";
    protected virtual string CounterPath => "Configs/Counters";
    protected virtual string TopLevelHeading => string.Empty;

    public void Initialize(string url)
    {
        var uri = new Uri(url);
        this.url = url;
        host = uri.Host;
        baseUrl = uri.GetLeftPart(System.UriPartial.Authority);
        urlIndex = itemIndex = 0;
    }

    public Dictionary<string, Rule> LoadRules()
    {
        if (rules.Any() && loadedRulesPaths.Contains(RulesPath))
            return rules;
        
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var yml = File.ReadAllText(RulesPath);
        var newRules = deserializer.Deserialize<Dictionary<string, Rule>>(yml);
        rules = rules.Union(newRules).ToDictionary(p => p.Key, p => p.Value);
        loadedRulesPaths.Add(RulesPath);
        return rules;
    }

    public string LoadTemplate()
    {
        if (template is "")
            template = File.ReadAllText(TemplatePath);
        return template;
    }

    static Type DecideParserType(string url)
    {
        var host = new Uri(url).Host;
        var parts = host.Split('.').Select
        (
            s => s.Length is 1 ?
            char.ToUpper(s[0]).ToString() :
            char.ToUpper(s[0]) + s.Substring(1)
        );
        var typeName = string.Join("", parts);
        var qualifiedTypeName = $"{typeof(Parser).Namespace}.{typeName}";
        var type = Type.GetType(qualifiedTypeName);

        return type ?? typeof(LCTTParser);
    }

    public static Parser GetParser(string url)
    {
        var host = new Uri(url).Host;
        if (parsers.ContainsKey(host) is false)
        {
            var type = DecideParserType(url);
            var constructor = type.GetConstructor(new Type[]{});
            var _parser = constructor is null
                ? new LCTTParser()
                : (Parser)constructor.Invoke(null);
            
            parsers.Add(host, _parser);
        }

        var parser = (Parser)parsers[host];
        parser.Initialize(url);
        return parser;
    }

    public virtual Article Parse(HtmlNode doc)
    {
        ExcludeNodes(doc);

        var (texts, urls) = (new List<string>(), new List<string>());

        // Must be in this exact order: ParseSummary => ParseCover => ParseContent, 
        // because technically Summary would be the first part of Content,
        // and Cover would be the second part of Content.
        ParseSummary(texts, urls, doc);
        ParseCover(texts, urls, doc);
        ParseContent(texts, urls, doc);

        return new Article
        {
            Title = ParseTitle(doc),
            Author = ParseAuthor(doc),
            URL = url,
            Date = ParseDate(doc),
            Texts = texts,
            Urls = urls
        };
    }

    protected virtual string ParseTitle(HtmlNode doc)
    {
        var node = doc.SelectSingleNode(rules[host].Title);
        var title = node.InnerText.Trim();
        title = HttpUtility.HtmlDecode(title);
        return title;
    }

    protected virtual void ParseSummary(List<string> texts, List<string> urls, HtmlNode doc)
    {
        var summaryRule = rules[host].Summary;
        if (!string.IsNullOrEmpty(summaryRule))
        {
            var node = doc.SelectSingleNode(summaryRule);
            if (node is not null)
                Parse(node, null, texts, urls, false);
        }
    }

    protected virtual void ParseCover(List<string> texts, List<string> urls, HtmlNode doc)
    {
        var coverRule = rules[host].Cover;
        if (!string.IsNullOrEmpty(coverRule))
        {
            var node = doc.SelectSingleNode(coverRule);
            if (node is not null)
                ParseImg(node, null, texts, urls, false);
        }
    }

    protected virtual Author ParseAuthor(HtmlNode doc)
    {
        var authorRule = rules[host].Author;
        if (!string.IsNullOrEmpty(authorRule))
        {
            var node = doc.SelectSingleNode(authorRule);
            if (node is not null)
            {
                var url = node.GetAttributeValue("href", string.Empty);
                url = url.StartsWith("http") ? url : baseUrl + url;
                var name = node.InnerText.Trim();
                return new Author
                {
                    URL = url,
                    Name = name
                };
            }
        }
        return new Author();
    }

    protected virtual DateTime ParseDate(HtmlNode doc)
    {
        var dateRule = rules[host].Date;
        if (!string.IsNullOrEmpty(dateRule))
        {
            var node = doc.SelectSingleNode(dateRule);
            if (node is not null)
            {
                var date = node.Name == "time" ?
                    node.GetAttributeValue("datetime", string.Empty) :
                    node.InnerText.Trim();
                return DateTimeOffset.Parse(date).ToLocalTime().DateTime;
            }
        }
        return DateTime.Today;
    }

    protected virtual void ParseContent(List<string> texts, List<string> urls, HtmlNode doc)
    {
        var contentRule = rules[host].Content;
        if (!string.IsNullOrEmpty(contentRule))
        {
            var node = doc.SelectSingleNode(contentRule);
            if (node is not null)
                Parse(node, null, texts, urls, false);
        }
    }

    protected virtual void ExcludeNodes(HtmlNode doc)
    {
        var exclusionRules = rules[host].Exclusions;
        foreach (var exlusion in exclusionRules)
        {
            var exclusionNodes = doc.SelectNodes(exlusion);
            if (exclusionNodes is not null)
                foreach (var exclusionNode in exclusionNodes)
                    exclusionNode.Remove();
        }
    }

    #region Parse Tags

    protected virtual void Parse(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        if (node.NodeType == HtmlNodeType.Text)
        {
            ParseText(node, builder, texts, urls, ordered);
            return;
        }
        switch (node.Name)
        {
            case "h1":
                ParseH1(node, builder, texts, urls, ordered);
                break;

            case "h2":
                ParseH2(node, builder, texts, urls, ordered);
                break;

            case "h3":
                ParseH3(node, builder, texts, urls, ordered);
                break;

            case "h4":
                ParseH4(node, builder, texts, urls, ordered);
                break;

            case "h5":
                ParseH5(node, builder, texts, urls, ordered);
                break;

            case "h6":
                ParseH6(node, builder, texts, urls, ordered);
                break;

            case "img":
            case "amp-img":
                ParseImg(node, builder, texts, urls, ordered);
                break;
            
            case "video":
                ParseVideo(node, builder, texts, urls, ordered);
                break;

            case "figure":
                ParseFigure(node, builder, texts, urls, ordered);
                break;

            case "a":
                ParseAnchor(node, builder, texts, urls, ordered);
                break;

            case "pre":
            case "code":
                ParseCode(node, builder, texts, urls, ordered);
                break;

            case "blockquote":
                ParseBlockQuote(node, builder, texts, urls, ordered);
                break;

            case "span":
                ParseSpan(node, builder, texts, urls, ordered);
                break;

            case "p":
                ParseParagragh(node, builder, texts, urls, ordered);
                break;

            case "b":
            case "strong":
                ParseBold(node, builder, texts, urls, ordered);
                break;

            case "i":
            case "em":
                ParseItalic(node, builder, texts, urls, ordered);
                break;

            case "ul":
                ParseUnordered(node, builder, texts, urls, ordered);
                break;

            case "ol":
                itemIndex = 0;
                ParseOrdered(node, builder, texts, urls, ordered);
                break;

            case "li":
                ParseListItem(node, builder, texts, urls, ordered);
                break;

            case "div":
                ParseDiv(node, builder, texts, urls, ordered);
                break;

            case "table":
                ParseTable(node, builder, texts, urls, ordered);
                break;
            
            case "tr":
                ParseTr(node, builder, texts, urls, ordered);
                break;

            case "td":
                ParseTd(node, builder, texts, urls, ordered);
                break;

            case "iframe":
                ParseIframe(node, builder, texts, urls, ordered);
                break;

            case "br":
                ParseBr(node, builder, texts, urls, ordered);
                break;

            case "script":
            case "style":
            case "noscript":
            case "figcaption":
            case "interaction":
                break;

            default:
                TraverseChildren(node, builder, texts, urls, ordered);
                break;
        }
    }

    protected virtual void ParseH1(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var h1Text = node.InnerText.Trim();
        if (h1Text is not "")
            texts.Add($"{TopLevelHeading} {h1Text}");
    }

    protected virtual void ParseH2(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var h2Text = node.InnerText.Trim();
        if (h2Text is not "")
            texts.Add($"{TopLevelHeading}# {h2Text}");
    }

    protected virtual void ParseH3(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var h3Text = node.InnerText.Trim();
        if (h3Text is not "")
            texts.Add($"{TopLevelHeading}## {h3Text}");
    }

    protected virtual void ParseH4(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var h4Text = node.InnerText.Trim();
        if (h4Text is not "")
            texts.Add($"{TopLevelHeading}### {h4Text}");
    }

    protected virtual void ParseH5(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var h5Text = node.InnerText.Trim();
        if (h5Text is not "")
            texts.Add($"{TopLevelHeading}#### {h5Text}");
    }

    protected virtual void ParseH6(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var h6Text = node.InnerText.Trim();
        if (h6Text is not "")
            texts.Add($"{TopLevelHeading}##### {h6Text}");
    }

    protected virtual void ParseImg(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var imgUrl = node.GetAttributeValue("data-lazy-srcset", string.Empty).Trim();
        if (imgUrl is "")
            imgUrl = node.GetAttributeValue("data-srcset", string.Empty).Trim();
        if (imgUrl is "")
            imgUrl = node.GetAttributeValue("srcset", string.Empty).Trim();
        if (imgUrl is not "")
        {
            imgUrl = imgUrl.Split(',')
                .Select(x => x.Trim())
                .Where(x => x.Any())
                .Select(x => x.Split(' '))
                .Where(x => x.Length is 2)
                .OrderByDescending(items => int.Parse(items[1].Replace("w", "")))
                .Select(items => items[0])
                .FirstOrDefault(string.Empty);
        }
        if (imgUrl is "")
            imgUrl = node.GetAttributeValue("data-orig-file", string.Empty).Trim();
        if (imgUrl is "")
            imgUrl = node.GetAttributeValue("data-src", string.Empty).Trim();
        if (imgUrl is "")
            imgUrl = node.GetAttributeValue("src", string.Empty).Trim();

        if (imgUrl is not "" && imgUrl.StartsWith("data:image") is false)
        {
            imgUrl = imgUrl.StartsWith("http") ? imgUrl : baseUrl + imgUrl;
            urls.Add(imgUrl);

            var imgTitle = node.GetAttributeValue("title", string.Empty);
            if (imgTitle is "")
                imgTitle = node.GetAttributeValue("alt", string.Empty).Trim();

            var imgText = $"![{imgTitle}][{++urlIndex}]";

            if (builder is not null)
                builder.Append(imgText);
            else
                texts.Add(imgText);
        }
    }

    protected virtual void ParseVideo(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
    }

    protected virtual void ParseFigure(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        TraverseChildren(node, builder, texts, urls, ordered);
    }

    protected virtual void ParseText(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var text = node.InnerText;
        if (text.Trim() is not "")
            if (builder is not null)
                builder.Append(text);
            else
                texts.Add(text);
    }

    protected virtual void ParseAnchor(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var aUrl = node.GetAttributeValue("href", string.Empty).Trim();
        aUrl = aUrl.StartsWith("http") ? aUrl : baseUrl + aUrl;

        var aBuilder = new StringBuilder();
        TraverseChildren(node, aBuilder, texts, urls, ordered);
        var aTitle = aBuilder.ToString().Trim();
        var anchorPointsToPicture = Regex.IsMatch(aTitle, @"!\[.*\]\[\d+\]");
        if (aTitle is not "")
        {
            var aText = string.Empty;

            if (anchorPointsToPicture)
                aText = aTitle;
            else
            {
                var repeated = urls.Any(url => url == aUrl);
                if (repeated)
                {
                    var index = urls.IndexOf(aUrl);
                    aText = $"[{aTitle}][{index + 1}]";
                }
                else
                {
                    RemoveQueryParameter(ref aUrl, "ref");
                    urls.Add(aUrl);
                    aText = $"[{aTitle}][{++urlIndex}]";
                }
            }

            if (builder is not null)
                builder.Append(aText);
            else
                texts.Add(aText);
        }

        void RemoveQueryParameter(ref string url, string? parameterName)
        {
            UriBuilder uriBuilder = new UriBuilder(url);
            string query = uriBuilder.Query;
            if (!string.IsNullOrEmpty(query))
            {
                var queryParams = System.Web.HttpUtility.ParseQueryString(query);
                if (parameterName is null)
                    queryParams.Clear();
                else
                    queryParams.Remove(parameterName);
                uriBuilder.Query = queryParams.ToString();
                url = uriBuilder.ToString();
            }
        }
    }

    protected virtual void ParseCode(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var codeText = node.InnerText.Trim();
        if (codeText is not "")
            if (builder is not null)
                builder.Append($"`{codeText}`");
            else
                texts.Add($"```\n{codeText}\n```");
    }

    protected virtual void ParseBlockQuote(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var quoteBuilder = new StringBuilder();
        TraverseChildren(node, quoteBuilder, texts, urls, ordered);
        var quoteText = quoteBuilder.ToString();
        quoteText = quoteText.Replace("\n", "\n> ");
        if (quoteText.EndsWith("\n> "))
            quoteText = quoteText.Substring(0, quoteText.Length - "\n> ".Length);
        quoteText = $"> {quoteText}";
        if (quoteText.Trim() is not "")
            if (builder is not null)
                builder.Append(quoteText);
            else
                texts.Add(quoteText);
    }

    protected virtual void ParseSpan(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var spanText = node.InnerText.Trim();
        if (spanText is not "")
            if (builder is not null)
                builder.Append(spanText);
            else
                texts.Add(spanText);
    }

    protected virtual void ParseParagragh(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var pBuilder = new StringBuilder();
        TraverseChildren(node, pBuilder, texts, urls, ordered);
        var pText = pBuilder.ToString();
        if (pText.Trim() is not "")
        {
            if (builder is not null)
                builder.Append(pText);
            else
                texts.Add(pText.Trim());
        }
    }

    protected virtual void ParseBold(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var bBuilder = new StringBuilder();
        TraverseChildren(node, bBuilder, texts, urls, ordered);
        var _bText = bBuilder.ToString();
        if (_bText.Trim() is not "")
        {
            var bText = $"**{_bText.Trim()}**";
            if (_bText.StartsWith(' '))
                bText = " " + bText;
            if (_bText.EndsWith(' '))
                bText = bText + " ";

            if (builder is not null)
                builder.Append(bText);
            else
                texts.Add(bText);
        }
    }

    protected virtual void ParseItalic(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var iBuilder = new StringBuilder();
        TraverseChildren(node, iBuilder, texts, urls, ordered);
        var _iText = iBuilder.ToString();
        if (_iText.Trim() is not "")
        {
            var iText = $"_{_iText.Trim()}_";
            if (_iText.StartsWith(' '))
                iText = " " + iText;
            if (_iText.StartsWith(' '))
                iText = iText + " ";

            if (builder is not null)
                builder.Append(iText);
            else
                texts.Add(iText);
        }
    }

    protected virtual void ParseUnordered(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        if (node.HasChildNodes)
        {
            var ulBuilder = new StringBuilder();
            TraverseChildren(node, ulBuilder, texts, urls, ordered);
            var ulText = ulBuilder.ToString().Trim();
            if (ulText is not "")
                texts.Add(ulText);
        }
    }

    protected virtual void ParseOrdered(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        if (node.HasChildNodes)
        {
            var olBuilder = new StringBuilder();
            TraverseChildren(node, olBuilder, texts, urls, ordered);
            var olText = olBuilder.ToString().Trim();
            if (olText is not "")
                texts.Add(olText);
        }
    }

    protected virtual void ParseListItem(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        if (node.HasChildNodes)
        {
            var liBuilder = new StringBuilder();
            TraverseChildren(node, liBuilder, texts, urls, ordered);
            var liText = liBuilder.ToString().Trim();
            if (liText is not "" && builder is not null)
                builder.Append(ordered ? $"{++itemIndex}. {liText}\n" : $"- {liText}\n");
        }
    }

    protected virtual void ParseDiv(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        TraverseChildren(node, builder, texts, urls, ordered);
    }

    protected virtual void ParseTable(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var tableBuilder = new StringBuilder();
        TraverseChildren(node, tableBuilder, texts, urls, ordered);
        var tableText = tableBuilder.ToString().Trim();
        if (tableText is not "")
            texts.Add(tableText);
    }

    protected virtual void ParseTr(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var trBuilder = new StringBuilder();
        TraverseChildren(node, trBuilder, texts, urls, ordered);
        var trText = trBuilder.ToString().Trim();
        if (trText is not "")
        {
            trText = $"{trText} |\n";
            if (builder is not null)
                builder.Append(trText);
            else
                texts.Add(trText);
        }
    }

    protected virtual void ParseTd(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var tdBuilder = new StringBuilder();
        TraverseChildren(node, tdBuilder, texts, urls, ordered);
        var tdText = tdBuilder.ToString().Trim();
        if (tdText is not "")
        {
            tdText = $"| {tdText} ";
            if (builder is not null)
                builder.Append(tdText);
            else
                texts.Add(tdText);
        }
    }

    protected virtual void ParseIframe(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var src = node.GetAttributeValue("src", string.Empty).Trim();
        if (src.Contains("youtube.com"))
        {
            urls.Add(src);
            texts.Add($"![YouTube Video][{urlIndex++}]");
        }
    }

    protected virtual void ParseBr(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
    }

    protected virtual void TraverseChildren(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        if (node.HasChildNodes)
            foreach (var child in node.ChildNodes)
                Parse(child, builder, texts, urls, ordered);
    }

    #endregion

    public void LoadCounter()
    {
        var counterConf = File.ReadAllText(CounterPath);
        Article.DeserializeCounter(counterConf);
    }

    public void PersistCounter()
    {
        var counterConf = Article.SerializeCounter();
        File.WriteAllText(CounterPath, counterConf);
    }

    public void IncreaseCounter(string date) => Article.IncreaseCounter(date);
}