namespace JobPortal.Configuration;

// Chỉ file appsettings.json tại content root cung cấp khóa/model Gemini.
public sealed class CauHinhGeminiCucBo : IDisposable
{
    private readonly IConfigurationRoot cauHinh;
    public CauHinhGeminiCucBo(string thuMucGoc) => cauHinh = new ConfigurationBuilder()
        .SetBasePath(thuMucGoc).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();
    public string? ApiKey => cauHinh["Gemini:ApiKey"]?.Trim();
    public string Model => cauHinh["Gemini:Model"]?.Trim() is { Length: > 0 } model ? model : "gemini-flash-latest";
    public void Dispose() => (cauHinh as IDisposable)?.Dispose();
}
