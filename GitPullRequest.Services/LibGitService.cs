using System;
using System.Collections.Generic;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Alm.Authentication;

namespace GitPullRequest.Services
{
    public class LibGitService : IGitService
    {
        public IDictionary<string, string> ListReferences(IRepository repo, string remoteName)
        {
            var credentialsHandler = CreateCredentialsHandler(repo, remoteName);

            var dictionary = new Dictionary<string, string>();
            var remote = repo.Network.Remotes[remoteName];
            var refs = credentialsHandler != null ?
                repo.Network.ListReferences(remote, credentialsHandler) : repo.Network.ListReferences(remote);
            foreach (var reference in refs)
            {
                dictionary[reference.CanonicalName] = reference.TargetIdentifier;
            }

            return dictionary;
        }

        public void Fetch(IRepository repo, string remoteName, string[] refSpecs, bool prune)
        {
            var credentialsHandler = CreateCredentialsHandler(repo, remoteName);
            ProgressHandler progressHandler = text =>
            {
                Console.Write(text);
                return true;
            };

            repo.Network.Fetch(remoteName, refSpecs, new FetchOptions
            {
                CredentialsProvider = credentialsHandler,
                OnProgress = progressHandler
            });
        }

        static CredentialsHandler CreateCredentialsHandler(IRepository repo, string remoteName)
        {
            var remote = repo.Network.Remotes[remoteName];
            var remoteUri = new Uri(remote.Url);

            var secrets = new SecretStore("git");
            var auth = new BasicAuthentication(secrets);

            var targetUrl = remoteUri.GetLeftPart(UriPartial.Authority);
            var creds = auth.GetCredentials(new TargetUri(targetUrl));
            if (creds == null)
            {
                return null;
            }

            CredentialsHandler credentialsHandler =
                (url, user, cred) => new UsernamePasswordCredentials
                {
                    Username = creds.Username,
                    Password = creds.Password
                };
            return credentialsHandler;
        }
    }
}
