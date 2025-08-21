# Thmd# Thmd

Thmd to biblioteka napisana w j�zyku C# przeznaczona do obs�ugi multimedi�w, zarz�dzania konfiguracj� oraz napisami w aplikacjach WPF. Projekt jest zgodny z .NET Framework 4.8.1 i wykorzystuje nowoczesne funkcje j�zyka C# 12.0.

## Funkcjonalno�ci

### 1. **Konfiguracja**
- Klasa `Config` umo�liwia zarz�dzanie ustawieniami aplikacji, w tym:
  - �adowanie i zapisywanie konfiguracji z/do pliku JSON.
  - Obs�uga ustawie� takich jak po��czenie z baz� danych, logowanie, �cie�ki do bibliotek, klucze API i inne.

### 2. **Obs�uga multimedi�w**
- Klasa `FileMediaStream` umo�liwia:
  - Strumieniowe odczytywanie plik�w multimedialnych.
  - Pobieranie zawarto�ci M3U8 zdalnie.
  - Asynchroniczne zarz�dzanie strumieniami.

### 3. **Zarz�dzanie napisami**
- Klasa `SubtitleManager` pozwala na:
  - Wczytywanie napis�w z plik�w w formacie SRT.
  - Wyszukiwanie napis�w w okre�lonym przedziale czasowym.
  - Obs�ug� wyj�tk�w zwi�zanych z b��dami wczytywania i parsowania napis�w.

## Wymagania

- **Platforma:** .NET Framework 4.8.1
- **J�zyk:** C# 12.0
- **Dodatkowe biblioteki:**
  - [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) - do obs�ugi JSON.
  - [System.Windows.Media](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media) - do obs�ugi kolor�w i czcionek w WPF.

## Instalacja

1. Sklonuj repozytorium:
```bash
git clone https://github.com/TwojeRepozytorium/Thmd.git
```
2. Otw�rz projekt w Visual Studio 2022.
3. Przygotuj �rodowisko:
- Upewnij si�, �e masz zainstalowany .NET Framework 4.8.1.
- Zainstaluj wymagane pakiety NuGet.

## Przyk�ady u�ycia

### Konfiguracja
```csharp
using Thmd.Configuration;
var config = Config.Instance; config.UpdateAndSave(cfg => { cfg.EnableLogging = true; cfg.ApiKey = "new-api-key"; });
```

### Obs�uga multimedi�w
```csharp
using Thmd.Media;
var mediaStream = new FileMediaStream("sample.mp4"); var duration = mediaStream.GetDuration(); Console.WriteLine($"Czas trwania: {duration} sekund");
```

### Zarz�dzanie napisami
```csharp
using Thmd.Subtitles;
var subtitleManager = new SubtitleManager("napisy.srt"); var subtitles = subtitleManager.GetStartToEndTimeSpan(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(60)); foreach (var subtitle in subtitles) { Console.WriteLine(subtitle.Text); }
```

## Struktura projektu

- **Configuration**: Zarz�dzanie ustawieniami aplikacji.
- **Media**: Obs�uga plik�w multimedialnych i strumieni.
- **Subtitles**: Zarz�dzanie napisami w formacie SRT.

## Autorzy

Projekt zosta� stworzony przez zesp� **Softbery by Pawe� Tobis**. Wszelkie pytania i sugestie prosimy kierowa� na [adres e-mail](mailto:kontakt@softbery.org).

## Licencja

Ten projekt jest obj�ty licencj� MIT. Szczeg�y znajduj� si� w pliku `LICENSE`.
