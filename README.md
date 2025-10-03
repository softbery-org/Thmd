# Thmd

Thmd to biblioteka napisana w języku C# przeznaczona do obsługi multimediów, zarządzania  oraz napisami w aplikacjach WPF. Projekt jest zgodny z .NET Framework 4.8.1 i wykorzystuje nowoczesne funkcje konfiguracji języka C# 12.0.

## Funkcjonalności

### 1. **Konfiguracja**
- Klasa `Config` umożliwia zarządzanie ustawieniami aplikacji, w tym:
  - Ładowanie i zapisywanie konfiguracji z/do pliku JSON.
  - Obsługa ustawień takich jak połączenie z bazą danych, logowanie, ścieżki do bibliotek, klucze API i inne.

### 2. **Obsługa multimediów**
- Klasa `FileMediaStream` umożliwia:
  - Strumieniowe odczytywanie plików multimedialnych.
  - Pobieranie zawartości `M3U8` zdalnie.
  - Asynchroniczne zarządzanie strumieniami.

### 3. **Zarządzanie napisami**
- Klasa `SubtitleManager` pozwala na:
  - Wczytywanie napisów z plików w formacie SRT.
  - Wyszukiwanie napisów w określonym przedziale czasowym.
  - Obsługą wyjątków związanych z błędami wczytywania i parsowania napisów.

## Wymagania

- **Platforma:** .NET Framework 4.8.1
- **Język:** C# 12.0
- **Dodatkowe biblioteki:**
  - [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) - do obsługi JSON.
  - [System.Windows.Media](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media) - do obsługi kolorów i czcionek w WPF.

## Instalacja

1. **Sklonuj repozytorium**:
```bash
git clone https://github.com/[TwojeRepozytorium]/Thmd.git
```
2. Otwórz projekt w Visual Studio 2022.
3. Przygotuj środowisko:
- Upewnij się, że masz zainstalowany `.NET Framework 4.8`.
- Zainstaluj wymagane pakiety `NuGet`.

## **Przykłady użycia**

### **Konfiguracja**
```csharp
using Thmd.Configuration;

var config = Config.Instance; 
config.UpdateAndSave(cfg => 
{ 
  cfg.EnableLogging = true; 
  cfg.ApiKey = "new-api-key"; 
});
```

### Obsługa multimediów
```csharp
using Thmd.Media;

var mediaStream = new FileMediaStream("sample.mp4"); 
var duration = mediaStream.GetDuration(); 
Console.WriteLine($"Czas trwania: {duration} sekund");
```

### Zarządzanie napisami
```csharp
using Thmd.Subtitles;

var subtitleManager = new SubtitleManager("napisy.srt"); 
var subtitles = subtitleManager.GetStartToEndTimeSpan(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(60)); 
foreach (var subtitle in subtitles) 
{ 
  Console.WriteLine(subtitle.Text); 
}
```

## Struktura projektu

- **Configuration**: zarządzanie ustawieniami aplikacji.
- **Media**: Obsługa plików multimedialnych i strumieni.
- **Subtitles**: zarządzanie napisami w formacie SRT.

## Autorzy

Projekt został stworzony przez zespół **Softbery**, zaś głównym CO projektu  jest **Paweł Tobis**. Wszelkie pytania i sugestie prosimy kierować na [adres e-mail](mailto:softbery@gmail.com).

## Licencja

Ten projekt jest objęty licencją MIT. Szczegóły znajdują się w pliku `LICENSE`.