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
        [TestCase("origin", "https://github.com/owner/repo", "refs/heads/branchName", "refs/heads/branchName", "https://github.com/owner/repo/compare/branchName")]
        [TestCase("origin", "https://github.com/owner/repo", "refs/heads/deleted", "refs/heads/branchName", null)]
        [TestCase(null, null, null, "refs/heads/branchName", null, Description = "No tracking branch")]
        public void FindCompareUrl(string remoteName, string remoteUrl, string upstreamBranchCanonicalName, string referenceCanonicalName,
            string expectUrl)
        {
            var remote = remoteName != null ? CreateRemote(remoteName, remoteUrl) : null;
            var remotes = remoteName != null ? new[] { remote } : Array.Empty<Remote>();
            var repo = CreateRepository("headSha", remoteName, upstreamBranchCanonicalName, remotes);
            if (remote != null)
            {
                AddRemoteReferences(repo, remote, new Dictionary<string, string> { { referenceCanonicalName, "refSha" } });
            }
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
            var repo = CreateRepository("sha", null, null, Array.Empty<Remote>());
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
            var remote = CreateRemote("origin", "https://github.com/owner/repo");
            var repo = CreateRepository(headSha, "origin", "refs/heads/one", new[] { remote });
            AddRemoteReferences(repo, remote, new Dictionary<string, string>
                {
                    { "refs/heads/one", prSha },
                    { $"refs/pull/{number}/head", prSha }
                });
            var target = new GitPullRequestService();
            var gitHubRepositories = target.GetGitHubRepositories(repo);

            var prs = target.FindPullRequests(gitHubRepositories, repo.Head);

            Assert.That(prs.FirstOrDefault().Number, Is.EqualTo(number));
        }
    }

    static IRepository CreateRepository(
        string headSha, string remoteName, string upstreamBranchCanonicalName,
        IList<Remote> remoteList)
    {
        var repo = Substitute.For<IRepository>();
        var network = CreateNetwork(remoteList);
        repo.Network.Returns(network);
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

    static void AddRemoteReferences(IRepository repository, Remote remote, IDictionary<string, string> refs)
    {
        var references = refs.Select(r =>
        {
            var reference = Substitute.For<Reference>();
            reference.CanonicalName.Returns(r.Key);
            reference.TargetIdentifier.Returns(r.Value);
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
