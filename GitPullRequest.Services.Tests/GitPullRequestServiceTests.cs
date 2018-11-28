using System.Linq;
using NUnit.Framework;
using GitPullRequest.Services;
using NSubstitute;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

public class GitPullRequestServiceTests
{
    public class TheFindCompareUrlMethod
    {
        [TestCase("https://github.com/owner/repo", "refs/heads/branchName", "refs/heads/branchName", "https://github.com/owner/repo/compare/branchName")]
        [TestCase("https://github.com/owner/repo", "refs/heads/deleted", "refs/heads/branchName", null)]
        [TestCase("https://github.com/owner/repo", null, "refs/heads/branchName", null, Description = "No tracking branch")]
        public void Foo(string originUrl, string upstreamBranchCanonicalName, string referenceCanonicalName,
            string expectUrl)
        {
            var repo = CreateRepository(originUrl,
                "headSha", upstreamBranchCanonicalName, new[]
                {
                    (referenceCanonicalName, "refSha")
                });
            var target = new GitPullRequestService();
            var gitHubRepository = target.GetGitHubRepository(repo);

            var compareUrl = target.FindCompareUrl(gitHubRepository, repo);

            Assert.That(compareUrl, Is.EqualTo(expectUrl));
        }
    }

    public class TheGetPullRequestUrlMethod
    {
        [TestCase("https://github.com/owner/repo", 777, "https://github.com/owner/repo/pull/777")]
        public void GetPullRequestUrl(string url, int number, string expectUrl)
        {
            var gitHubRepository = new GitHubRepository { Url = url };
            var target = new GitPullRequestService();

            var pullRequestUrl = target.GetPullRequestUrl(gitHubRepository, number);

            Assert.That(pullRequestUrl, Is.EqualTo(expectUrl));
        }
    }

    public class TheFindPullRequestsMethod
    {
        [Test]
        public void NoPrs()
        {
            var repo = CreateRepository("https://github.com/owner/repo", "sha", null, new (string, string)[0]);
            var target = new GitPullRequestService();
            var gitHubRepository = target.GetGitHubRepository(repo);

            var prs = target.FindPullRequests(gitHubRepository, repo);

            Assert.That(prs, Is.Empty);
        }

        [TestCase("headSha", "prSha")]
        [TestCase("sameHeadAndPrSha", "sameHeadAndPrSha")]
        public void LivePr(string headSha, string prSha)
        {
            var number = 777;
            var repo = CreateRepository("https://github.com/owner/repo",
                headSha, "refs/heads/one", new[]
                {
                    ("refs/heads/one", prSha),
                    ($"refs/pull/{number}/head", prSha)
                });
            var target = new GitPullRequestService();
            var gitHubRepository = target.GetGitHubRepository(repo);

            var prs = target.FindPullRequests(gitHubRepository, repo);

            Assert.That(prs.FirstOrDefault(), Is.EqualTo(number));
        }
    }

    static IRepository CreateRepository(
        string originUrl,
        string headSha, string upstreamBranchCanonicalName,
        (string CanonicalName, string TargetIdentifier)[] refs)
    {
        var repo = Substitute.For<IRepository>();
        var network = CreateNetwork(refs);
        var remotes = CreateRemoteCollection(originUrl);
        network.Remotes.Returns(remotes);
        repo.Network.Returns(network);
        var branch = CreateBranch(headSha, upstreamBranchCanonicalName);
        repo.Head.Returns(branch);
        return repo;
    }

    static object CreateRemoteCollection(string originUrl)
    {
        var remotes = Substitute.For<RemoteCollection>();
        var remote = Substitute.For<Remote>();
        remote.Url.Returns(originUrl);
        remotes["origin"].Returns(remote);
        return remotes;
    }

    static Branch CreateBranch(string sha, string upstreamBranchCanonicalName)
    {
        var isTracking = upstreamBranchCanonicalName != null;
        var branch = Substitute.For<Branch>();
        var commit = Substitute.For<Commit>();
        commit.Sha.Returns(sha);
        branch.Tip.Returns(commit);
        branch.IsTracking.Returns(isTracking);
        branch.UpstreamBranchCanonicalName.Returns(upstreamBranchCanonicalName);
        return branch;
    }

    static Network CreateNetwork((string CanonicalName, string TargetIdentifier)[] refs)
    {
        var network = Substitute.For<Network>();
        var references = refs.Select(r =>
        {
            var reference = Substitute.For<Reference>();
            reference.CanonicalName.Returns(r.CanonicalName);
            reference.TargetIdentifier.Returns(r.TargetIdentifier);
            return reference;
        }).ToList();
        network.ListReferences(null as Remote, null as CredentialsHandler).ReturnsForAnyArgs(references);
        return network;
    }
}
