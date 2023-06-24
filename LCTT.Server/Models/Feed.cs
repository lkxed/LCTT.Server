public class Feed
{
    public string Title { get; set; } = string.Empty;
    public string URL { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime PubDate { get; set; } = DateTime.Now;
}