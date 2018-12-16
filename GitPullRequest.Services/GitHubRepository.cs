using System.Text.RegularExpressions;
using GitHub.Primitives;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public class GitHubRepository : RemoteRepository
    {
        public GitHubRepository(IGitService gitService, IRepository repo, string remoteName)
            : base(gitService, repo, remoteName)
        {
        }

        protected override string GetRepositoryUrl(IRepository repo, string remoteName)
        {
            var url = base.GetRepositoryUrl(repo, remoteName);
            var uriString = new UriString(url);
            return uriString.ToRepositoryUrl()?.ToString() ?? "";
        }

        public override int FindPullRequestForCanonicalName(string canonicalName)
        {
            var match = Regex.Match(canonicalName, "^refs/pull/([0-9]+)/head$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
            {
                return number;
            }

            return -1;
        }

        public override string GetPullRequestUrl(int number)
        {
            return $"{Url}/pull/{number}";
        }

        public override string GetCompareUrl(string friendlyBranchName)
        {
            return $"{Url}/compare/{friendlyBranchName}";
        }
    }
}