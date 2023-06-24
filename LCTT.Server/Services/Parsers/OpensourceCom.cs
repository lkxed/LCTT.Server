using System.Text;
using HtmlAgilityPack;

namespace LCTT.Server.Services.Parsers;

public class OpensourceCom : LCTTParser
{
    // protected override void ParseCode(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    // {
    //     if (node.ChildNodes.Any(div => div.HasClass("geshifilter")))
    //     {
    //         var codeBuilder = new StringBuilder();
    //         TraverseChildren(node, codeBuilder, texts, urls, ordered);
    //         var codeText = codeBuilder.ToString().Trim();
    //         if (codeText is not null)
    //             if (builder is not null)
    //                 builder.Append($"```\n{codeText}\n```");
    //             else
    //                 texts.Add($"```\n{codeText}\n```");
    //     }
    //     else base.ParseCode(node, builder, texts, urls, ordered);
    // }

    // protected override void ParseBr(HtmlNode node, StringBuilder? builder, List<string> texts, List<string> urls, bool ordered)
    // {
    //     if (builder is not null && node.Ancestors("div").Any(div => div.HasClass("geshifilter")))
    //         builder.Append("\n");
    // }
}