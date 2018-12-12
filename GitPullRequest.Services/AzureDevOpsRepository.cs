using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    internal class AzureDevOpsRepository : RemoteRepository
    {
        public AzureDevOpsRepository(IGitService gitService, IRepository repo, string remoteName)
            : base(gitService, repo, remoteName)
        {
        }

        public override int FindPullRequestForCanonicalName(string canonicalName)
        {
            var match = Regex.Match(canonicalName, "^refs/pull/([0-9]+)/merge$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
            {
                return number;
            }

            return -1;
        }

        protected override IDictionary<string, string> GetReferences(IRepository repo, string remoteName)
        {
            // for Azure DevOps we need to fetch PR branches so we can explore their history and get the commit before the automatic merge commit that is done on the server
            GitService.Fetch(repo, remoteName, $"+refs/pull/*/merge:refs/remotes/{remoteName}/pull/*/merge");

            return base.GetReferences(repo, remoteName);
        }

        protected override string GetTipForReference(IRepository repo, string canonicalName, string targetIdentifier)
        {
            if (FindPullRequestForCanonicalName(canonicalName) != -1)
            {
                // Get the commit at HEAD^1 as Azure DevOps automatically adds a merge commit on the server
                var commit = repo.Commits.QueryBy(new CommitFilter() { IncludeReachableFrom = targetIdentifier }).Skip(1).FirstOrDefault();
                return commit.Sha;
            }

            return targetIdentifier;
        }

        public override string GetPullRequestUrl(int number)
        {
            return $"{Url}/pullrequest/{number}";
        }

        public override string GetCompareUrl(string friendlyBranchName)
        {
            return $"{Url}/pullrequestcreate?sourceRef={Uri.EscapeUriString(friendlyBranchName)}";
        }
    }
}
