using System.Collections.Generic;
using LibGit2Sharp;

namespace GitPullRequest.Services
{
    public interface IGitService
    {
        void Fetch(IRepository repo, string remoteName, string refSpec);
        IDictionary<string, string> ListReferences(IRepository repo, string remoteName);
    }
}