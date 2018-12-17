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
            var target = CreateGitPullRequestService();
            var gitHubRepositories = target.GetRemoteRepositoryCache(repo, e => { });

            var compareUrl = target.FindCompareUrl(gitHubRepositories, repo);

            Assert.That(compareUrl, Is.EqualTo(expectUrl));
        }
    }

    public class TheFindPullRequestsMethod
    {
        [Test]
        public void No_Pull_Request()
        {
            var repo = CreateRepository("sha", null, null, Array.Empty<Remote>());
            var target = CreateGitPullRequestService();
            var remoteRepositoryCache = target.GetRemoteRepositoryCache(repo, e => { });
            var upstreamRepositories = CreateUpstreamRepositoires(remoteRepositoryCache, repo);

            var prs = target.FindPullRequests(remoteRepositoryCache, upstreamRepositories, repo.Head);

            Assert.That(prs, Is.Empty);
        }

        [TestCase("headSha", "prSha")]
        [TestCase("sameHeadAndPrSha", "sameHeadAndPrSha")]
        public void Live_Pull_Request(string headSha, string prSha)
        {
            var number = 777;
            var remoteName = "origin";
            var remoteUrl = "https://github.com/owner/repo";
            var remote = CreateRemote(remoteName, remoteUrl);
            var repo = CreateRepository(headSha, "origin", "refs/heads/one", new[] { remote });
            AddRemoteReferences(repo, remote, new Dictionary<string, string>
                {
                    { "refs/heads/one", prSha },
                    { $"refs/pull/{number}/head", prSha }
                });
            var target = CreateGitPullRequestService();
            var remoteRepositoryCache = target.GetRemoteRepositoryCache(repo, e => { });
            var upstreamRepositories = CreateUpstreamRepositoires(remoteRepositoryCache, repo);

            var prs = target.FindPullRequests(remoteRepositoryCache, upstreamRepositories, repo.Head);

            var pr = prs.FirstOrDefault();
            Assert.That(pr.Repository.RemoteName, Is.EqualTo(remoteName));
            Assert.That(pr.Repository.Url, Is.EqualTo(remoteUrl));
            Assert.That(pr.Number, Is.EqualTo(number));
        }

        [Test]
        public void Pull_Request_To_Upstream_Repository()
        {
            var number = 777;
            var originUrl = "https://github.com/origin/repo";
            var upstreamRemoteName = "upstream";
            var upstreamUrl = "https://github.com/upstream/repo";
            var prSha = "prSha";
            var originRemote = CreateRemote("origin", originUrl);
            var upstreamRemote = CreateRemote(upstreamRemoteName, upstreamUrl);
            var repo = CreateRepository(prSha, "origin", "refs/heads/one", new[] { originRemote, upstreamRemote });
            AddRemoteReferences(repo, originRemote, new Dictionary<string, string> { { "refs/heads/one", prSha } });
            AddRemoteReferences(repo, upstreamRemote, new Dictionary<string, string> { { $"refs/pull/{number}/head", prSha } });
            var target = CreateGitPullRequestService();
            var gitHubRepositories = target.GetRemoteRepositoryCache(repo, e => { });
            var upstreamRepositories = repo.Network.Remotes
                .Select(r => gitHubRepositories.FindRemoteRepository(r.Name))
                .Where(r => r != null)
                .ToList();

            var prs = target.FindPullRequests(gitHubRepositories, upstreamRepositories, repo.Head);

            var pr = prs.FirstOrDefault();
            Assert.That(pr.Repository.Url, Is.EqualTo(upstreamUrl));
            Assert.That(pr.Repository.RemoteName, Is.EqualTo(upstreamRemoteName));
            Assert.That(pr.Number, Is.EqualTo(number));
        }
    }

    static GitPullRequestService CreateGitPullRequestService()
    {
        var gitService = new LibGitService();
        var factory = new RemoteRepositoryFactory(new LibGitService(), new ShellGitService(), false);
        return new GitPullRequestService(factory);
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

    private static Remote CreateRemote(string name, string url)
    {
        var remote = Substitute.For<Remote>();
        remote.Name.Returns(name);
        remote.Url.Returns(url);
        return remote;
    }

    static IList<RemoteRepository> CreateUpstreamRepositoires(RemoteRepositoryCache remoteRepositoryCache, IRepository repo)
    {
        return repo.Network.Remotes
            .Select(r => remoteRepositoryCache.FindRemoteRepository(r.Name))
            .Where(r => r != null)
            .ToList();
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
