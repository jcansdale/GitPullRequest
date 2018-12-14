using System.Linq;
using System.Collections.Generic;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public class GitPullRequestService
    {
        readonly IGitService gitService;

        public GitPullRequestService(IGitService gitService)
        {
            this.gitService = gitService;
        }

        public RemoteRepositoryCache GetRemoteRepositoryCache(IRepository repo)
        {
            return new RemoteRepositoryCache(gitService, repo);
        }

        public IList<(RemoteRepository Repository, int Number, bool IsDeleted)> FindPullRequests(
            RemoteRepositoryCache remoteRepositoryCache, Branch branch)
        {
            var isDeleted = false;
            string sha = null;
            if (branch.IsTracking || branch.IsRemote)
            {
                var gitRepository = remoteRepositoryCache[branch.RemoteName];
                var references = gitRepository.References;
                isDeleted = !references.TryGetValue(branch.UpstreamBranchCanonicalName, out sha);
            }

            if (sha == null)
            {
                sha = branch.Tip.Sha;
            }

            return FindPullRequestsForSha(remoteRepositoryCache, sha)
                .Select(pr => (pr.Repository, pr.Number, isDeleted)).ToList();
        }

        public IList<(RemoteRepository Repository, int Number)> FindPullRequestsForSha(
            RemoteRepositoryCache remoteRepositoryCache, string sha)
        {
            // Only consider one repository per URL and prioritize ones with a remote named "origin"
            var uniqueRepositories = remoteRepositoryCache.Values
                .GroupBy(r => r.Url)
                .Select(g => g
                    .OrderBy(r => r.RemoteName == "origin" ? 0 : 1)
                    .First());

            return uniqueRepositories
                .SelectMany(r => r.References, (x, y) => (Repository: x, Reference: y))
                .Where(kv => kv.Reference.Value == sha).Select(kv => (kv.Repository, Number: kv.Repository.FindPullRequestForCanonicalName(kv.Reference.Key)))
                .Where(pr => pr.Number != -1)
                .ToList();
        }

        public string FindCompareUrl(RemoteRepositoryCache remoteRepositoryCache, IRepository repo)
        {
            var branch = repo.Head;
            if (!branch.IsTracking)
            {
                return null;
            }

            var upstreamBranchCanonicalName = branch.UpstreamBranchCanonicalName;
            var gitRepository = remoteRepositoryCache[branch.RemoteName];
            if (!gitRepository.References.ContainsKey(upstreamBranchCanonicalName))
            {
                return null;
            }

            var friendlyName = GetFriendlyName(upstreamBranchCanonicalName);
            return gitRepository.GetCompareUrl(friendlyName);
        }

        static string GetFriendlyName(string canonicalName)
        {
            var prefix = "refs/heads/";
            if (!canonicalName.StartsWith(prefix))
            {
                return canonicalName;
            }

            return canonicalName.Substring(prefix.Length);
        }
    }
}
