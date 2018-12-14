using LibGit2Sharp;
using System.Collections.Generic;

namespace GitPullRequest.Services
{
    public class RemoteRepositoryCache : Dictionary<string, RemoteRepository>
    {
        public RemoteRepositoryCache(IGitService gitService, IRepository repo)
        {
            foreach (var remote in repo.Network.Remotes)
            {
                var remoteName = remote.Name;
                var hostedRepository = GitRepositoryFactory.Create(gitService, repo, remote.Name);
                if (hostedRepository != null)
                {
                    this[remoteName] = hostedRepository;
                }
            }
        }
    }
}
