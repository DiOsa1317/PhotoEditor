using System.Net.Http.Headers;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using Shared.Domain;
using Shared.Infrastructure.Interfaces;

namespace WebApi.Controllers;

[Route("tasks")]
[ApiController]
public class TasksController : ControllerBase
{
    private readonly ITaskRepository _taskRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ISessionRepository _sessionRepository;

    private const string NoFileText = "Файл не передан";
    private const string ShouldBeImageText = "Файл должен быть изображением";
    private const string BadFilterText = "Неверный фильтр";
    private const string BlurRadiusShouldBePositiveText = "blurRadius не может быть отрицательным";
    private const string InvalidSessionText = "Отсутствует или недействительна сессия";
    private const string BadTaskIdText = "Неверный id задачи";
    private const string TaskNotFoundText = "Задача не найдена";
    private const string ResultNotFound = "Результат ещё не готов";
    

    public TasksController(
        ITaskRepository taskRepository,
        IFileStorage fileStorage,
        IPublishEndpoint publishEndpoint,
        ISessionRepository sessionRepository)
    {
        _taskRepository = taskRepository;
        _fileStorage = fileStorage;
        _publishEndpoint = publishEndpoint;
        _sessionRepository = sessionRepository;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<TaskResponse>> Create(
        IFormFile file,
        [FromForm] string filter,
        [FromForm] double blurRadius = 0,
        [FromHeader(Name = "Authorization")] string? authorization = null
        )
    {
        if (!AuthenticationHeaderValue.TryParse(authorization, out var authHeader) ||
            !string.Equals(authHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(authHeader.Parameter))
            return Unauthorized(new { error = InvalidSessionText });

        var user = _sessionRepository.GetUserBySessionId(authHeader.Parameter);
        if (user is null)
            return Unauthorized(new { error = InvalidSessionText });

        if (file is  null || file.Length == 0)
            return BadRequest(new {error = NoFileText});
        if (!file.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? true)
            return BadRequest(new  {error = ShouldBeImageText});
        if (!Enum.TryParse<FilterType>(filter, true, out var filterType))
            return BadRequest(new  {error = BadFilterText});
        if (blurRadius < 0)
            return BadRequest(new
            {
                error = BlurRadiusShouldBePositiveText
            });
        await using var stream = file.OpenReadStream();
        var sourceFileId = await _fileStorage.SaveAsync(stream, file.FileName, file.ContentType);

        var task = new ImageTask
        {
            UserId = user.Id,
            Filter = filterType,
            BlurRadius = blurRadius,
            SourceFileId = sourceFileId
        };
        await _taskRepository.InsertAsync(task);
        await _publishEndpoint.Publish(new TaskCreated(Guid.Parse(task.Id), Guid.Parse(task.UserId), task.Filter,
            task.BlurRadius, task.SourceFileId));

        return Ok(new TaskResponse(task.Id, task.UserId, task.Status, task.SourceFileId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskResponse>> Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound(new {error = BadTaskIdText});
        var task = await _taskRepository.FindByIdAsync(id);
        if (task is null)
            return NotFound(new {error = TaskNotFoundText});
        return Ok(new TaskResponse(task.Id, task.UserId, task.Status, task.SourceFileId));
    }

    [HttpGet("{id}/result")]
    public async Task<IActionResult> GetResult(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound(new { error = BadTaskIdText });
        var task = await _taskRepository.FindByIdAsync(id);
        if (task is null)
            return NotFound(new { error = TaskNotFoundText });
        if (task.Status != ImageTaskStatus.Completed)
            return Conflict(new { error = ResultNotFound });
        var resultStream = await _fileStorage.GetAsync(task.ResultFileId);
        return File(resultStream, "image/png", $"{task.Id}.png");
    }

    public class TaskResponse(string id, string userId, ImageTaskStatus status, string sourceFileId)
    {
        public string Id { get; } = id;
        public string UserId { get; } = userId;
        public ImageTaskStatus Status { get; } = status;
        public string SourceFileId { get; } = sourceFileId;
    }
}
