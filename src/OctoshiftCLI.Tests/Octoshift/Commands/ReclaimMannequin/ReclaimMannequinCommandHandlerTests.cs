using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using OctoshiftCLI.Commands.ReclaimMannequin;
using OctoshiftCLI.Services;
using Xunit;

namespace OctoshiftCLI.Tests.Octoshift.Commands.ReclaimMannequin;

public class ReclaimMannequinCommandHandlerTests
{
    private readonly Mock<OctoLogger> _mockOctoLogger = TestHelpers.CreateMock<OctoLogger>();
    private readonly Mock<ReclaimService> _mockReclaimService = TestHelpers.CreateMock<ReclaimService>();
    private readonly Mock<ConfirmationService> _confirmationService = TestHelpers.CreateMock<ConfirmationService>();
    private readonly ReclaimMannequinCommandHandler _handler;

    private const string GITHUB_ORG = "FooOrg";
    private const string MANNEQUIN_USER = "mona";
    private const string TARGET_USER = "mona_emu";

    public ReclaimMannequinCommandHandlerTests()
    {
        _handler = new ReclaimMannequinCommandHandler(_mockOctoLogger.Object, _mockReclaimService.Object, _confirmationService.Object)
        {
            FileExists = _ => true,
            GetFileContent = _ => Array.Empty<string>()
        };
    }

    [Fact]
    public async Task CSV_CSVFileDoesNotExist_OctoshiftCliException()
    {
        _handler.FileExists = _ => false;

        var args = new ReclaimMannequinCommandArgs
        {
            GithubOrg = GITHUB_ORG,
            Csv = "I_DO_NOT_EXIST_CSV_PATH",
        };
        await FluentActions
            .Invoking(async () => await _handler.Handle(args))
            .Should().ThrowAsync<OctoshiftCliException>();
    }

    [Fact]
    public async Task SingleReclaiming_Happy_Path()
    {
        string mannequinUserId = null;

        _mockReclaimService.Setup(x => x.ReclaimMannequin(MANNEQUIN_USER, mannequinUserId, TARGET_USER, GITHUB_ORG, false)).Returns(Task.FromResult(default(object)));

        var args = new ReclaimMannequinCommandArgs
        {
            GithubOrg = GITHUB_ORG,
            MannequinUser = MANNEQUIN_USER,
            MannequinId = mannequinUserId,
            TargetUser = TARGET_USER,
        };
        await _handler.Handle(args);

        _mockReclaimService.Verify(x => x.ReclaimMannequin(MANNEQUIN_USER, mannequinUserId, TARGET_USER, GITHUB_ORG, false), Times.Once);
    }

    [Fact]
    public async Task SingleReclaiming_WithIdSpecifiedHappy_Path()
    {
        var mannequinUserId = "monaid";

        _mockReclaimService.Setup(x => x.ReclaimMannequin(MANNEQUIN_USER, mannequinUserId, TARGET_USER, GITHUB_ORG, false)).Returns(Task.FromResult(default(object)));

        var args = new ReclaimMannequinCommandArgs
        {
            GithubOrg = GITHUB_ORG,
            MannequinUser = MANNEQUIN_USER,
            MannequinId = mannequinUserId,
            TargetUser = TARGET_USER,
        };
        await _handler.Handle(args);

        _mockReclaimService.Verify(x => x.ReclaimMannequin(MANNEQUIN_USER, mannequinUserId, TARGET_USER, GITHUB_ORG, false), Times.Once);
    }

    [Fact]
    public async Task CSVReclaiming_Happy_Path()
    {
        var args = new ReclaimMannequinCommandArgs
        {
            GithubOrg = GITHUB_ORG,
            Csv = "file.csv",
        };
        await _handler.Handle(args);

        _mockReclaimService.Verify(x => x.ReclaimMannequins(Array.Empty<string>(), GITHUB_ORG, false, false), Times.Once);
    }

    [Fact]
    public async Task CSV_CSV_TakesPrecedence()
    {
        var args = new ReclaimMannequinCommandArgs
        {
            GithubOrg = GITHUB_ORG,
            Csv = "file.csv",
            MannequinUser = MANNEQUIN_USER,
            TargetUser = TARGET_USER,
        };
        await _handler.Handle(args); // All parameters passed. CSV has precedence

        _mockReclaimService.Verify(x => x.ReclaimMannequins(Array.Empty<string>(), GITHUB_ORG, false, false), Times.Once);
    }

    [Fact]
    public async Task Skip_Invitation_Happy_Path()
    {
        // Arrange
        _confirmationService.Setup(x => x.AskForConfirmation(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var args = new ReclaimMannequinCommandArgs
        {
            GithubOrg = GITHUB_ORG,
            SkipInvitation = true,
            Csv = "file.csv",
        };

        // Act
        await _handler.Handle(args);

        // Assert
        _mockReclaimService.Verify(x => x.ReclaimMannequins(Array.Empty<string>(), GITHUB_ORG, false, true), Times.Once);
    }

    [Fact]
    public async Task Skip_Invitation_No_Confirmation_With_NoPrompt_Arg()
    {
        // Arrange
        var args = new ReclaimMannequinCommandArgs
        {
            GithubOrg = GITHUB_ORG,
            SkipInvitation = true,
            Csv = "file.csv",
            NoPrompt = true
        };

        // Act
        await _handler.Handle(args);

        // Assert
        _mockReclaimService.Verify(x => x.ReclaimMannequins(Array.Empty<string>(), GITHUB_ORG, false, true), Times.Once);
        _confirmationService.Verify(x => x.AskForConfirmation(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
