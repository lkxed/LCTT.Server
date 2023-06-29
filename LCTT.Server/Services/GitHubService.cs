using Octokit;
using Octokit.Internal;

namespace LCTT.Server.Services;

public static class GitHubService
{
    static string baseOwner = "LCTT";
    static string owner = "GITHUB_ID";
    static string name = "TranslateProject";
    static InMemoryCredentialStore credentials = new InMemoryCredentialStore(new Credentials("GITHUB_PERSONAL_ACCESS_TOKEN"));
    static GitHubClient client = new GitHubClient(new ProductHeaderValue("LCTT.Server"), credentials);

    public static string CreateBranch(string branch)
    {
        var masterRefString =$"refs/heads/master";
        var masterRef = client.Git.Reference.Get(owner, name, masterRefString).GetAwaiter().GetResult();
        var masterSha = masterRef.Object.Sha;
        var branchRefString = $"refs/heads/{branch}";
        var newReference = new NewReference(branchRefString, masterSha);
        try
        {
            var branchRef = client.Git.Reference.Create(owner, name, newReference).GetAwaiter().GetResult();
            return branchRef.Url;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static IEnumerable<string> ListOpenPRs()
    {
        var prs = client.PullRequest.GetAllForRepository(baseOwner, name).GetAwaiter().GetResult();
        return prs
            .Where(pr => pr.State == ItemState.Open && pr.User.Login == owner)
            .Select(pr => pr.HtmlUrl);
    }

    public static string Refork()
    {
        DeleteRepository();
        return Fork();
    }

    public static void DeleteRepository()
    {
        client.Repository.Delete(owner, name).Wait();
    }

    public static string Fork()
    {
        var repo = client.Repository.Forks.Create(baseOwner, name, new NewRepositoryFork()).GetAwaiter().GetResult();
        return repo.CloneUrl;
    }
    
    public static string CreateFile(string branch, string category, string filename, string content)
    {
        var request = new CreateFileRequest($"[手动选题][{category}]: {filename}", content, branch);
        var path = $"sources/{category}/{filename}";
        try
        {
            var changeSet = client.Repository.Content.CreateFile(owner, name, path, request).GetAwaiter().GetResult();
            return changeSet.Content.HtmlUrl;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string CreatePR(string branch, string category, string filename)
    {
        var title = $"[手动选题][{category}]: {filename}";
        var head = $"{owner}:{branch}";
        var baseRef = "master";
        var request = new NewPullRequest(title, head, baseRef);
        request.MaintainerCanModify = true;
        request.Body = $"This article is collected by GITHUB_ID.";
        try
        {
            var pr = client.PullRequest.Create(baseOwner, name, request).GetAwaiter().GetResult();
            return pr.HtmlUrl;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string SearchForFirstMatch(string url)
    {
        var term = $"via: {url}";
        var request = new SearchCodeRequest(term, baseOwner, name);
        try
        {
            var task = client.Search.SearchCode(request);
            task.Wait();
            var result = task.GetAwaiter().GetResult();
            return result.Items.Any() ? result.Items.First().HtmlUrl : string.Empty;
        }
        catch (Exception ex)
        {
            // GitHub API secondary rate limit:
            // https://docs.github.com/en/rest/guides/best-practices-for-integrators?apiVersion=2022-11-28#dealing-with-secondary-rate-limits
            return ex.Message;
        }
    }
}