﻿using System;
using System.Collections.Generic;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Alm.Authentication;

namespace GitPullRequest.Services
{
    public class GitService
    {
        public IDictionary<string, string> ListReferences(IRepository repo, string remoteName)
        {
            var credentialsHandler = CreateCredentialsHandler(repo, remoteName);

            var dictionary = new Dictionary<string, string>();
            var remote = repo.Network.Remotes[remoteName];
            var refs = repo.Network.ListReferences(remote, credentialsHandler);
            foreach (var reference in refs)
            {
                dictionary[reference.CanonicalName] = reference.TargetIdentifier;
            }

            return dictionary;
        }

        public void Fetch(IRepository repo, string remoteName, string refSpec)
        {
            var credentialsHandler = CreateCredentialsHandler(repo, remoteName);
            repo.Network.Fetch(remoteName, new[] { refSpec }, new FetchOptions { CredentialsProvider = credentialsHandler });
        }

        static CredentialsHandler CreateCredentialsHandler(IRepository repo, string remoteName)
        {
            var remote = repo.Network.Remotes[remoteName];
            var remoteUri = new Uri(remote.Url);

            var secrets = new SecretStore("git");
            var auth = new BasicAuthentication(secrets);
            var creds = auth.GetCredentials(new TargetUri(remoteUri.GetLeftPart(UriPartial.Authority)));

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
