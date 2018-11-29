using System.Collections.Generic;

namespace GitPullRequest.Services
{
    public class GitHubRepository
    {
        public string RemoteName { get; set; }
        public string Url { get; set; }
        public IDictionary<string, string> References { get; set; }
    }
}