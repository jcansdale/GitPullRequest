using System;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace GitPullRequest.Services
{
    internal class AzureDevOpsRepository : GitRepository
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

        protected override string GetTipForReference(IRepository repo, Reference reference, CredentialsHandler credentialsHandler)
        {
            // Sadly for Azure DevOps there is an automatic merge commit that is at the tip of the ref, so we have to fetch it, in order to find the previous commit
            // as that is what the users branch will be up to
            if (reference.CanonicalName.StartsWith("refs/pull/", StringComparison.OrdinalIgnoreCase))
            {
                Commit commit;
                try
                {
                    commit = GetCommit();
                }
                catch
                {
                    // Try again, but this time lets fetch the specific commit and see if that helps
                    repo.Network.Fetch(this.RemoteName, new string[] { reference.CanonicalName }, new FetchOptions { CredentialsProvider = credentialsHandler });
                    commit = GetCommit();
                }
                return commit.Sha;
            }
            return reference.TargetIdentifier;

            Commit GetCommit()
            {
                return repo.Commits.QueryBy(new CommitFilter() { IncludeReachableFrom = reference.TargetIdentifier }).Skip(1).FirstOrDefault();
            }
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