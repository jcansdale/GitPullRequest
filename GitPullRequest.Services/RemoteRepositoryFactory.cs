using System;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public class RemoteRepositoryFactory
    {
        readonly IGitService libGitService;
        readonly IGitService shellGitService;
        readonly bool shell;

        /// <summary>
        /// The default <see cref="IGitService"/> implementation or null for automatic.
        /// </summary>
        /// <param name="defaultGitService"></param>
        public RemoteRepositoryFactory(
            IGitService libGitService,
            IGitService shellGitService,
            bool shell = false)
        {
            this.libGitService = libGitService;
            this.shellGitService = shellGitService;
            this.shell = shell;
        }

        public RemoteRepository Create(IRepository repo, string remoteName)
        {
            var gitService = shell ? shellGitService : libGitService;

            var url = repo.Network.Remotes[remoteName].Url;
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                throw new NotSupportedException($"Unknown URI '{url}' for {remoteName}");
            }

            if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                gitService = shellGitService;
            }

            if (uri.GetLeftPart(UriPartial.Authority).Contains("@"))
            {
                gitService = shellGitService;
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
