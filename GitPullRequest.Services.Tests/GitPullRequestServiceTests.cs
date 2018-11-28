using System;
using NUnit.Framework;
using GitPullRequest.Services;
using NSubstitute;
using LibGit2Sharp;
using System.Linq;
using LibGit2Sharp.Handlers;

public class GitPullRequestServiceTests
{
    public class TheFindPullRequestsMethod
    {
        [Test]
        public void NoPrs()
        {
            var repo = CreateRepository("sha", false, null, null, new (string, string)[0]);
            var target = new GitPullRequestService();

            var prs = target.FindPullRequests(repo);

            Assert.That(prs, Is.Empty);
        }

        [TestCase("headSha", "prSha")]
        [TestCase("sameHeadAndPrSha", "sameHeadAndPrSha")]
        public void LivePr(string headSha, string prSha)
        {
            var number = 777;
            var repo = CreateRepository(headSha, true, "refs/heads/one", "refs/remotes/origin/one", new[]
            {
                ("refs/heads/one", prSha),
                ($"refs/pull/{number}/head", prSha)
            });
            var target = new GitPullRequestService();

            var prs = target.FindPullRequests(repo);

            Assert.That(prs.FirstOrDefault(), Is.EqualTo(number));
        }

        static IRepository CreateRepository(
            string headSha, bool isTracking, string upstreamBranchCanonicalName, string trackedBranchCanonicalName,
            (string CanonicalName, string TargetIdentifier)[] refs)
        {
            var repo = Substitute.For<IRepository>();
            var network = CreateNetwork(refs);
            repo.Network.Returns(network);
            var branch = CreateBranch(headSha, isTracking, upstreamBranchCanonicalName, trackedBranchCanonicalName);
            repo.Head.Returns(branch);
            return repo;
        }

        static Branch CreateBranch(string sha, bool isTracking, string upstreamBranchCanonicalName,
            string trackedBranchCanonicalName)
        {
            var branch = Substitute.For<Branch>();
            var commit = Substitute.For<Commit>();
            commit.Sha.Returns(sha);
            branch.Tip.Returns(commit);
            branch.IsTracking.Returns(isTracking);
            branch.UpstreamBranchCanonicalName.Returns(upstreamBranchCanonicalName);
            if (trackedBranchCanonicalName != null)
            {
                var trackedBranch = Substitute.For<Branch>();
                trackedBranch.CanonicalName.Returns(trackedBranchCanonicalName);
                branch.TrackedBranch.Returns(trackedBranch);
            }
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
}
