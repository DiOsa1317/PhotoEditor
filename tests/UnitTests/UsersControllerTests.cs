using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Domain;
using Shared.Infrastructure.Interfaces;
using WebApi.Controllers;
using WebApi.Models;

namespace UnitTests;

public class UsersControllerTests
{
    private Mock<IUserRepository> _userRepository = null!;
    private Mock<ISessionRepository> _sessionRepository = null!;
    private UsersController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = new Mock<IUserRepository>();
        _sessionRepository = new Mock<ISessionRepository>();
        _controller = new UsersController(_userRepository.Object, _sessionRepository.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _controller.Dispose();
    }

    [Test]
    public void GetUserById_ReturnsUserDto_WhenUserExists()
    {
        var user = CreateUser();
        _userRepository.Setup(repository => repository.GetUserById(user.Id)).Returns(user);

        var result = _controller.GetUserBYId(user.Id);

        var response = result.Result as OkObjectResult;
        var userDto = response?.Value as UserDto;
        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(userDto?.Id, Is.EqualTo(user.Id));
            Assert.That(userDto?.Login, Is.EqualTo(user.Login));
        });
    }

    [Test]
    public void GetUserById_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _userRepository.Setup(repository => repository.GetUserById("missing")).Returns((User?)null);

        var result = _controller.GetUserBYId("missing");

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public void RegisterUser_ReturnsCreated_AndStoresUser()
    {
        User? storedUser = null;
        _userRepository
            .Setup(repository => repository.PostUser(It.IsAny<User>()))
            .Callback<User>(user => storedUser = user)
            .Returns(true);

        var result = _controller.RegisterUser(new CredentialsDto("new-user", "password123"));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<CreatedResult>());
            Assert.That(storedUser, Is.Not.Null);
            Assert.That(storedUser!.Login, Is.EqualTo("new-user"));
            Assert.That(Guid.TryParse(storedUser.Id, out _), Is.True);
            Assert.That(storedUser.PasswordHash, Is.EqualTo("hash"));
        });
        _userRepository.Verify(repository => repository.PostUser(It.IsAny<User>()), Times.Once);
    }

    [Test]
    public void RegisterUser_ReturnsConflict_WhenRepositoryRejectsDuplicate()
    {
        _userRepository.Setup(repository => repository.PostUser(It.IsAny<User>())).Returns(false);

        var result = _controller.RegisterUser(new CredentialsDto("existing", "password123"));

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        _userRepository.Verify(repository => repository.PostUser(It.IsAny<User>()), Times.Once);
    }

    [Test]
    public void LoginUser_ReturnsBadRequest_WhenLoginDoesNotExist()
    {
        _userRepository.Setup(repository => repository.GetUserByLogin("missing")).Returns((User?)null);

        var result = _controller.LoginUser(new CredentialsDto("missing", "password123"));

        Assert.That(result.Result, Is.InstanceOf<BadRequestResult>());
        _sessionRepository.Verify(
            repository => repository.PostSession(It.IsAny<string>(), It.IsAny<User>()),
            Times.Never);
    }

    [Test]
    public void LoginUser_ReturnsSession_AndStoresIt_WhenUserExists()
    {
        var user = CreateUser();
        string? storedSessionId = null;
        _userRepository.Setup(repository => repository.GetUserByLogin(user.Login)).Returns(user);
        _sessionRepository
            .Setup(repository => repository.PostSession(It.IsAny<string>(), user))
            .Callback<string, User>((sessionId, _) => storedSessionId = sessionId)
            .Returns(true);

        var result = _controller.LoginUser(new CredentialsDto(user.Login, "password123"));

        var response = result.Result as OkObjectResult;
        var session = response?.Value as SessionDto;
        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(session, Is.Not.Null);
            Assert.That(Guid.TryParse(session!.SessionId, out _), Is.True);
            Assert.That(storedSessionId, Is.EqualTo(session.SessionId));
        });
        _sessionRepository.Verify(repository => repository.PostSession(session!.SessionId, user), Times.Once);
    }

    [Test]
    public void LoginUser_ReturnsBadRequest_WhenSessionCannotBeStored()
    {
        var user = CreateUser();
        _userRepository.Setup(repository => repository.GetUserByLogin(user.Login)).Returns(user);
        _sessionRepository
            .Setup(repository => repository.PostSession(It.IsAny<string>(), user))
            .Returns(false);

        var result = _controller.LoginUser(new CredentialsDto(user.Login, "password123"));

        Assert.That(result.Result, Is.InstanceOf<BadRequestResult>());
    }

    private static User CreateUser() =>
        new("existing-user", Guid.NewGuid().ToString(), "hash", DateTime.UtcNow);
}
