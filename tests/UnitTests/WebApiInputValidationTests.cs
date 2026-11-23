using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Domain;
using Shared.Infrastructure.Interfaces;
using WebApi.Controllers;
using WebApi.Models;

namespace UnitTests;

public class WebApiInputValidationTests
{
    [TestCase("ab", "password123")]
    [TestCase("user", "short")]
    [TestCase("", "password123")]
    public void CredentialsDto_RejectsValuesOutsideConfiguredLimits(string login, string password)
    {
        var credentials = new CredentialsDto(login, password);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            credentials,
            new ValidationContext(credentials),
            results,
            validateAllProperties: true);

        Assert.That(isValid, Is.False);
    }

    [Test]
    public async Task Create_RejectsFileWithUnsupportedSignature()
    {
        var fileStorage = new Mock<IFileStorage>();
        var controller = CreateController(fileStorage);
        var file = CreateFile([0x00, 0x01, 0x02]);

        var result = await controller.Create(file, "Negative", authorization: "Bearer session");

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        fileStorage.Verify(
            storage => storage.SaveAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Create_RejectsFileLargerThanTenMebibytes()
    {
        var controller = CreateController(new Mock<IFileStorage>());
        var file = CreateFile(new byte[10 * 1024 * 1024 + 1]);

        var result = await controller.Create(file, "Negative", authorization: "Bearer session");

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result.Result!).StatusCode, Is.EqualTo(StatusCodes.Status413PayloadTooLarge));
    }

    [Test]
    public async Task Create_RejectsUndefinedNumericFilter()
    {
        var controller = CreateController(new Mock<IFileStorage>());
        var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var file = CreateFile(pngHeader);

        var result = await controller.Create(file, "999", authorization: "Bearer session");

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    private static TasksController CreateController(Mock<IFileStorage> fileStorage)
    {
        var user = new User("test-user", Guid.NewGuid().ToString(), "hash", DateTime.UtcNow);
        var sessionRepository = new Mock<ISessionRepository>();
        sessionRepository
            .Setup(repository => repository.GetUserBySessionId("session"))
            .Returns(user);

        var controller = new TasksController(
            Mock.Of<ITaskRepository>(),
            fileStorage.Object,
            Mock.Of<MassTransit.IPublishEndpoint>(),
            sessionRepository.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private static FormFile CreateFile(byte[] contents)
    {
        var stream = new MemoryStream(contents);
        return new FormFile(stream, 0, contents.Length, "file", "upload.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }
}
