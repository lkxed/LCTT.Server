using System.Text;
using HtmlAgilityPack;
using LCTT.Server.Models;

namespace LCTT.Server.Services.Parsers;

public class DebugpointnewsCom : LCTTParser
{
    protected override Author ParseAuthor(HtmlNode doc) => new Author
    {
        URL = "https://debugpointnews.com/author/dpicubegmail-com/",
        Name = "arindam"
    };
}