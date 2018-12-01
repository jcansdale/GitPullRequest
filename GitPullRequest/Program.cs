using System;
using System.IO;
using System.Diagnostics;
using LibGit2Sharp;
using GitPullRequest.Services;
using McMaster.Extensions.CommandLineUtils;
using System.Linq;

namespace GitPullRequest
{
    [Command(Name = "git-pr", Description = "Git extension to view pull requests")]
    [HelpOption("-?")]
    public class Program
    {
        public static int Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        [Argument(0, Description = "The target pull request number")]
        public int PullRequestNumber { get; }

        [Option("--dir", Description = "The target git directory")]
        public string TargetDir { get; } = Directory.GetCurrentDirectory();

        [Option("--list", Description = "List local branches with associated pull requests")]
        public bool List { get; }

        [Option("--all", Description = "List all branches with associated pull requests")]
        public bool All { get; }

        void OnExecute()
        {
            var repoPath = Repository.Discover(TargetDir);
            if (repoPath == null)
            {
                Console.WriteLine("Couldn't find Git repository");
                return;
            }

            var service = new GitPullRequestService();
            using (var repo = new Repository(repoPath))
            {
                if (List || All)
                {
                    ListBranches(service, repo);
                    return;
                }

                BrowsePullRequest(service, repo);
            }
        }

        void BrowsePullRequest(GitPullRequestService service, Repository repo)
        {
            var gitHubRepositories = service.GetGitHubRepositories(repo);

            var prs = (PullRequestNumber == 0 ? service.FindPullRequests(gitHubRepositories, repo.Head) :
                repo.Branches
                    .SelectMany(b => service.FindPullRequests(gitHubRepositories, b))
                    .Where(pr => pr.Number == PullRequestNumber)).ToList();

            if (prs.Count > 0)
            {
                foreach (var pr in prs)
                {
                    Browse(service.GetPullRequestUrl(pr.Repository, pr.Number));
                }

                return;
            }

            if (PullRequestNumber != 0)
            {
                Console.WriteLine("Couldn't find pull request #" + PullRequestNumber);
                return;
            }

            var compareUrl = service.FindCompareUrl(gitHubRepositories, repo);
            if (compareUrl != null)
            {
                Browse(compareUrl);
                return;
            }

            Console.WriteLine("Couldn't find pull request or remote branch");
        }

        void ListBranches(GitPullRequestService service, Repository repo)
        {
            var gitHubRepositories = service.GetGitHubRepositories(repo);
            var groups = repo.Branches
                .Where(b => All || !b.IsRemote)
                .SelectMany(b => service.FindPullRequests(gitHubRepositories, b), (b, p) => (Branch: b, PullRequest: p))
                .Where(bp => PullRequestNumber == 0 || bp.PullRequest.Number == PullRequestNumber)
                .GroupBy(bp => bp.PullRequest)
                .OrderBy(g => g.Key.Repository.RemoteName)
                .ThenBy(g => g.Key.Number)
                .ToList();

            if (groups.Count == 0)
            {
                if (PullRequestNumber == 0)
                {
                    Console.WriteLine("Couldn't find any branch with associated pull request in repository");
                    return;
                }

                Console.WriteLine($"Couldn't find branch associated with pull request #{PullRequestNumber} in repository");
                return;
            }

            foreach (var group in groups)
            {
                var isHead = group.Any(bp => bp.Branch.IsCurrentRepositoryHead);
                var headMarker = isHead ? "* " : "  ";
                var remotePrefix = group.Key.Repository.RemoteName != "origin" ? group.Key.Repository.RemoteName : "";
                var isOrigin = group.Any(bp => bp.Branch.RemoteName == "origin");
                var remotes = string.Join(", ", group.Select(bp => bp.Branch.RemoteName));
                var friendlyNames = string.Join(", ", group.Select(pb => pb.Branch.FriendlyName));
                var remotePostfix = isOrigin ? "" : $" ({remotes}))";
                Console.WriteLine($"{headMarker}{remotePrefix}#{group.Key.Number} {friendlyNames}{remotePostfix}");
            }
        }

        void Browse(string pullUrl)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = pullUrl,
                UseShellExecute = true
            });
        }
    }
}
