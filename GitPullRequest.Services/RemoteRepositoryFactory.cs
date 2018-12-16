using System;
using LibGit2Sharp;
using GitHub.Primitives;

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

            var uriString = new UriString(url);
            if (!uriString.IsHypertextTransferProtocol && !uriString.IsFileUri)
            {
                // LibGit2Sharp only works with HTTP and file remotes
                gitService = shellGitService;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) &&
                uri.GetLeftPart(UriPartial.Authority).Contains("@"))
            {
                // LibGit2Sharp doesn't appear to work when a user is specified
                gitService = shellGitService;
            }

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
