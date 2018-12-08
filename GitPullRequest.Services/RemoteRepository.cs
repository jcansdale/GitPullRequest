using System.Collections.Generic;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public abstract class RemoteRepository
    {
        protected GitService GitService { get; }
        public string RemoteName { get; }
        public string Url { get; }
        public IDictionary<string, string> References { get; }

        protected RemoteRepository(GitService gitService, IRepository repo, string remoteName)
        {
            GitService = gitService;
            RemoteName = remoteName;
            Url = GetRepositoryUrl(repo, remoteName);
            References = GetReferences(repo, remoteName);
        }

        public abstract string GetPullRequestUrl(int number);

        public abstract string GetCompareUrl(string friendlyBranchName);

        public abstract int FindPullRequestForCanonicalName(string key);

        protected virtual string GetRepositoryUrl(IRepository repo, string remoteName)
        {
            return repo.Network.Remotes[remoteName].Url;
        }

        protected virtual IDictionary<string, string> GetReferences(IRepository repo, string remoteName)
        {
            var refs = GitService.ListReferences(repo, remoteName);

            var dictionary = new Dictionary<string, string>();
            foreach (var reference in refs)
            {
                var (targetIdentifier, canonicalName) = (reference.Value, reference.Key);
                dictionary[canonicalName] = GetTipForReference(repo, canonicalName, targetIdentifier);
            }

            return dictionary;
        }

        protected virtual string GetTipForReference(IRepository repo, string canonicalName, string targetIdentifier)
        {
            return targetIdentifier;
        }
    }
}
