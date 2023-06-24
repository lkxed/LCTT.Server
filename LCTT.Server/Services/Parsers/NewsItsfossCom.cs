using System.Text;
using HtmlAgilityPack;

namespace LCTT.Server.Services.Parsers;

public class NewsItsfossCom : LCTTParser
{
    protected override void ParseDiv(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        if (node.GetAttributeValue("class", string.Empty).Contains("kg-card kg-callout-card"))
            ParseBlockQuote(node, builder, texts, urls, ordered);
        else if (node.GetAttributeValue("class", string.Empty) is "kg-callout-emoji")
        {
            var emoji = node.InnerText.Trim() + " ";
            builder!.Append(emoji);
        }
        else
            base.ParseDiv(node, builder, texts, urls, ordered);
    }

    protected override void ParseIframe(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    {
        var src = node.GetAttributeValue("src", string.Empty);
        if (src is not "" && src.StartsWith("https://www.youtube.com/embed/"))
        {
            var url = src.Replace("https://www.youtube.com/embed/", "").Split("?").FirstOrDefault(src);
            if (src.IndexOf(url) is not 0)
                url = "https://youtu.be/" + url;
            var title = node.GetAttributeValue("title", string.Empty).Trim();
            if (title is "")
                title = "YouTube Video";
            var iframeText = $"![{title}][{++urlIndex}]";
            texts.Add(iframeText);
            urls.Add(url);
        }
    }
}