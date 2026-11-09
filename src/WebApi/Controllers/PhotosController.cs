using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using Shared.Domain;
using Shared.Infrastructure.Interfaces;

namespace WebApi.Controllers;

[ApiController]
public class PhotosController : Controller
{
    private readonly IPhotoRepository _photoStorage;

    public PhotosController(IPhotoRepository photoStorage)
    {
        _photoStorage = photoStorage;
    }

    [HttpPost("/photo")]
    [Produces("application/json")]
    public async Task<ActionResult<PhotoCreatedDto>> UploadPhoto(IFormFile? file)
    {
        var photoId = Guid.NewGuid().ToString();
        byte[] fileBytes = Array.Empty<byte>();

        if (file != null && file.Length > 0)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            fileBytes = ms.ToArray();
        }

        var photo = new Photo(photoId, "in_progress", fileBytes);

        if (!_photoStorage.PostPhoto(photo))
            return StatusCode(500, "Internal Server Error");
        
        _ = Task.Run(async () =>
        {
            await Task.Delay(10000);
            _photoStorage.UpdatePhotoStatus(photoId, "ready");
        });

        return Ok(new PhotoCreatedDto(photoId));
    }

    [HttpGet("/status/{photo_id}")]
    [Produces("application/json")]
    public ActionResult<PhotoStatusDto> GetStatus([FromRoute] string photo_id)
    {
        var photo = _photoStorage.GetPhotoById(photo_id);
        if (photo == null) return NotFound();

        return Ok(new PhotoStatusDto(photo.Status));
    }

    [HttpGet("/result/{photo_id}")]
    public ActionResult GetResult([FromRoute] string photo_id)
    {
        var photo = _photoStorage.GetPhotoById(photo_id);
        if (photo == null) return NotFound();

        if (photo.Status != "ready")
            return BadRequest(new { Error = "Photo is not ready yet" });

        return Ok(new { Message = "Here will be the processed photo bytes" });
    }
}