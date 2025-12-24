# SRS – Datei-Kopier-Tool (WPF, C#)

## Ziel
Ein Windows-Desktop-Tool (WPF, .NET 6/7), das alle Dateien eines Quellverzeichnisses inkl. Unterverzeichnissen rekursiv in ein Zielverzeichnis kopiert, mit Fortschrittsanzeige, Status-Text und Abschlussbericht. Bereitstellung als eigenständige EXE.

## Funktionen
- Quell- und Zielverzeichnis per Textfeld oder Folder-Dialog wählen.
- Quellverzeichnis muss existieren; Ziel kann erstellt werden.
- Rekursive Aufzählung aller zu kopierenden Dateien bei Quellwahl; Anzeige der Gesamtzahl.
- Start-Knopf erst aktiv, wenn Quelle existiert und Ziel gesetzt.
- Asynchrones Kopieren mit ProgressBar (0–100%).
- Laufender Status: „Kopiere Datei X von N: <Name>“ in einer Zeile unter dem Fortschrittsbalken.
- Abschlussbericht mit Erfolg/Fehler-Liste (optional scrollbarer Bereich oder Dialog).
- Optional: Abbrechen während des Kopierens.

## Nicht-Funktionen
- Kein Netzwerk-/FTP-Kopieren.
- Keine Deduplizierung oder Synchronisation; stets kopieren (overwrite=true).

## Architektur/Technologie
- .NET 6/7 WPF, C#.
- MVVM-lightweight: View (MainWindow.xaml), ViewModel (MainViewModel), Services (FileCopyService, DialogService).
- Asynchronität via async/await, `IProgress<T>` für UI-Updates.

## UI-Anforderungen
- Zwei Textboxen (Quelle/Ziel) + je ein „Durchsuchen“-Button (FolderBrowserDialog).
- „Kopieren“-Button (disabled, bis Quelle existiert & Ziel gesetzt; disabled während Kopierens).
- ProgressBar unter den Buttons.
- Statuszeile direkt unter ProgressBar: „Kopiere Datei X von N: <Dateiname>“.
- Abschlussbericht (Dialog oder Textbereich) mit Zusammenfassung und Fehlern.
- Optional: „Abbrechen“-Button mit CancellationToken.

## Logik & Datenstrukturen
- Datei-Aufzählung: `Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)` in Task; speichert `totalCount`.
- Progress-DTO: `CopyProgress { int CurrentIndex; int Total; string FileName; double Percent; }`.
- Kopierroutine: `CopyAllAsync(string source, string target, IProgress<CopyProgress>, CancellationToken)`.
- Pro Datei: Zielordner via `Directory.CreateDirectory(Path.GetDirectoryName(targetPath))`; Kopie mit `File.Copy(src, dst, overwrite:true)`.
- Fehlerbehandlung per try/catch je Datei; Fehlerliste für Bericht sammeln.

## Validierungen & Zustände
- Quelle muss existieren; sonst Fehlermeldung, Start gesperrt.
- Ziel: falls nicht vorhanden, anbieten anzulegen (DialogService).
- Sperren der Eingaben während des Kopierens; entsperren bei Ende/Abbruch.

## Fortschritt & Status
- `IProgress<CopyProgress>` meldet UI: Index, Total, Dateiname, Prozent.
- ProgressBar.Value = Percent; StatusText = Format „Kopiere Datei {i} von {n}: {name}“.

## Abbruch (optional)
- UI „Abbrechen“-Button; `CancellationTokenSource`; Schleife prüft Token; bricht sauber ab und meldet Teilresultat.

## Bericht
- Inhalte: Start-/Endzeit, Dauer, Anzahl kopiert, Anzahl Fehler, Liste fehlgeschlagener Dateien mit Meldung.
- Anzeige in Dialog oder Textbereich; Status „Fertig“ setzen.

## Fehlerfälle
- Fehlende Leserechte/gesperrte Dateien → Fehlerliste, Kopieren läuft weiter.
- Lange Pfade → optional `GroupPolicy`-Hinweis; Standard-API nutzen.
- Ziel nicht schreibbar → Abbruch mit Dialog.

## Tests / Checks
- Pfad-Validierungen: Quelle existiert, Ziel erstellbar.
- Szenarien: leeres Quellverzeichnis (kein Start), vorhandene Ziel-Dateien (overwrite), gesperrte Datei (Fehlerliste), Abbruch mitten im Kopieren.
- Kleiner Testbaum mit Unterordnern.

## Bereitstellung
- Publish: `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`.
- Ergebnis: EXE im `bin/Release/netX.X/win-x64/publish/`.
