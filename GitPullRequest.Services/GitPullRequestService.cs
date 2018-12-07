using System.Linq;
using System.Collections.Generic;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public class GitPullRequestService
    {
        readonly GitService gitService;

        public GitPullRequestService(GitService gitService)
        {
            this.gitService = gitService;
        }

        public IDictionary<string, RemoteRepository> GetGitRepositories(IRepository repo)
        {
            var gitRepositories = new Dictionary<string, RemoteRepository>();
            foreach (var remote in repo.Network.Remotes)
            {
                var remoteName = remote.Name;
                gitRepositories[remoteName] = GitRepositoryFactory.Create(gitService, repo, remoteName);
            }

            return gitRepositories;
        }

        public IList<(RemoteRepository Repository, int Number, bool IsDeleted)> FindPullRequests(
            IDictionary<string, RemoteRepository> gitRepositories, Branch branch)
        {
            var isDeleted = false;
            string sha = null;
            if (branch.IsTracking)
            {
                var gitRepository = gitRepositories[branch.RemoteName];
                var references = gitRepository.References;
                isDeleted = !references.TryGetValue(branch.UpstreamBranchCanonicalName, out sha);
            }

            if (sha == null)
            {
                sha = branch.Tip.Sha;
            }

            return FindPullRequestsForSha(gitRepositories, sha)
                .Select(pr => (pr.Repository, pr.Number, isDeleted)).ToList();
        }

        public IList<(RemoteRepository Repository, int Number)> FindPullRequestsForSha(
            IDictionary<string, RemoteRepository> gitRepositories, string sha)
        {
            return gitRepositories
                .SelectMany(r => r.Value.References, (x, y) => (Repository: x.Value, Reference: y))
                .Where(kv => kv.Reference.Value == sha).Select(kv => (kv.Repository, Number: kv.Repository.FindPullRequestForCanonicalName(kv.Reference.Key)))
                .Where(pr => pr.Number != -1)
                .ToList();
        }

        public string FindCompareUrl(IDictionary<string, RemoteRepository> gitRepositories, IRepository repo)
        {
            var branch = repo.Head;
            if (!branch.IsTracking)
            {
                return null;
            }

            var upstreamBranchCanonicalName = branch.UpstreamBranchCanonicalName;
            var gitRepository = gitRepositories[branch.RemoteName];
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
