using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using Octoshift.Models;
using OctoshiftCLI.BbsToGithub;
using OctoshiftCLI.Extensions;
using OctoshiftCLI.Services;
using Xunit;

namespace OctoshiftCLI.Tests.BbsToGithub.Commands
{
    public class BbsInspectorServiceTests
    {
        private readonly OctoLogger _logger = TestHelpers.CreateMock<OctoLogger>().Object;
        private readonly Mock<BbsApi> _mockBbsApi = TestHelpers.CreateMock<BbsApi>();
        private readonly BbsInspectorService _service;

        private const string FOO_REPO = "FOO_REPO";
        private const string BBS_FOO_PROJECT_KEY = "FP";
        private const string BBS_BAR_PROJECT_KEY = "BP";

        public BbsInspectorServiceTests() => _service = new(_logger, _mockBbsApi.Object);

        [Fact]
        public async Task GetProjects_Should_Return_All_Projects()
        {
            // Arrange
            var project1 = "project1";
            var project2 = "project2";
            var projects = new[] {
                (Id: 1, Key: BBS_FOO_PROJECT_KEY, Name: project1),
                (Id: 1, Key: BBS_BAR_PROJECT_KEY, Name: project2)
            };

            _mockBbsApi.Setup(m => m.GetProjects()).ReturnsAsync(projects);

            // Act
            var result = await _service.GetProjects();

            // Assert
            result.Should().BeEquivalentTo(new List<string>() { BBS_FOO_PROJECT_KEY, BBS_BAR_PROJECT_KEY });
        }

        [Fact]
        public async Task GetRepos_Should_Return_All_Repos()
        {
            // Arrange
            var repo1 = "repo1";
            var repo2 = "repo2";
            var repos = new[]
            {
                (Id: 1, Slug: repo1, Name: repo1),
                (Id: 2, Slug: repo2, Name: repo2)
            };

            _mockBbsApi.Setup(m => m.GetRepos(BBS_FOO_PROJECT_KEY)).ReturnsAsync(repos);

            // Act
            var result = await _service.GetRepos(BBS_FOO_PROJECT_KEY);

            // Assert
            result.Should().BeEquivalentTo(new List<BbsRepository>() { new() { Name = repo1, Slug = repo1 }, new() { Name = repo2, Slug = repo2 } });
        }

        [Fact]
        public async Task GetRepoCount_Should_Return_Count()
        {
            // Arrange
            var project = "project";
            var projects = new[] {
                (Id: 1, Key: BBS_FOO_PROJECT_KEY, Name: project)
            };
            var repo1 = "repo1";
            var repo2 = "repo2";
            var repos = new[]
            {
                (Id: 1, Slug: repo1, Name: repo1),
                (Id: 2, Slug: repo2, Name: repo2)
            };
            var expectedCount = 2;

            _mockBbsApi.Setup(m => m.GetProjects()).ReturnsAsync(projects);
            _mockBbsApi.Setup(m => m.GetRepos(BBS_FOO_PROJECT_KEY)).ReturnsAsync(repos);

            // Act
            var result = await _service.GetRepoCount();

            // Assert
            result.Should().Be(expectedCount);
        }

        [Fact]
        public async Task GetRepoCount_With_Project_Keys_Should_Return_Count()
        {
            // Arrange
            var repo1 = "repo1";
            var repo2 = "repo2";
            var repos = new[]
            {
                (Id: 1, Slug: repo1, Name: repo1),
                (Id: 2, Slug: repo2, Name: repo2)
            };
            var expectedCount = 2;

            _mockBbsApi.Setup(m => m.GetRepos(BBS_FOO_PROJECT_KEY)).ReturnsAsync(repos);

            // Act
            var result = await _service.GetRepoCount(new[] { BBS_FOO_PROJECT_KEY });

            // Assert
            result.Should().Be(expectedCount);
            _mockBbsApi.Verify(m => m.GetProjects(), Times.Never);
        }

        [Fact]
        public async Task GetPullRequestCount_Should_Return_Count()
        {
            // Arrange
            var project = "project";
            var repo1 = "repo1";
            var repo2 = "repo2";
            var repos = new[]
            {
                (Id: 1, Slug: repo1, Name: repo1),
                (Id: 2, Slug: repo2, Name: repo2)
            };

            var prs1 = new[]
            {
                (Id: 1, Name: "pr1"),
                (Id: 2, Name: "pr2")
            };
            var prs2 = new[]
            {
                (Id: 3, Name: "pr3")
            };
            var expectedCount = 3;

            _mockBbsApi.Setup(m => m.GetRepos(project)).ReturnsAsync(repos);
            _mockBbsApi.Setup(m => m.GetRepositoryPullRequests(project, repo1)).ReturnsAsync(prs1);
            _mockBbsApi.Setup(m => m.GetRepositoryPullRequests(project, repo2)).ReturnsAsync(prs2);

            // Act
            var result = await _service.GetPullRequestCount(project);

            // Assert
            result.Should().Be(expectedCount);
        }

        [Fact]
        public async Task GetRepositoryPullRequestCount_Should_Return_Count()
        {
            // Arrange
            var project = "project";
            var repo = "repo1";
            var prs = new[]
            {
                (Id: 1, Name: "pr1"),
                (Id: 2, Name: "pr2")
            };
            var expectedCount = 2;

            _mockBbsApi.Setup(m => m.GetRepositoryPullRequests(project, repo)).ReturnsAsync(prs);

            // Act
            var result = await _service.GetRepositoryPullRequestCount(project, repo);

            // Assert
            result.Should().Be(expectedCount);
        }

        [Fact]
        public async Task GetLastCommitDate_Should_Return_LastCommitDate()
        {
            var expectedDate = new DateTime(2022, 2, 14);

            var commit = new
            {
                values = new[]
                {
                    new { authorTimestamp = 1644816000000 }
                }
            };
            var jObject = JObject.Parse(commit.ToJson());
            var response = Task.FromResult(jObject);

            _mockBbsApi.Setup(m => m.GetRepositoryLatestCommit(BBS_FOO_PROJECT_KEY, FOO_REPO)).Returns(response);

            var result = await _service.GetLastCommitDate(BBS_FOO_PROJECT_KEY, FOO_REPO);

            result.Should().Be(expectedDate);
        }

        [Fact]
        public async Task GetLastCommitDate_Should_Return_MinDate_When_No_Commits()
        {
            var commit = new
            {
                values = Array.Empty<object>()
            };
            var jObject = JObject.Parse(commit.ToJson());
            var response = Task.FromResult(jObject);

            _mockBbsApi.Setup(m => m.GetRepositoryLatestCommit(BBS_FOO_PROJECT_KEY, FOO_REPO)).Returns(response);

            var result = await _service.GetLastCommitDate(BBS_FOO_PROJECT_KEY, FOO_REPO);

            result.Should().Be(DateTime.MinValue);
        }

        [Fact]
        public async Task GetLastCommitDate_Should_Return_MinDate_When_Empty_JObject_Response()
        {
            var commit = new JObject();
            var jObject = JObject.Parse(commit.ToJson());
            var response = Task.FromResult(jObject);

            _mockBbsApi.Setup(m => m.GetRepositoryLatestCommit(BBS_FOO_PROJECT_KEY, FOO_REPO)).Returns(response);

            var result = await _service.GetLastCommitDate(BBS_FOO_PROJECT_KEY, FOO_REPO);

            result.Should().Be(DateTime.MinValue);
        }

        [Fact]
        public async Task GetRepositoryAndAttachmentsSize_Should_Return_Repository_And_Attachments_Size()
        {
            var sizes = new
            {
                repository = 10000UL,
                attachments = 10000UL
            };
            var jObject = JObject.Parse(sizes.ToJson());
            var response = Task.FromResult(jObject);

            _mockBbsApi.Setup(m => m.GetRepositorySize(BBS_FOO_PROJECT_KEY, FOO_REPO, "bbs-username", "bbs-password")).Returns(response);

            var result = await _service.GetRepositoryAndAttachmentsSize(BBS_FOO_PROJECT_KEY, FOO_REPO, "bbs-username", "bbs-password");

            result.Should().Be((sizes.repository, sizes.attachments));
        }
    }
}
