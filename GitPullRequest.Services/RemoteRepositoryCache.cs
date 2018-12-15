using System.Collections.Generic;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public class RemoteRepositoryCache
    {
        readonly RemoteRepositoryFactory remoteRepositoryFactory;
        readonly IRepository repo;
        readonly Dictionary<string, RemoteRepository> cache;

        public RemoteRepositoryCache(RemoteRepositoryFactory remoteRepositoryFactory, IRepository repo)
        {
            this.remoteRepositoryFactory = remoteRepositoryFactory;
            this.repo = repo;
            cache = new Dictionary<string, RemoteRepository>();
        }

        public RemoteRepository this[string remoteName]
        {
            get
            {
                if (!cache.ContainsKey(remoteName))
                {
                    var remoteRepository = remoteRepositoryFactory.Create(repo, remoteName);
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
