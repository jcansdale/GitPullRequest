using System.Collections.Generic;

namespace GitPullRequest.Services
{
    public class GitHubRepository
    {
        public IDictionary<string, string> References { get; set; }
        public string Url { get; set; }
    }
}