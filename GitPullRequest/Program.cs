using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using LibGit2Sharp;
using GitPullRequest.Services;
using McMaster.Extensions.CommandLineUtils;

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

        [Option("--prune", Description = "Remove pull requests with deleted remote branches")]
        public bool Prune { get; }

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
                if (Prune)
                {
                    PruneBranches(service, repo);
                    return;
                }

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
                    var url = service.GetPullRequestUrl(pr.Repository, pr.Number);
                    Console.WriteLine(url);
                    TryBrowse(url);
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
                Console.WriteLine(compareUrl);
                TryBrowse(compareUrl);
                return;
            }

            Console.WriteLine("Couldn't find pull request or remote branch");
        }

        void ListBranches(GitPullRequestService service, Repository repo)
        {
            var gitHubRepositories = service.GetGitHubRepositories(repo);
            var prs = repo.Branches
                .Where(b => All || !b.IsRemote)
                .SelectMany(b => service.FindPullRequests(gitHubRepositories, b), (b, p) => (Branch: b, PullRequest: p))
                .Where(bp => PullRequestNumber == 0 || bp.PullRequest.Number == PullRequestNumber)
                .OrderBy(bp => bp.Branch.IsRemote)
                .ThenBy(bp => bp.PullRequest.Number)
                .ToList();

            if (prs.Count == 0)
            {
                if (PullRequestNumber == 0)
                {
                    Console.WriteLine("Couldn't find any branch with associated pull request in repository");
                    return;
                }

                Console.WriteLine($"Couldn't find branch associated with pull request #{PullRequestNumber} in repository");
                return;
            }

            foreach (var bp in prs)
            {
                var isHead = bp.Branch.IsCurrentRepositoryHead ? "* " : "  ";
                var remotePrefix = bp.PullRequest.Repository.RemoteName != "origin" ? bp.PullRequest.Repository.RemoteName : "";

                var postfix = bp.PullRequest.IsDeleted ? "x " : "" + bp.Branch.RemoteName != "origin" ? bp.Branch.RemoteName : "";
                if (postfix.Length > 0)
                {
                    postfix = $" ({postfix.TrimEnd()})";
                }

                Console.WriteLine($"{isHead}{remotePrefix}#{bp.PullRequest.Number} {bp.Branch.FriendlyName}{postfix}");
            }
        }

        void PruneBranches(GitPullRequestService service, Repository repo)
        {
            var gitHubRepositories = service.GetGitHubRepositories(repo);
            var prs = repo.Branches
                .Where(b => !b.IsRemote)
                .SelectMany(b => service.FindPullRequests(gitHubRepositories, b), (b, p) => (Branch: b, PullRequest: p))
                .Where(bp => bp.PullRequest.IsDeleted)
                .Where(bp => PullRequestNumber == 0 || bp.PullRequest.Number == PullRequestNumber)
                .ToList();

            if (prs.Count == 0)
            {
                Console.WriteLine($"Couldn't find any pull requests with deleted remote branches to remove");
                return;
            }

            foreach (var bp in prs)
            {
                if (bp.Branch.IsCurrentRepositoryHead)
                {
                    Console.WriteLine($"Can't remove current repository head #{bp.PullRequest.Number} {bp.Branch.FriendlyName}");
                }
                else
                {
                    Console.WriteLine($"Removing #{bp.PullRequest.Number} {bp.Branch.FriendlyName}");
                    repo.Branches.Remove(bp.Branch);
                }
            }
        }

        bool TryBrowse(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });

                return false;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }
    }
}
