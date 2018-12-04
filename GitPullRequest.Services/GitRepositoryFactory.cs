using System;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public static class GitRepositoryFactory
    {
        public static RemoteRepository Create(IRepository repo, string remoteName)
        {
            var uri = new Uri(repo.Network.Remotes[remoteName].Url);

            if (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {
                return new GitHubRepository(repo, remoteName);
            }
            else if (uri.Host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase) || uri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase))
            {
                return new AzureDevOpsRepository(repo, remoteName);
            }
            throw new NotSupportedException("Sorry, your git host is not supported!");
        }
    }
}
