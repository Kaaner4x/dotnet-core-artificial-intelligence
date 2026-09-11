# DotnetCoreAI - Proje 04: Google Gemini Chatbot

Bu proje, **.NET 8** platformunda **Google Gemini REST API** (`gemini-flash-latest`) kullanılarak geliştirilmiş konsol tabanlı bir yapay zeka sohbet botudur (Chatbot). Sürekli etkileşimli sohbet desteği, `.env` dosyasıyla güvenli API anahtarı yönetimi ve kapsamlı hata yakalama mekanizmalarına sahiptir.

---

## 🚀 Kurulum ve Başlangıç

1. **Gemini API Anahtarı Alın:**  
   [Google AI Studio](https://aistudio.google.com/) üzerinden ücretsiz bir API anahtarı temin edin.

2. **Ortam Değişkenini (.env) Yapılandırın:**  
   Proje klasöründeki `.env.example` dosyasını referans alarak bir `.env` dosyası oluşturun ve anahtarınızı ekleyin:
   ```env
   GEMINI_API_KEY=buraya_aldiginiz_api_anahtarini_yazin
   ```
   *(Not: `.gitignore` dosyası sayesinde `.env` dosyanız asla GitHub'a yüklenmez.)*

3. **Projeyi Çalıştırın:**
   ```bash
   dotnet run
   ```

---

## 📖 Kodların Satır Satır ve Blok Blok Detaylı Açıklaması

Aşağıda `Program.cs` dosyasındaki tüm kod blokları ve görevleri sırasıyla açıklanmıştır:

### 1. Gerekli Kütüphaneler (Namespace Tanımlamaları)

```csharp
using System.Text;
using System.Text.Json;
using DotNetEnv;
```
- **`System.Text`**: Türkçe karakterler ve özel simgelerin HTTP isteklerinde ve konsolda bozulmadan iletilmesi için UTF-8 karakter kodlamasını (`Encoding.UTF8`) sağlar.
- **`System.Text.Json`**: .NET'in yerleşik ve yüksek performanslı JSON kütüphanesidir. Gemini'ye gönderilecek veriyi JSON formatına dönüştürmek (serialize) ve gelen yanıtı okumak (deserialize) için kullanılır.
- **`DotNetEnv`**: Proje dizinindeki `.env` dosyasını okuyarak içindeki değerleri sistem ortam değişkeni (`Environment`) seviyesine yükler.

---

### 2. Konsol Karakter Kodlaması Ayarı

```csharp
Console.OutputEncoding = Encoding.UTF8;
```
- Konsol ekranının Türkçe karakterleri (ş, ğ, ü, ç, ö, ı vb.) ve emojileri doğru gösterebilmesi için çıktı kodlamasını UTF-8 olarak ayarlar.

---

### 3. `.env` Dosyasının Otomatik Aranması ve Yüklenmesi

```csharp
// .env dosyasını otomatik ara ve yükle
Env.TraversePath().Load();

var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
```
- **`Env.TraversePath().Load()`**: Uygulama çalıştığı klasörden başlayarak üst dizinlere doğru `.env` dosyasını otomatik olarak arar ve bulduğunda içeriğini ortam değişkenlerine aktarır.
- **`Environment.GetEnvironmentVariable("GEMINI_API_KEY")`**: İşletim sisteminden veya `.env` dosyasından anahtarı çeker. Bu sayede API anahtarınız kaynak kod içine yazılmaz (hardcoded olmaz) ve GitHub'da ifşa olmaz.

---

### 4. API Anahtarı Doğrulama ve Yedek Mekanizma (Fallback)

```csharp
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Warning: 'GEMINI_API_KEY' environment variable not found.");
    Console.Write("Please enter your Gemini API key: ");
    Console.ResetColor();

    apiKey = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("No API key provided. Terminating application.");
        Console.ResetColor();
        return;
    }
    Console.Clear();
}
```
- Eğer kullanıcı `.env` dosyasını hazırlamamışsa veya anahtar boş bırakılmışsa program hata fırlatıp çökmez.
- Konsolda kullanıcıyı sarı renkte uyararak API anahtarını elle girmesini ister.
- Eğer kullanıcı hiçbir şey yazmadan Enter'a basarsa, program temiz bir hata mesajı vererek sonlanır (`return`).

---

### 5. Başlangıç Bilgileri ve Sürekli Sohbet Döngüsü

```csharp
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=== Gemini AI Chatbot Started ===");
Console.WriteLine("(Type 'exit' or 'quit' to end the conversation)\n");
Console.ResetColor();

using var httpClient = new HttpClient();
var model = "gemini-flash-latest";

while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("You: ");
    Console.ResetColor();

    var prompt = Console.ReadLine();
```
- **`using var httpClient = new HttpClient()`**: HTTP istekleri atmamızı sağlayan nesneyi tanımlar. `using` ifadesi sayesinde program bittiğinde kaynaklar otomatik temizlenir.
- **`model = "gemini-flash-latest"`**: Hızlı yanıt üreten ve güncel kalan `gemini-flash-latest` modeli seçilmiştir.
- **`while (true)`**: Tek soru-cevap yerine sohbetin kesintisiz devam etmesini sağlar.
- **`Console.ForegroundColor`**: Konsol çıktılarını renklendirir (Kullanıcı için Yeşil, Gemini için Mavi, Sistem için Camgöbeği, Hatalar için Kırmızı).

---

### 6. Boş Girdi ve Çıkış Kontrolü

```csharp
    if (string.IsNullOrWhiteSpace(prompt))
        continue;

    if (prompt.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        prompt.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Goodbye!");
        break;
    }
```
- **`continue`**: Kullanıcı yanlışlıkla boş Enter'a bastığında boş istek atılmasını engeller ve tekrar girdi bekler.
- **`break`**: Kullanıcı `exit` veya `quit` yazdığında döngüyü sonlandırarak uygulamayı kapatır.

---

### 7. Gemini API İstek Gövdesinin (Payload) Hazırlanması

```csharp
    var requestBody = new
    {
        contents = new[]
        {
            new
            {
                parts = new[]
                {
                    new { text = prompt }
                }
            }
        }
    };

    var json = JsonSerializer.Serialize(requestBody);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
```
- Gemini REST API'nin beklediği hiyerarşik JSON yapısı anonim bir C# nesnesi olarak kurulur:
  ```json
  {
    "contents": [
      {
        "parts": [
          { "text": "Kullanıcının sorusu" }
        ]
      }
    ]
  }
  ```
- **`JsonSerializer.Serialize`**: Nesneyi standart JSON metnine çevirir.
- **`StringContent`**: Hazırlanan JSON metnini `UTF-8` formatında ve `application/json` başlığıyla HTTP istek gövdesine dönüştürür.

---

### 8. HTTP POST İsteği ve Gelen Yanıtın Ayrıştırılması

```csharp
    try
    {
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        var response = await httpClient.PostAsync(endpoint, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var result = JsonSerializer.Deserialize<JsonElement>(responseString);
            var answer = result.GetProperty("candidates")[0]
                               .GetProperty("content")
                               .GetProperty("parts")[0]
                               .GetProperty("text")
                               .GetString();

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"\nGemini: {answer?.Trim()}\n");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn error occurred (Status: {response.StatusCode}):");
            Console.WriteLine(responseString + "\n");
            Console.ResetColor();
        }
    }
```
- **`httpClient.PostAsync(...)`**: Google Gemini API'sine asenkron bir POST isteği gönderir.
- **`response.IsSuccessStatusCode`**: Sunucudan `200 OK` yanıtı gelip gelmediğini kontrol eder.
- **`JsonElement ile Okuma`**: Gelen JSON yanıtının içinden şu yolu takip ederek cevabı alır:
  `candidates` (0. eleman) ➔ `content` ➔ `parts` (0. eleman) ➔ `text`.
- İstek başarısız olursa HTTP durum kodunu ve hata detayını ekrana yazdırır.

---

### 9. Ağ ve Bağlantı Hatalarını Yakalama

```csharp
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\nConnection error: {ex.Message}\n");
        Console.ResetColor();
    }
```
- İnternet kopması, DNS sorunları veya zaman aşımı gibi beklenmeyen sistem hatalarında uygulamanın çökmesini önler ve kullanıcıya bilgi verir.
