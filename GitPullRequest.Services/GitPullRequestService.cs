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
        public IDictionary<string, GitHubRepository> GetGitHubRepositories(IRepository repo)
        {
            var gitHubRepositories = new Dictionary<string, GitHubRepository>();
            foreach (var remote in repo.Network.Remotes)
            {
                var remoteName = remote.Name;
                gitHubRepositories[remoteName] = GetGitHubRepository(repo, remoteName);
            }

            return gitHubRepositories;
        }

        public GitHubRepository GetGitHubRepository(IRepository repo, string remoteName)
        {
            return new GitHubRepository
            {
                RemoteName = remoteName,
                Url = GetRepositoryUrl(repo, remoteName),
                References = GetReferences(repo, remoteName)
            };
        }

        public IList<(GitHubRepository Repository, int Number, bool IsDeleted)> FindPullRequests(
            IDictionary<string, GitHubRepository> gitHubRepositories, Branch branch)
        {
            var isDeleted = false;
            string sha = null;
            if (branch.IsTracking)
            {
                var gitHubRepository = gitHubRepositories[branch.RemoteName];
                var references = gitHubRepository.References;
                isDeleted = !references.TryGetValue(branch.UpstreamBranchCanonicalName, out sha);
            }

            if (sha == null)
            {
                sha = branch.Tip.Sha;
            }

            return FindPullRequestsForSha(gitHubRepositories, sha)
                .Select(pr => (pr.Repository, pr.Number, isDeleted)).ToList();
        }

        public IList<(GitHubRepository Repository, int Number)> FindPullRequestsForSha(
            IDictionary<string, GitHubRepository> gitHubRepositories, string sha)
        {
            return gitHubRepositories
                .SelectMany(r => r.Value.References, (x, y) => (Repository: x.Value, Reference: y))
                .Where(kv => kv.Reference.Value == sha)
                .Select(kv => (kv.Repository, Number: FindPullRequestForCanonicalName(kv.Reference.Key)))
                .Where(pr => pr.Number != -1)
                .ToList();
        }

        public string GetPullRequestUrl(GitHubRepository gitHubRepository, int number)
        {
            return $"{gitHubRepository.Url}/pull/{number}";
        }

        public string FindCompareUrl(IDictionary<string, GitHubRepository> gitHubRepositories, IRepository repo)
        {
            var branch = repo.Head;
            if (!branch.IsTracking)
            {
                return null;
            }

            var upstreamBranchCanonicalName = branch.UpstreamBranchCanonicalName;
            var gitHubRepository = gitHubRepositories[branch.RemoteName];
            if (!gitHubRepository.References.ContainsKey(upstreamBranchCanonicalName))
            {
                return null;
            }

            var friendlyName = GetFriendlyName(upstreamBranchCanonicalName);
            return $"{gitHubRepository.Url}/compare/{friendlyName}";
        }

        static string GetFriendlyName(string canonicalName)
        {
            var prefix = "refs/heads/";
            if (!canonicalName.StartsWith(prefix))
            {
                return canonicalName;
            }

            return canonicalName.Substring(prefix.Length);
        }

        static int FindPullRequestForCanonicalName(string canonicalName)
        {
            var match = Regex.Match(canonicalName, "^refs/pull/([0-9]+)/head$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
            {
                return number;
            }

            return -1;
        }

        static string GetRepositoryUrl(IRepository repo, string remoteName)
        {
            var url = repo.Network.Remotes[remoteName].Url;
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
            var remote = repo.Network.Remotes[remoteName];
            var refs = repo.Network.ListReferences(remote, credentialsHandler);
            foreach (var reference in refs)
            {
                dictionary[reference.CanonicalName] = reference.TargetIdentifier;
            }

            return dictionary;
        }
    }
}
