using System.Collections.Generic;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public class RemoteRepositoryCache
    {
        readonly IGitService gitService;
        readonly IRepository repo;
        readonly Dictionary<string, RemoteRepository> cache;

        public RemoteRepositoryCache(IGitService gitService, IRepository repo)
        {
            this.gitService = gitService;
            this.repo = repo;
            cache = new Dictionary<string, RemoteRepository>();
        }

        public RemoteRepository this[string remoteName]
        {
            get
            {
                if (!cache.ContainsKey(remoteName))
                {
                    var remoteRepository = GitRepositoryFactory.Create(gitService, repo, remoteName);
                    if (remoteRepository != null)
                    {
                        cache[remoteName] = remoteRepository;
                    }

                    cache[remoteName] = remoteRepository;
                }

                return cache[remoteName];
            }
        }
    }
}
