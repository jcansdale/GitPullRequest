using System;
using System.Collections.Generic;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public class RemoteRepositoryCache
    {
        readonly Dictionary<string, RemoteRepository> cache = new Dictionary<string, RemoteRepository>();

        readonly RemoteRepositoryFactory remoteRepositoryFactory;
        readonly IRepository repo;
        readonly Action<Exception> exceptionLogger;

        public RemoteRepositoryCache(
            RemoteRepositoryFactory remoteRepositoryFactory,
            IRepository repo,
            Action<Exception> exceptionLogger)
        {
            this.remoteRepositoryFactory = remoteRepositoryFactory;
            this.repo = repo;
            this.exceptionLogger = exceptionLogger;
        }

        public RemoteRepository FindRemoteRepository(string remoteName)
        {
            if (!cache.ContainsKey(remoteName))
            {
                TryCreateRemoteRepository(remoteName, out RemoteRepository remoteRepository);
                cache[remoteName] = remoteRepository;
            }

            return cache[remoteName];
        }

        bool TryCreateRemoteRepository(string remoteName, out RemoteRepository remoteRepository)
        {
            try
            {
                remoteRepository = remoteRepositoryFactory.Create(repo, remoteName);
                return true;
            }
            catch (Exception e)
            {
                exceptionLogger(e);
                remoteRepository = null;
                return false;
            }
        }
    }
}
