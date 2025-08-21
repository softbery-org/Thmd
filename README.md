# Thmd# Thmd

Thmd to biblioteka napisana w jêzyku C# przeznaczona do obs³ugi multimediów, zarz¹dzania konfiguracj¹ oraz napisami w aplikacjach WPF. Projekt jest zgodny z .NET Framework 4.8.1 i wykorzystuje nowoczesne funkcje jêzyka C# 12.0.

## Funkcjonalnoœci

### 1. **Konfiguracja**
- Klasa `Config` umo¿liwia zarz¹dzanie ustawieniami aplikacji, w tym:
  - £adowanie i zapisywanie konfiguracji z/do pliku JSON.
  - Obs³uga ustawieñ takich jak po³¹czenie z baz¹ danych, logowanie, œcie¿ki do bibliotek, klucze API i inne.

### 2. **Obs³uga multimediów**
- Klasa `FileMediaStream` umo¿liwia:
  - Strumieniowe odczytywanie plików multimedialnych.
  - Pobieranie zawartoœci M3U8 zdalnie.
  - Asynchroniczne zarz¹dzanie strumieniami.

### 3. **Zarz¹dzanie napisami**
- Klasa `SubtitleManager` pozwala na:
  - Wczytywanie napisów z plików w formacie SRT.
  - Wyszukiwanie napisów w okreœlonym przedziale czasowym.
  - Obs³ugê wyj¹tków zwi¹zanych z b³êdami wczytywania i parsowania napisów.

## Wymagania

- **Platforma:** .NET Framework 4.8.1
- **Jêzyk:** C# 12.0
- **Dodatkowe biblioteki:**
  - [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) - do obs³ugi JSON.
  - [System.Windows.Media](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media) - do obs³ugi kolorów i czcionek w WPF.

## Instalacja

1. Sklonuj repozytorium:
```bash
git clone https://github.com/TwojeRepozytorium/Thmd.git
```
2. Otwórz projekt w Visual Studio 2022.
3. Przygotuj œrodowisko:
- Upewnij siê, ¿e masz zainstalowany .NET Framework 4.8.1.
- Zainstaluj wymagane pakiety NuGet.

## Przyk³ady u¿ycia

### Konfiguracja
```csharp
using Thmd.Configuration;
var config = Config.Instance; config.UpdateAndSave(cfg => { cfg.EnableLogging = true; cfg.ApiKey = "new-api-key"; });
```

### Obs³uga multimediów
```csharp
using Thmd.Media;
var mediaStream = new FileMediaStream("sample.mp4"); var duration = mediaStream.GetDuration(); Console.WriteLine($"Czas trwania: {duration} sekund");
```

### Zarz¹dzanie napisami
```csharp
using Thmd.Subtitles;
var subtitleManager = new SubtitleManager("napisy.srt"); var subtitles = subtitleManager.GetStartToEndTimeSpan(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(60)); foreach (var subtitle in subtitles) { Console.WriteLine(subtitle.Text); }
```

## Struktura projektu

- **Configuration**: Zarz¹dzanie ustawieniami aplikacji.
- **Media**: Obs³uga plików multimedialnych i strumieni.
- **Subtitles**: Zarz¹dzanie napisami w formacie SRT.

## Autorzy

Projekt zosta³ stworzony przez zespó³ **Softbery by Pawe³ Tobis**. Wszelkie pytania i sugestie prosimy kierowaæ na [adres e-mail](mailto:kontakt@softbery.org).

## Licencja

Ten projekt jest objêty licencj¹ MIT. Szczegó³y znajduj¹ siê w pliku `LICENSE`.
