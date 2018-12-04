using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitPullRequest.Services
{
    public static class GitRepositoryFactory
    {
        public static GitRepository Create(IRepository repo, string remoteName)
        {
            var uri = new Uri(repo.Network.Remotes[remoteName].Url);

            if (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {
                return new GitHubRepository(repo, remoteName);
            }
            else if (uri.Host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase))
            {
                return new AzureDevOpsRepository(repo, remoteName);
            }
            throw new NotSupportedException("Sorry, your git host is not supported!");
        }
    }
}
