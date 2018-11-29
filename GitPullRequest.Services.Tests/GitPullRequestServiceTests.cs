using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using GitPullRequest.Services;
using NSubstitute;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

public class GitPullRequestServiceTests
{
    public class TheFindCompareUrlMethod
    {
        [TestCase("https://github.com/owner/repo", "origin", "refs/heads/branchName", "refs/heads/branchName", "https://github.com/owner/repo/compare/branchName")]
        [TestCase("https://github.com/owner/repo", "origin", "refs/heads/deleted", "refs/heads/branchName", null)]
        [TestCase("https://github.com/owner/repo", null, null, "refs/heads/branchName", null, Description = "No tracking branch")]
        public void FindCompareUrl(string originUrl, string originName, string upstreamBranchCanonicalName, string referenceCanonicalName,
            string expectUrl)
        {
            var repo = CreateRepository(originUrl,
                "headSha", originName, upstreamBranchCanonicalName, new[]
                {
                    (referenceCanonicalName, "refSha")
                });
            var target = new GitPullRequestService();
            var gitHubRepositories = target.GetGitHubRepositories(repo);

            var compareUrl = target.FindCompareUrl(gitHubRepositories, repo);

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
            var repo = CreateRepository("https://github.com/owner/repo", "sha", null, null, new (string, string)[0]);
            var target = new GitPullRequestService();
            var gitHubRepositories = target.GetGitHubRepositories(repo);

            var prs = target.FindPullRequests(gitHubRepositories, repo.Head);

            Assert.That(prs, Is.Empty);
        }

        [TestCase("headSha", "prSha")]
        [TestCase("sameHeadAndPrSha", "sameHeadAndPrSha")]
        public void LivePr(string headSha, string prSha)
        {
            var number = 777;
            var repo = CreateRepository("https://github.com/owner/repo",
                headSha, "origin", "refs/heads/one", new[]
                {
                    ("refs/heads/one", prSha),
                    ($"refs/pull/{number}/head", prSha)
                });
            var target = new GitPullRequestService();
            var gitHubRepositories = target.GetGitHubRepositories(repo);

            var prs = target.FindPullRequests(gitHubRepositories, repo.Head);

            Assert.That(prs.FirstOrDefault().Number, Is.EqualTo(number));
        }
    }

    static IRepository CreateRepository(
        string originUrl,
        string headSha, string remoteName, string upstreamBranchCanonicalName,
        (string CanonicalName, string TargetIdentifier)[] refs)
    {
        var repo = Substitute.For<IRepository>();
        var remoteList = remoteName != null ? new Remote[] { CreateRemote(remoteName, originUrl) } : Array.Empty<Remote>();
        var network = CreateNetwork(remoteList);
        repo.Network.Returns(network);
        if (remoteName != null)
        {
            AddReferences(repo, remoteList[0], refs);
        }
        var branch = CreateBranch(headSha, remoteName, upstreamBranchCanonicalName);
        repo.Head.Returns(branch);
        return repo;
    }

    private static Remote CreateRemote(string remoteName, string originUrl)
    {
        var remote = Substitute.For<Remote>();
        remote.Name.Returns(remoteName);
        remote.Url.Returns(originUrl);
        return remote;
    }

    static Branch CreateBranch(string sha, string remoteName, string upstreamBranchCanonicalName)
    {
        var isTracking = remoteName != null && upstreamBranchCanonicalName != null;
        var branch = Substitute.For<Branch>();
        var commit = Substitute.For<Commit>();
        commit.Sha.Returns(sha);
        branch.Tip.Returns(commit);
        branch.IsTracking.Returns(isTracking);
        branch.RemoteName.Returns(remoteName);
        branch.UpstreamBranchCanonicalName.Returns(upstreamBranchCanonicalName);
        return branch;
    }

    static Network CreateNetwork(IList<Remote> remoteList)
    {
        var network = Substitute.For<Network>();
        var remotes = CreateRemoteCollection(remoteList);
        network.Remotes.Returns(remotes);
        return network;
    }

    static void AddReferences(IRepository repository, Remote remote, (string CanonicalName, string TargetIdentifier)[] refs)
    {
        var references = refs.Select(r =>
        {
            var reference = Substitute.For<Reference>();
            reference.CanonicalName.Returns(r.CanonicalName);
            reference.TargetIdentifier.Returns(r.TargetIdentifier);
            return reference;
        }).ToList();

        repository.Network.ListReferences(remote, Arg.Any<CredentialsHandler>()).Returns(references);
    }

    static RemoteCollection CreateRemoteCollection(IList<Remote> remoteList)
    {
        var remotes = Substitute.For<RemoteCollection>();
        remotes.GetEnumerator().Returns(_ => remoteList.GetEnumerator());
        foreach (var remote in remoteList)
        {
            remotes[remote.Name].Returns(remote);
        }
        return remotes;
    }
}
