using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/content/media")]
[Authorize]
public class ContentMediaController(IConfiguration configuration, IWebHostEnvironment environment) : ControllerBase
{
    private const int MaximumBytes = 2 * 1024 * 1024;
    private string MediaDirectory => configuration["ContentMedia:Directory"]
        ?? Path.Combine(environment.ContentRootPath, "content-media");

    [HttpPost]
    [RequirePermission("content.create")]
    [RequestSizeLimit(MaximumBytes + 8192)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (!Uri.TryCreate(configuration["App:PublicApiUrl"], UriKind.Absolute, out var publicApi) ||
            publicApi.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(publicApi.UserInfo))
            return BadRequest(new { error = "Configure a URL pública HTTPS da API para habilitar o envio de artes." });

        if (file.Length is < 4 or > MaximumBytes)
            return BadRequest(new { error = "A arte deve ser um JPEG de até 2 MB." });

        await using var source = file.OpenReadStream();
        using var buffer = new MemoryStream((int)file.Length);
        await source.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();
        if (bytes[0] != 0xff || bytes[1] != 0xd8 || bytes[2] != 0xff ||
            bytes[^2] != 0xff || bytes[^1] != 0xd9)
            return BadRequest(new { error = "A arte deve estar no formato JPEG." });

        var id = Guid.NewGuid();
        Directory.CreateDirectory(MediaDirectory);
        var path = MediaPath(id);
        var temporaryPath = path + ".tmp";
        try
        {
            await System.IO.File.WriteAllBytesAsync(temporaryPath, bytes, ct);
            System.IO.File.Move(temporaryPath, path);
        }
        finally
        {
            System.IO.File.Delete(temporaryPath);
        }

        var url = new Uri(publicApi.AbsoluteUri.TrimEnd('/') + "/" + $"api/content/media/{id:D}.jpg");
        return Ok(new { success = true, data = new { url = url.AbsoluteUri } });
    }

    [HttpGet("{id:guid}.jpg")]
    [AllowAnonymous]
    public IActionResult Get(Guid id)
    {
        var path = MediaPath(id);
        if (!System.IO.File.Exists(path)) return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "public, max-age=86400";
        return PhysicalFile(path, "image/jpeg");
    }

    private string MediaPath(Guid id) => Path.GetFullPath(Path.Combine(MediaDirectory, $"{id:D}.jpg"));
}
