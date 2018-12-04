using System;
using System.Collections.Generic;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Alm.Authentication;

namespace GitPullRequest.Services
{
    public abstract class RemoteRepository
    {
        public string RemoteName { get; }
        public string Url { get; }
        public IDictionary<string, string> References { get; }

        protected RemoteRepository(IRepository repo, string remoteName)
        {
            RemoteName = remoteName;
            Url = GetRepositoryUrl(repo, remoteName);
            References = GetReferences(repo, remoteName);
        }

        public abstract string GetPullRequestUrl(int number);

        public abstract string GetCompareUrl(string friendlyBranchName);

        public abstract int FindPullRequestForCanonicalName(string key);

        protected virtual string GetRepositoryUrl(IRepository repo, string remoteName)
        {
            return repo.Network.Remotes[remoteName].Url;
        }

        protected virtual IDictionary<string, string> GetReferences(IRepository repo, string remoteName)
        {
            CredentialsHandler credentialsHandler = CreateCredentialsHandler();

            var dictionary = new Dictionary<string, string>();
            var remote = repo.Network.Remotes[remoteName];
            var refs = repo.Network.ListReferences(remote, credentialsHandler);
            foreach (var reference in refs)
            {
                var sha = GetTipForReference(repo, reference);
                dictionary[reference.CanonicalName] = sha;
            }

            return dictionary;
        }

        protected CredentialsHandler CreateCredentialsHandler()
        {
            var remoteUri = new Uri(Url);
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

        protected virtual string GetTipForReference(IRepository repo, Reference reference)
        {
            return reference.TargetIdentifier;
        }
    }
}