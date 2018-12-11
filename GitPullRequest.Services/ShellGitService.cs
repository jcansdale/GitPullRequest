using System;
using System.Diagnostics;
using System.Collections.Generic;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public class ShellGitService : IGitService
    {
        public IDictionary<string, string> ListReferences(IRepository repo, string remoteName)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"ls-remote {remoteName}",
                WorkingDirectory = repo.Info.WorkingDirectory,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var dictionary = new Dictionary<string, string>();
            using (var process = Process.Start(startInfo))
            {
                while (true)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    var split = line.Split('\t');
                    var targetIdentifier = split[0];
                    var canonicalName = split[1];
                    dictionary[canonicalName] = targetIdentifier;
                }
            }

            return dictionary;
        }

        public void Fetch(IRepository repo, string remoteName, string[] refSpecs, bool prune)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"fetch {remoteName} {string.Join(" ", refSpecs)}" + (prune ? " --prune" : ""),
                WorkingDirectory = repo.Info.WorkingDirectory,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var dictionary = new Dictionary<string, string>();
            using (var process = Process.Start(startInfo))
            {
                while (true)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    Console.WriteLine(line);
                }
            }
        }
    }
}
