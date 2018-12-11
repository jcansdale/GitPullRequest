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
            // because we save this in our our own refs/pulls namespace we might not need to fetch if nothing has changed on the server

            // Get remote references the normal way
            var remoteRefs = base.GetReferences(repo, remoteName);

            var needsFetch = false;
            foreach (var kvp in remoteRefs)
            {
                var pr = FindPullRequestForCanonicalName(kvp.Key);
                if (pr != -1)
                {
                    var localRef = repo.Refs[GetPullRequestRefName(remoteName, pr.ToString())];
                    // If we don't have a local ref for this PR branch, or we do but its not up to date with the server, we need to fetch
                    if (localRef == null || !localRef.TargetIdentifier.Equals(kvp.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        needsFetch = true;
                        break;
                    }
                }
            }
            if (needsFetch)
            {
                GitService.Fetch(repo, remoteName, new[] { $"+refs/pull/*/merge:{GetPullRequestRefName(remoteName, "*")}" }, false);
            }

            // Now that we know the data is available we can go through and reset our PR refs to point to one commit before the merge commit
            var pullRequestRefs = remoteRefs.Where(k => k.Key.StartsWith("refs/pull/", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var kvp in pullRequestRefs)
            {
                // Get the commit at HEAD^1 as Azure DevOps automatically adds a merge commit on the server
                var commit = repo.Commits.QueryBy(new CommitFilter() { IncludeReachableFrom = kvp.Value }).Skip(1).FirstOrDefault();
                remoteRefs[kvp.Key] = commit.Sha;
            }

            return remoteRefs;
        }

        private static string GetPullRequestRefName(string remoteName, string pr)
        {
            return $"refs/pulls/{remoteName}/pull/{pr}/merge";
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
