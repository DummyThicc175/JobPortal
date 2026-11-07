using System.Text;

namespace JobPortal.Services;

public sealed class PrivateFileStorage
{
    public const long MaxPdfBytes = 10 * 1024 * 1024;
    public const long MaxAvatarBytes = 2 * 1024 * 1024;
    private readonly string _pdfRoot;
    private readonly string _avatarRoot;
    private readonly IWebHostEnvironment _environment;

    public PrivateFileStorage(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _pdfRoot = Path.GetFullPath(configuration["Storage:RootPath"] ?? Path.Combine(environment.ContentRootPath, "Backend", "Storage", "cvs"));
        _avatarRoot = configuration["Storage:AvatarRootPath"] ?? Path.Combine(environment.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(_pdfRoot);
        Directory.CreateDirectory(_avatarRoot);
    }

    public async Task<string> SavePdfAsync(IFormFile file, CancellationToken token)
    {
        if (file.Length is <= 0 or > MaxPdfBytes || !string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("CV phải là file PDF, dung lượng tối đa 10 MB.");
        await using var input = file.OpenReadStream();
        var header = new byte[5];
        if (await input.ReadAtLeastAsync(header, 5, false, token) != 5 || Encoding.ASCII.GetString(header) != "%PDF-")
            throw new InvalidDataException("Nội dung file không phải PDF hợp lệ.");
        var name = Guid.NewGuid().ToString("N") + ".pdf";
        var path = Path.Combine(_pdfRoot, name);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await output.WriteAsync(header, token);
            await input.CopyToAsync(output, token);
            return name;
        }
        catch { File.Delete(path); throw; }
    }

    public async Task<string> SaveAvatarAsync(IFormFile file, CancellationToken token)
    {
        if (file.Length is <= 0 or > MaxAvatarBytes)
            throw new InvalidDataException("Ảnh đại diện phải nhỏ hơn 2 MB.");
        await using var input = file.OpenReadStream();
        var header = new byte[8];
        var read = await input.ReadAtLeastAsync(header, 8, false, token);
        var png = read == 8 && header.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = read >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if ((!png || extension != ".png") && (!jpeg || extension is not (".jpg" or ".jpeg")))
            throw new InvalidDataException("Chỉ chấp nhận ảnh PNG hoặc JPEG hợp lệ.");
        var name = Guid.NewGuid().ToString("N") + (png ? ".png" : ".jpg");
        var path = Path.Combine(_avatarRoot, name);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await output.WriteAsync(header.AsMemory(0, read), token);
            await input.CopyToAsync(output, token);
            return "/uploads/avatars/" + name;
        }
        catch { File.Delete(path); throw; }
    }

    public string? GetPdfPath(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name) || Path.GetExtension(name).ToLowerInvariant() != ".pdf") return null;
        var path = Path.Combine(_pdfRoot, name);
        if (File.Exists(path)) return path;
        // Old CVs stay behind authorization while files are relocated to private storage.
        var legacy = Path.Combine(_environment.WebRootPath, "uploads", "cvs", name);
        return File.Exists(legacy) ? legacy : null;
    }

    public void DeleteNewPdf(string? name)
    {
        if (!string.IsNullOrEmpty(name) && name == Path.GetFileName(name)) File.Delete(Path.Combine(_pdfRoot, name));
    }

    public void DeleteAvatar(string? url)
    {
        if (url?.StartsWith("/uploads/avatars/", StringComparison.Ordinal) == true && Path.GetFileName(url) == url["/uploads/avatars/".Length..])
            File.Delete(Path.Combine(_avatarRoot, Path.GetFileName(url)));
    }

    public void ImportLegacyFiles()
    {
        var legacy = Path.Combine(_environment.WebRootPath, "uploads", "cvs");
        if (!Directory.Exists(legacy)) return;
        foreach (var file in Directory.EnumerateFiles(legacy, "*.pdf", SearchOption.TopDirectoryOnly))
        {
            var target = Path.Combine(_pdfRoot, Path.GetFileName(file));
            if (!File.Exists(target)) File.Copy(file, target);
            // Compare before deleting, including when a previous import was interrupted.
            if (new FileInfo(file).Length == new FileInfo(target).Length &&
                System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file)).AsSpan()
                    .SequenceEqual(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(target)))) File.Delete(file);
        }
    }
}
