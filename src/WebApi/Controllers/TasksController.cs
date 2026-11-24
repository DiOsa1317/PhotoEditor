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
    private const string ShouldBeImageText = "Файл должен быть изображением JPEG, PNG или WebP";
    private const string FileTooLargeText = "Размер файла превышает 10 MiB";
    private const string BadFilterText = "Неверный фильтр";
    private const string BlurRadiusShouldBePositiveText = "blurRadius должен быть конечным числом не меньше 0";
    private const string BadTaskIdText = "Неверный id задачи";
    private const string TaskNotFoundText = "Задача не найдена";
    private const string ResultNotFound = "Результат ещё не готов";
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private const long MaxRequestSizeBytes = MaxFileSizeBytes + 1024 * 1024;

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
    [RequestSizeLimit(MaxRequestSizeBytes)]
    public async Task<ActionResult<TaskResponse>> Create(
        IFormFile file,
        [FromForm] string filter,
        [FromForm] double blurRadius = 0,
        [FromForm] string sessionId = "anonymous",
        [FromHeader(Name = "Authorization")] string? authorization = null)
    {
        // Попытка Bearer-авторизации через сессию
        string resolvedSessionId = sessionId;
        string userId = Guid.NewGuid().ToString();

        if (AuthenticationHeaderValue.TryParse(authorization, out var authHeader) &&
            string.Equals(authHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(authHeader.Parameter))
        {
            var user = _sessionRepository.GetUserBySessionId(authHeader.Parameter);
            if (user is not null)
            {
                resolvedSessionId = authHeader.Parameter;
                userId = user.Id;
            }
        }

        if (string.IsNullOrWhiteSpace(resolvedSessionId) || resolvedSessionId == "anonymous")
            resolvedSessionId = Guid.NewGuid().ToString();

        if (file is null || file.Length == 0)
            return BadRequest(new { error = NoFileText });
        if (file.Length > MaxFileSizeBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                error = FileTooLargeText,
                maxFileSizeBytes = MaxFileSizeBytes
            });

        var contentType = await DetectImageContentTypeAsync(file, HttpContext.RequestAborted);
        if (contentType is null)
            return BadRequest(new { error = ShouldBeImageText });
        if (!Enum.TryParse<FilterType>(filter, true, out var filterType) ||
            !Enum.IsDefined(filterType))
            return BadRequest(new
            {
                error = BadFilterText,
                allowedValues = Enum.GetNames<FilterType>()
            });
        if (!double.IsFinite(blurRadius) || blurRadius < 0)
            return BadRequest(new { error = BlurRadiusShouldBePositiveText });

        await using var stream = file.OpenReadStream();
        var sourceFileId = await _fileStorage.SaveAsync(stream, file.FileName, contentType);

        var task = new ImageTask
        {
            UserId = userId,
            SessionId = resolvedSessionId,
            Filter = filterType,
            BlurRadius = blurRadius,
            SourceFileId = sourceFileId
        };
        await _taskRepository.InsertAsync(task);
        await _publishEndpoint.Publish(new TaskCreated(Guid.Parse(task.Id), Guid.Parse(task.SessionId), task.Filter,
            task.BlurRadius, task.SourceFileId));

        return Ok(new TaskResponse(task.Id, task.SessionId, task.Status, task.SourceFileId));
    }

    private static async Task<string?> DetectImageContentTypeAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var bytesReadTotal = 0;

        while (bytesReadTotal < header.Length)
        {
            var bytesRead = await stream.ReadAsync(
                header.AsMemory(bytesReadTotal),
                cancellationToken);
            if (bytesRead == 0)
                break;
            bytesReadTotal += bytesRead;
        }

        if (bytesReadTotal >= 3 &&
            header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return "image/jpeg";

        if (bytesReadTotal >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            return "image/png";

        if (bytesReadTotal >= 12 &&
            header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' &&
            header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P')
            return "image/webp";

        return null;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskResponse>> Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound(new { error = BadTaskIdText });
        var task = await _taskRepository.FindByIdAsync(id);
        if (task is null)
            return NotFound(new { error = TaskNotFoundText });
        return Ok(new TaskResponse(task.Id, task.SessionId, task.Status, task.SourceFileId));
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

    public class TaskResponse(string id, string sessionId, ImageTaskStatus status, string sourceFileId)
    {
        public string Id { get; } = id;
        public string SessionId { get; } = sessionId;
        public ImageTaskStatus Status { get; } = status;
        public string SourceFileId { get; } = sourceFileId;
    }
}
