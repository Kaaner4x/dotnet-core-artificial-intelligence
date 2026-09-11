using System.Text;
using System.Text.Json;
using DotNetEnv;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string currentDir = Directory.GetCurrentDirectory();

        string[] possibleEnvPaths = new[]
        {
            Path.Combine(currentDir, ".env"),
            Path.Combine(baseDir, ".env"),
            Path.Combine(currentDir, "DotnetCoreAI.Project05_AudioTranscriptWithGoogleGemini", ".env")
        };

        bool envLoaded = false;
        foreach (var path in possibleEnvPaths)
        {
            if (File.Exists(path))
            {
                Env.Load(path);
                envLoaded = true;
                break;
            }
        }

        if (!envLoaded)
        {
            Env.TraversePath().Load();
        }

        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("your_gemini_api_key_here", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: No valid 'GEMINI_API_KEY' found in '.env' file.");
            Console.Write("Please enter your Google Gemini API key: ");
            Console.ResetColor();

            apiKey = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: No API key provided. Terminating application.");
                Console.ResetColor();
                return;
            }
        }

        var model = Environment.GetEnvironmentVariable("GEMINI_MODEL");
        if (string.IsNullOrWhiteSpace(model))
        {
            model = "gemini-3.6-flash";
        }

        string? audioFilePath = null;

        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            audioFilePath = args[0];
        }
        else
        {
            audioFilePath = Environment.GetEnvironmentVariable("AUDIO_FILE_PATH");
        }

        if (string.IsNullOrWhiteSpace(audioFilePath))
        {
            audioFilePath = "Arctic Monkeys - Do I Wanna Know_  (Live).mp3";
        }

        string? resolvedFilePath = ResolveAudioFilePath(audioFilePath);

        while (resolvedFilePath == null || !File.Exists(resolvedFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nWarning: File '{audioFilePath}' not found.");
            Console.Write("Please enter the full path of the audio file to transcribe: ");
            Console.ResetColor();

            audioFilePath = Console.ReadLine()?.Trim('\"', ' ');

            if (string.IsNullOrWhiteSpace(audioFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: No file path provided. Terminating application.");
                Console.ResetColor();
                return;
            }

            resolvedFilePath = ResolveAudioFilePath(audioFilePath);
        }

        var fileInfo = new FileInfo(resolvedFilePath);
        var mimeType = GetAudioMimeType(resolvedFilePath);
        double fileSizeMb = fileInfo.Length / (1024.0 * 1024.0);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[File Information]");
        Console.WriteLine($" -> File Name : {fileInfo.Name}");
        Console.WriteLine($" -> File Size : {fileSizeMb:F2} MB");
        Console.WriteLine($" -> MIME Type : {mimeType}");
        Console.WriteLine($" -> AI Model  : {model}");
        Console.ResetColor();

        try
        {
            byte[] audioBytes = await File.ReadAllBytesAsync(resolvedFilePath);
            string base64Audio = Convert.ToBase64String(audioBytes);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                text = "Please transcribe this audio recording completely and accurately. " +
                                       "If it is a song, transcribe the lyrics. If it is speech, transcribe the spoken dialogue. " +
                                       "Return only the transcribed text without any introductory or concluding comments."
                            },
                            new
                            {
                                inlineData = new
                                {
                                    mimeType = mimeType,
                                    data = base64Audio
                                }
                            }
                        }
                    }
                }
            };

            var jsonPayload = JsonSerializer.Serialize(requestBody);
            using var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(3)
            };

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            var response = await httpClient.PostAsync(endpoint, httpContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                string? transcript = null;

                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        transcript = parts[0].GetProperty("text").GetString();
                    }
                }

                if (!string.IsNullOrWhiteSpace(transcript))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nTRANSCRIPT\n");
                    Console.ResetColor();
                    Console.WriteLine(transcript.Trim());
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.ResetColor();

                    string outputTxtPath = Path.Combine(
                        fileInfo.DirectoryName ?? AppDomain.CurrentDomain.BaseDirectory,
                        $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}_transcript.txt"
                    );

                    await File.WriteAllTextAsync(outputTxtPath, transcript.Trim(), Encoding.UTF8);
                    Console.WriteLine("\n");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Gemini returned a response, but the text content is empty or may have been filtered.");
                    Console.WriteLine($"Raw Response: {responseContent}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[Error] Gemini API request failed. Status Code: {(int)response.StatusCode} ({response.StatusCode})");

                try
                {
                    using var errorDoc = JsonDocument.Parse(responseContent);
                    if (errorDoc.RootElement.TryGetProperty("error", out var errElement) &&
                        errElement.TryGetProperty("message", out var msgElement))
                    {
                        Console.WriteLine($"Error Message: {msgElement.GetString()}");
                    }
                    else
                    {
                        Console.WriteLine($"Response: {responseContent}");
                    }
                }
                catch
                {
                    Console.WriteLine($"Response: {responseContent}");
                }

                Console.ResetColor();
            }
        }
        catch (TaskCanceledException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[Timeout Error] The request timed out. Please check your internet connection or file size.");
            Console.ResetColor();
        }
        catch (HttpRequestException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Network Error] Could not connect to Gemini servers: {ex.Message}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Unexpected Error] {ex.Message}");
            Console.ResetColor();
        }
    }

    private static string? ResolveAudioFilePath(string path)
    {
        if (File.Exists(path))
            return Path.GetFullPath(path);

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string combinedWithBase = Path.Combine(baseDir, path);
        if (File.Exists(combinedWithBase))
            return Path.GetFullPath(combinedWithBase);

        string fileName = Path.GetFileName(path);
        string currentDir = Directory.GetCurrentDirectory();
        string combinedCurrent = Path.Combine(currentDir, fileName);
        if (File.Exists(combinedCurrent))
            return Path.GetFullPath(combinedCurrent);

        return null;
    }

    private static string GetAudioMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".mp3" => "audio/mp3",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".m4a" => "audio/m4a",
            ".flac" => "audio/flac",
            ".aac" => "audio/aac",
            ".webm" => "audio/webm",
            ".wma" => "audio/x-ms-wma",
            _ => "audio/mp3"
        };
    }
}
