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

        [Option("--remote", Description = "List remote branches with associated pull requests")]
        public string Remote { get; }

        [Option("--prune", Description = "Remove pull requests with deleted remote branches")]
        public bool Prune { get; }

        [Option("--shell", Description = "Shell out to git for ls-remote and fetch")]
        public bool Shell { get; }

        void OnExecute()
        {
            var repoPath = Repository.Discover(TargetDir);
            if (repoPath == null)
            {
                Console.WriteLine("Couldn't find Git repository");
                return;
            }

            var gitService = Shell ? new ShellGitService() : new LibGitService() as IGitService;
            var service = new GitPullRequestService(gitService);
            using (var repo = new Repository(repoPath))
            {
                if (Prune)
                {
                    PruneBranches(service, repo);
                    return;
                }

                try
                {
                    if (List || Remote != null)
                    {
                        ListBranches(service, repo, Remote);
                        return;
                    }

                    BrowsePullRequest(service, repo);
                }
                catch (LibGit2SharpException e)
                {
                    Console.WriteLine($"{e.GetType()}: {e.Message}");
                    Console.WriteLine("Please try again using the --shell option to use native git authentication");
                }
            }
        }

        void BrowsePullRequest(GitPullRequestService service, Repository repo)
        {
            var gitRepositories = service.GetGitRepositories(repo);

            var prs = (PullRequestNumber == 0 ? service.FindPullRequests(gitRepositories, repo.Head) :
                repo.Branches
                    .SelectMany(b => service.FindPullRequests(gitRepositories, b))
                    .Where(pr => pr.Number == PullRequestNumber)
                    .Distinct()).ToList();

            if (prs.Count > 0)
            {
                foreach (var pr in prs)
                {
                    var url = pr.Repository.GetPullRequestUrl(pr.Number);
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

            var compareUrl = service.FindCompareUrl(gitRepositories, repo);
            if (compareUrl != null)
            {
                Console.WriteLine(compareUrl);
                TryBrowse(compareUrl);
                return;
            }

            Console.WriteLine("Couldn't find pull request or remote branch");
        }

        void ListBranches(GitPullRequestService service, Repository repo, string remoteName)
        {
            var gitRepositories = service.GetGitRepositories(repo);
            var prs = repo.Branches
                .Where(b => remoteName == null && !b.IsRemote || remoteName != null && b.IsRemote && b.RemoteName == remoteName)
                .SelectMany(b => service.FindPullRequests(gitRepositories, b), (b, p) => (Branch: b, PullRequest: p))
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
                if (remoteName != null &&
                    bp.Branch.IsRemote &&
                    bp.Branch.RemoteName != bp.PullRequest.Repository.RemoteName &&
                    bp.PullRequest.Repository.References.ContainsKey(bp.Branch.UpstreamBranchCanonicalName))
                {
                    // Ignore branches forked from parent repository
                    continue;
                }

                var isHead = bp.Branch.IsCurrentRepositoryHead ? "* " : "  ";
                var remotePrefix = bp.PullRequest.Repository.RemoteName != "origin" ? bp.PullRequest.Repository.RemoteName : "";

                var branchRemoteName = bp.Branch.RemoteName ?? "?";
                var postfix = (bp.PullRequest.IsDeleted ? "x " : "") + (branchRemoteName != "origin" ? branchRemoteName : "");
                if (postfix.Length > 0)
                {
                    postfix = $" ({postfix.TrimEnd()})";
                }

                Console.WriteLine($"{isHead}{remotePrefix}#{bp.PullRequest.Number} {bp.Branch.FriendlyName}{postfix}");
            }
        }

        void PruneBranches(GitPullRequestService service, Repository repo)
        {
            var gitHubRepositories = service.GetGitRepositories(repo);
            var prs = repo.Branches
                .Where(b => !b.IsRemote)
                .SelectMany(b => service.FindPullRequests(gitHubRepositories, b), (b, p) => (Branch: b, PullRequest: p))
                .Where(bp => bp.PullRequest.IsDeleted)
                .Where(bp => PullRequestNumber == 0 || bp.PullRequest.Number == PullRequestNumber)
                .GroupBy(bp => bp.Branch)
                .Select(g => g.First()) // Select only one PR for each branch
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
