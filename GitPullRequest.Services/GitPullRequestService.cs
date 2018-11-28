using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Alm.Authentication;

namespace GitPullRequest.Services
{
    public class GitPullRequestService
    {
        const string RemoteOrigin = "origin";

        public IList<int> FindPullRequests(IRepository repo)
        {
            var references = GetReferences(repo, RemoteOrigin);

            var branch = repo.Head;
            if (!branch.IsTracking || !references.TryGetValue(branch.UpstreamBranchCanonicalName, out string sha))
            {
                sha = branch.Tip.Sha;
            }

            return FindPullRequestsForSha(references, sha).ToList();
        }

        public string GetPullRequestUrl(IRepository repo, int number)
        {
            var repositoryUrl = GetRepositoryUrl(repo);
            return $"{repositoryUrl}/pull/{number}";
        }

        public string GetCompareUrl(IRepository repo)
        {
            var friendlyName = GetUpstreamBranchFriendlyName(repo.Head);
            var repositoryUrl = GetRepositoryUrl(repo);
            return $"{repositoryUrl}/compare/{friendlyName}";
        }

        static string GetUpstreamBranchFriendlyName(Branch branch)
        {
            var canonicalName = branch.UpstreamBranchCanonicalName;
            var prefix = "refs/heads/";
            if (!canonicalName.StartsWith(prefix))
            {
                return canonicalName;
            }

            return canonicalName.Substring(prefix.Length);
        }

        static IEnumerable<int> FindPullRequestsForSha(IDictionary<string, string> refs, string sha)
        {
            return refs
                .Where(kv => kv.Value == sha)
                .Select(kv => FindPullRequestForCanonicalName(kv.Key))
                .Where(pr => pr != null)
                .Cast<int>();
        }

        static int? FindPullRequestForCanonicalName(string canonicalName)
        {
            var match = Regex.Match(canonicalName, "^refs/pull/([0-9]+)/head$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
            {
                return number;
            }

            return null;
        }

        static object GetRepositoryUrl(IRepository repo)
        {
            var url = repo.Network.Remotes[RemoteOrigin].Url;
            var postfix = ".git";
            if (url.EndsWith(postfix))
            {
                url = url.Substring(0, url.Length - postfix.Length);
            }

            return url;
        }

        static IDictionary<string, string> GetReferences(IRepository repo, string remoteName)
        {
            var secrets = new SecretStore("git");
            var auth = new BasicAuthentication(secrets);
            var creds = auth.GetCredentials(new TargetUri("https://github.com"));

            CredentialsHandler credentialsHandler =
                (url, user, cred) => new UsernamePasswordCredentials
                {
                    Username = creds.Username,
                    Password = creds.Password
                };

            var dictionary = new Dictionary<string, string>();
            var origin = repo.Network.Remotes[remoteName];
            var refs = repo.Network.ListReferences(origin, credentialsHandler);
            foreach (var reference in refs)
            {
                dictionary[reference.CanonicalName] = reference.TargetIdentifier;
            }

            return dictionary;
        }
    }
}
