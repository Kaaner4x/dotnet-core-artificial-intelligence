# DotnetCoreAI - Proje 05: Google Gemini ile Ses Transkripsiyonu (Audio Transcription)

Bu proje, **.NET 8** platformunda **Google Gemini REST API** (`gemini-3.6-flash`) kullanılarak geliştirilmiş konsol tabanlı modern bir ses işleme ve konuşmayı metne dönüştürme (Speech-to-Text / Audio Transcription) uygulamasıdır.

OpenAI Whisper'ın Google tarafındaki en güncel çok modlu (multimodal) muadili olan Gemini Flash modeli kullanılarak; MP3, WAV gibi ses dosyaları doğrudan Base64 formatında Gemini'ye iletilir ve yüksek doğrulukla metne dökülür.

---

## 🌟 Öne Çıkan Özellikler

* **Google Gemini 3.6 Flash:** Düşük gecikme süresi (low latency) ve yüksek transkripsiyon doğruluğu.
* **Ücretsiz Kullanım:** Google AI Studio üzerinden tamamen ücretsiz API anahtarı ile çalışır (kredi kartı veya ön ödeme gerektirmez).
* **Güvenli Yapılandırma (`.env`):** Hassas API anahtarınız kaynak kod içine yazılmaz; `.env` dosyasından dinamik olarak okunur ve `.gitignore` ile korunur.
* **Akıllı Dosya Çözümleme (`ResolveAudioFilePath`):** Komut satırı argümanı, `.env` ayarı veya proje dizinindeki ses dosyalarını otomatik tespit eder.
* **Otomatik Dosya Kaydı:** Transkript metnini konsolda renkli olarak göstermenin yanı sıra `[DosyaAdi]_transkript.txt` dosyasına UTF-8 olarak kaydeder.
* **Zengin Format Desteği:** MP3, WAV, OGG, M4A, FLAC, AAC, WEBM formatlarını otomatik tanır.

---

## 🚀 Kurulum ve Başlangıç

### 1. Google Gemini API Anahtarı Alın
1. [Google AI Studio](https://aistudio.google.com/) adresine gidin ve Google hesabınızla giriş yapın.
2. **"Get API key"** butonuna tıklayarak yeni bir API anahtarı oluşturun ve kopyalayın.

---

### 2. Ortam Değişkenlerini (.env) Yapılandırın
Proje klasöründeki `.env.example` dosyasını referans alarak bir `.env` dosyası oluşturun (veya mevcut `.env` dosyasını düzenleyin):

```env
# Google Gemini API Key
GEMINI_API_KEY=buraya_aldiginiz_api_anahtarini_yazin

# Kullanılacak Model (Varsayılan: gemini-3.6-flash)
GEMINI_MODEL=gemini-3.6-flash

# (Opsiyonel) İşlenecek ses dosyası yolu
AUDIO_FILE_PATH=Arctic Monkeys - Do I Wanna Know_  (Live).mp3
```

> [!NOTE]
> Projedeki `.gitignore` dosyası sayesinde `.env` dosyanız asla GitHub veya kaynak kontrol sistemine yüklenmez.

---

### 3. Projeyi Çalıştırın

Varsayılan ses dosyasıyla (`Arctic Monkeys - Do I Wanna Know_ (Live).mp3`) çalıştırmak için:
```bash
dotnet run --project DotnetCoreAI.Project05_AudioTranscriptWithGoogleGemini
```

Farklı bir ses dosyasını parametre olarak iletmek için:
```bash
dotnet run --project DotnetCoreAI.Project05_AudioTranscriptWithGoogleGemini -- "C:\Sesler\toplanti.mp3"
```

---

## 📖 Kodların Satır Satır ve Blok Blok Detaylı Açıklaması

Aşağıda `Program.cs` dosyasındaki tüm kod blokları ve görevleri sırasıyla açıklanmıştır:

---

### 1. Gerekli Kütüphaneler (Namespace Tanımlamaları)

```csharp
using System.Text;
using System.Text.Json;
using DotNetEnv;
```

* **`System.Text`**: Türkçe karakterler (ç, ğ, ı, ö, ş, ü) ve emojilerin konsolda ve kaydedilen metin dosyasında bozulmadan görüntülenmesi için UTF-8 kodlamasını (`Encoding.UTF8`) sağlar.
* **`System.Text.Json`**: Gemini REST API'ye gönderilecek JSON istek gövdesini oluşturmak (`JsonSerializer.Serialize`) ve dönen yanıtı ayrıştırmak (`JsonDocument.Parse`) için kullanılır.
* **`DotNetEnv`**: `.env` dosyasını okuyarak çevre değişkenlerini sisteme yükler.

---

### 2. Konsol Karakter Kodlaması ve Karşılama Başlığı

```csharp
Console.OutputEncoding = Encoding.UTF8;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("==================================================================");
Console.WriteLine(" DotnetCoreAI - Proje 05: Google Gemini ile Ses Transkripsiyonu  ");
Console.WriteLine("==================================================================\n");
Console.ResetColor();
```

* Konsolun UTF-8 çıktısı vermesini sağlar ve kullanıcı dostu renkli bir karşılama başlığı basar.

---

### 3. `.env` Dosyasının Esnek Taranması ve Yüklenmesi

```csharp
// .env dosyasını ara ve yükle (proje klasörü, çıktı dizini veya üst dizinler)
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
```

* Uygulama ister çözüm kökünden (`dotnet run --project ...`), ister proje klasöründen, ister `bin/Debug/net8.0` altından çalıştırılsın, `.env` dosyasını hatasız bulup yükler.

---

### 4. API Anahtarı Kontrolü ve Yedek Girdi Mekanizması

```csharp
var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Equals("your_gemini_api_key_here", StringComparison.OrdinalIgnoreCase))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Uyarı: '.env' dosyasında geçerli bir 'GEMINI_API_KEY' bulunamadı.");
    Console.Write("Lütfen Google Gemini API anahtarınızı giriniz: ");
    Console.ResetColor();

    apiKey = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Hata: API anahtarı girilmedi. Uygulama sonlandırılıyor.");
        Console.ResetColor();
        return;
    }
}
```

* `.env` dosyasında anahtar unutulmuşsa program çökmek yerine konsoldan kullanıcıya sorar. Boş geçilirse güvenli bir şekilde sonlanır.

---

### 5. Model ve Ses Dosyasının Dinamik Çözümlenmesi (`ResolveAudioFilePath`)

```csharp
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
```

* Öncelik sırasıyla komut satırı argümanı (`args[0]`), `.env` değişkeni veya varsayılan `Arctic Monkeys...` parçası kontrol edilir.
* `ResolveAudioFilePath` fonksiyonu dosyanın tam disk yolunu (`Path.GetFullPath`) doğrular.

---

### 6. Ses Dosyasının Okunması ve Base64'e Dönüştürülmesi

```csharp
byte[] audioBytes = await File.ReadAllBytesAsync(resolvedFilePath);
string base64Audio = Convert.ToBase64String(audioBytes);
```

* Gemini REST API, ses dosyalarını çok modlu `inlineData` parçası olarak **Base64** formatında kabul eder.
* Dosya asenkron olarak byte dizisine okunur ve Base64 metnine dönüştürülür.

---

### 7. Gemini Multimodal İstek Gövdesinin (Payload) Hazırlanması

```csharp
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
                    text = "Lütfen bu ses kaydını eksiksiz ve doğru bir şekilde transkribe et (metne dök). " +
                           "Müzik/şarkı ise sözlerini, konuşma ise konuşulanları tam olarak aktar. " +
                           "Ekstra giriş/kapanış cümlesi veya yorum yapmadan doğrudan ses kaydında geçen metni yaz."
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
```

* Gemini API'sine hem komut (prompt) metni hem de `inlineData` objesi (MIME tipi ve Base64 ses verisi) tek bir `contents` gövdesi içinde iletilir.
* `application/json` başlığıyla HTTP isteğine hazır hale getirilir.

---

### 8. İstek Gönderimi ve Zaman Aşımı Yönetimi

```csharp
using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(3)
};

var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
var response = await httpClient.PostAsync(endpoint, httpContent);
var responseContent = await response.Content.ReadAsStringAsync();
```

* Büyük ses dosyalarının yüklenmesi ve model tarafından işlenmesi için `Timeout` süresi **3 dakika** olarak belirlenmiştir.
* `generateContent` uç noktasına POST isteği gönderilir.

---

### 9. Başarılı Yanıtın Ayrıştırılması ve Dosyaya Kaydedilmesi

```csharp
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
        Console.WriteLine(transcript.Trim());

        // Transkripti aynı klasöre .txt dosyası olarak kaydet
        string outputTxtPath = Path.Combine(
            fileInfo.DirectoryName ?? AppDomain.CurrentDomain.BaseDirectory,
            $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}_transkript.txt"
        );

        await File.WriteAllTextAsync(outputTxtPath, transcript.Trim(), Encoding.UTF8);
    }
}
```

* Gemini'den dönen hiyerarşik JSON yapısından transkript metni okunur:
  `candidates[0] ➔ content ➔ parts[0] ➔ text`
* Elde edilen transkript yeşil renkte konsola yazdırılır ve otomatik olarak `[DosyaAdi]_transkript.txt` dosyasına kaydedilir.

---

### 10. Hata Yönetimi (Error Handling)

```csharp
else
{
    using var errorDoc = JsonDocument.Parse(responseContent);
    if (errorDoc.RootElement.TryGetProperty("error", out var errElement) &&
        errElement.TryGetProperty("message", out var msgElement))
    {
        Console.WriteLine($"Hata Mesajı: {msgElement.GetString()}");
    }
}
```

* Geçersiz API anahtarı, kota aşımı, desteklenmeyen model gibi durumlarda Google API'sinin döndürdüğü hata mesajı ayrıştırılarak ekrana basılır.
* `TaskCanceledException` (zaman aşımı) ve `HttpRequestException` (ağ kopması) gibi durumlar yakalanarak uygulamanın çökmesi engellenir.

---

## 🛠️ Desteklenen Ses Formatları

| Format | MIME Türü |
| :--- | :--- |
| `.mp3` | `audio/mp3` |
| `.wav` | `audio/wav` |
| `.ogg` | `audio/ogg` |
| `.m4a` | `audio/m4a` |
| `.flac`| `audio/flac`|
| `.aac` | `audio/aac` |
| `.webm`| `audio/webm`|
| `.wma` | `audio/x-ms-wma`|
