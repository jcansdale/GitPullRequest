using System;
using System.Linq;
using System.Collections.Generic;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public class GitPullRequestService
    {
        readonly RemoteRepositoryFactory remoteRepositoryFactory;

        public GitPullRequestService(RemoteRepositoryFactory remoteRepositoryFactory)
        {
            this.remoteRepositoryFactory = remoteRepositoryFactory;
        }

        public RemoteRepositoryCache GetRemoteRepositoryCache(IRepository repo, Action<Exception> exceptionLogger)
        {
            return new RemoteRepositoryCache(remoteRepositoryFactory, repo, exceptionLogger);
        }

        public IList<(RemoteRepository Repository, int Number, bool IsDeleted)> FindPullRequests(
            RemoteRepositoryCache remoteRepositoryCache, IList<RemoteRepository> upstreamRepositories, Branch branch)
        {
            var isDeleted = false;
            string sha = null;
            if (branch.IsTracking || branch.IsRemote)
            {
                var remoteRepository = remoteRepositoryCache.FindRemoteRepository(branch.RemoteName);
                if (remoteRepository == null)
                {
                    return Array.Empty<(RemoteRepository, int, bool)>();
                }

                var references = remoteRepository.References;
                isDeleted = !references.TryGetValue(branch.UpstreamBranchCanonicalName, out sha);
            }

            if (sha == null)
            {
                sha = branch.Tip.Sha;
            }

            return FindPullRequestsForSha(remoteRepositoryCache, upstreamRepositories, sha)
                .Select(pr => (pr.Repository, pr.Number, isDeleted)).ToList();
        }

        public IList<(RemoteRepository Repository, int Number)> FindPullRequestsForSha(
            RemoteRepositoryCache remoteRepositoryCache, ICollection<RemoteRepository> upstreamRepositories, string sha)
        {
            return upstreamRepositories
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
            var remoteRepository = remoteRepositoryCache.FindRemoteRepository(branch.RemoteName);
            if (remoteRepository == null)
            {
                return null;
            }

            if (!remoteRepository.References.ContainsKey(upstreamBranchCanonicalName))
            {
                return null;
            }

            var friendlyName = GetFriendlyName(upstreamBranchCanonicalName);
            return remoteRepository.GetCompareUrl(friendlyName);
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
