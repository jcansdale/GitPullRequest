using System;
using System.Collections.Generic;
using GitHub.Primitives;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public class DynamicGitService : IGitService
    {
        readonly IGitService libGitService;
        readonly IGitService shellGitService;
        readonly bool shell;

        public DynamicGitService(
            IGitService libGitService,
            IGitService shellGitService,
            bool shell = false)
        {
            this.libGitService = libGitService;
            this.shellGitService = shellGitService;
            this.shell = shell;
        }

        public void Fetch(IRepository repo, string remoteName, string[] refSpecs, bool prune)
        {
            GetGitService(repo, remoteName).Fetch(repo, remoteName, refSpecs, prune);
        }

        public IDictionary<string, string> ListReferences(IRepository repo, string remoteName)
        {
            return GetGitService(repo, remoteName).ListReferences(repo, remoteName);
        }

        public IGitService GetGitService(IRepository repo, string remoteName)
        {
            if (shell)
            {
                return shellGitService;
            }

            var url = repo.Network.Remotes[remoteName].Url;
            var uriString = new UriString(url);
            if (!uriString.IsHypertextTransferProtocol && !uriString.IsFileUri)
            {
                // LibGit2Sharp only works with HTTP and file remotes
                return shellGitService;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) &&
                uri.GetLeftPart(UriPartial.Authority).Contains("@"))
            {
                // LibGit2Sharp doesn't appear to work when a user is specified
                return shellGitService;
            }

            return libGitService;
        }
    }
}
