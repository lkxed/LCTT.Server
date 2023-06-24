using HtmlAgilityPack;
using LCTT.Server.Models;

namespace LCTT.Server.Services.Parsers;

public class BlogCentosOrg : COSSIGParser
{
    protected override Author ParseAuthor(HtmlNode doc)
    {
        var author = rules[host].Author;
        var index = author.LastIndexOf(' ');
        return new Author
        {
            Name = author.Substring(0, index),
            URL = author.Substring(index + 1)
        };
    }
}