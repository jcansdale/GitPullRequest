using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    internal class GitHubRepository : RemoteRepository
    {
        public GitHubRepository(IGitService gitService, IRepository repo, string remoteName)
            : base(gitService, repo, remoteName)
        {
        }

        protected override string GetRepositoryUrl(IRepository repo, string remoteName)
        {
            var url = base.GetRepositoryUrl(repo, remoteName);
            var postfix = ".git";
            if (url.EndsWith(postfix))
            {
                url = url.Substring(0, url.Length - postfix.Length);
            }
            return url;
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