using System;
using System.IO;
using System.Diagnostics;
using LibGit2Sharp;
using GitPullRequest.Services;

namespace GitPullRequest
{
    class Program
    {
        static void Main(string[] args)
        {
            var targetDir = args.Length >= 1 ? args[0] : Directory.GetCurrentDirectory();
            var repoPath = Repository.Discover(targetDir);
            if (repoPath == null)
            {
                Console.WriteLine("Couldn't find Git repository");
                return;
            }

            using (var repo = new Repository(repoPath))
            {
                var service = new GitPullRequestService();
                var prs = service.FindPullRequests(repo);

                if (prs.Count == 0)
                {
                    Console.WriteLine("Couldn't find pull request for this branch");
                    return;
                }

                foreach (var pr in prs)
                {
                    var prUrl = service.GetPullRequestUrl(repo, pr);
                    Browse(prUrl);
                }
            }
        }

        static void Browse(string pullUrl)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = pullUrl,
                UseShellExecute = true
            });
        }
    }
}
