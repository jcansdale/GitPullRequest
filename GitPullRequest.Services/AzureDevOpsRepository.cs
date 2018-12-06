using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace GitPullRequest.Services
{
    internal class AzureDevOpsRepository : RemoteRepository
    {
        public AzureDevOpsRepository(IRepository repo, string remoteName)
            : base(repo, remoteName)
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
            repo.Network.Fetch(remoteName, new string[] { "+refs/pull/*/merge:refs/remotes/origin/pull/*/merge" }, new FetchOptions { CredentialsProvider = CreateCredentialsHandler() });
            return base.GetReferences(repo, remoteName);
        }

        protected override string GetTipForReference(IRepository repo, Reference reference)
        {
            if (reference.CanonicalName.StartsWith("refs/pull/", StringComparison.OrdinalIgnoreCase))
            {
                // Get the commit at HEAD^1 as Azure DevOps automatically adds a merge commit on the server
                var commit = repo.Commits.QueryBy(new CommitFilter() { IncludeReachableFrom = reference.TargetIdentifier }).Skip(1).FirstOrDefault();
                return commit.Sha;
            }
            return reference.TargetIdentifier;
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