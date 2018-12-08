using System;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public static class GitRepositoryFactory
    {
        public static RemoteRepository Create(GitService gitService, IRepository repo, string remoteName)
        {
            var url = repo.Network.Remotes[remoteName].Url;
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                return null;
            }

            if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (uri.GetLeftPart(UriPartial.Authority).Contains("@"))
            {
                return null;
            }

            if (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {
                return new GitHubRepository(gitService, repo, remoteName);
            }
            else if (uri.Host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase) || uri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase))
            {
                return new AzureDevOpsRepository(gitService, repo, remoteName);
            }
            throw new NotSupportedException("Sorry, your git host is not supported!");
        }
    }
}
