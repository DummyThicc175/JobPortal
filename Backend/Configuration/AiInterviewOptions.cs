namespace JobPortal.Configuration;

public sealed class AiInterviewOptions
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434/";
    public string Model { get; set; } = "qwen3:1.7b";
    public string SpeechModelPath { get; set; } = "Backend/Storage/AI/speech/phowhisper-small-source";
    public string SpeechPythonPath { get; set; } = "Backend/Storage/AI/tools/python/python.exe";
    public int TimeoutSeconds { get; set; } = 180;
    public int SpeechTimeoutSeconds { get; set; } = 600;
}
