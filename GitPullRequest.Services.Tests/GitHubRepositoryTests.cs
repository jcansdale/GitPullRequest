using GitPullRequest.Services;
using LibGit2Sharp;
using NSubstitute;
using NUnit.Framework;

public class GitHubRepositoryTests
{
    public class TheUrlProperty
    {
        [TestCase("https://github.com/owner/repo", "https://github.com/owner/repo")]
        [TestCase("https://github.com/owner/repo.git", "https://github.com/owner/repo")]
        [TestCase("https://github.com/owner/repo.git/", "https://github.com/owner/repo")]
        [TestCase("git://github.com/owner/repo", "https://github.com/owner/repo")]
        [TestCase("git@github.com:owner/repo.git", "https://github.com/owner/repo")]
        [TestCase("ssh://git@github.com/owner/repo.git", "https://github.com/owner/repo")]
        [TestCase("http://github.com/owner/repo", "http://github.com/owner/repo")] // Keeps http
        [TestCase(@"c:\source\repo", "file:///c:/source/repo")]
        public void Url(string remoteUrl, string expectUrl)
        {
            var gitService = Substitute.For<IGitService>();
            var remoteName = "remoteName";
            var repo = CreateRepository(remoteName, remoteUrl);

            var target = new GitHubRepository(gitService, repo, remoteName);

            Assert.That(target.Url, Is.EqualTo(expectUrl));
        }

        private static IRepository CreateRepository(string remoteName, string remoteUrl)
        {
            var repository = Substitute.For<IRepository>();
            var network = Substitute.For<Network>();
            var remoteCollection = Substitute.For<RemoteCollection>();
            var remote = Substitute.For<Remote>();
            remote.Name.Returns(remoteName);
            remote.Url.Returns(remoteUrl);
            remoteCollection[remoteName].Returns(remote);
            network.Remotes.Returns(remoteCollection);
            repository.Network.Returns(network);
            return repository;
        }
    }

}
