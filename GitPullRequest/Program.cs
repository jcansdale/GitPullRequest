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

        [Argument(0, Description = "The target git directory")]
        public string TargetDir { get; } = Directory.GetCurrentDirectory();

        [Option("--list", Description = "List local branches with associated pull requests")]
        public bool List { get; }

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
                if (List)
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
            var prs = service.FindPullRequests(gitHubRepositories, repo.Head);

            if (prs.Count > 0)
            {
                foreach (var pr in prs)
                {
                    var prUrl = service.GetPullRequestUrl(pr.Repository, pr.Number);
                    Browse(prUrl);
                }

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
            foreach (var branch in repo.Branches)
            {
                if (branch.IsRemote)
                {
                    continue;
                }

                var prs = service.FindPullRequests(gitHubRepositories, branch);
                var pr = prs.FirstOrDefault();
                if (pr == default)
                {
                    continue;
                }

                var remotePostfix = branch.RemoteName != "origin" ? $" ({branch.RemoteName})" : "";
                Console.WriteLine($"#{pr.Number} {branch.FriendlyName}{remotePostfix}");
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
