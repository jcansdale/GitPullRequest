using System.Collections.Generic;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public interface IGitService
    {
        void Fetch(IRepository repo, string remoteName, string[] refSpecs, bool prune);
        IDictionary<string, string> ListReferences(IRepository repo, string remoteName);
    }
}