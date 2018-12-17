using System;
using LibGit2Sharp;
using GitHub.Primitives;

namespace GitPullRequest.Services
{
    public class RemoteRepositoryFactory
    {
        readonly IGitService gitService;

        public RemoteRepositoryFactory(IGitService gitService)
        {
            this.gitService = gitService;
        }

        public RemoteRepository Create(IRepository repo, string remoteName)
        {
            var url = repo.Network.Remotes[remoteName].Url;
            var uriString = new UriString(url);
            if (uriString.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {
                return new GitHubRepository(gitService, repo, remoteName);
            }
            else if (uriString.Host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase) ||
                uriString.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase))
            {
                return new AzureDevOpsRepository(gitService, repo, remoteName);
            }

            throw new NotSupportedException("Sorry, your git host is not supported!");
        }
    }
}
