# Pfand

Ein Incremental-Spiel: Du schaust von oben auf einen Park, sammelst die Pfandflaschen der Besucher ein, wirfst sie in den Automaten und baust den Park mit dem Geld aus. Mehr Wege, Bänke und Laternen bringen mehr Leute und damit mehr Flaschen.

## Loslegen – Schritt für Schritt (auch ohne Vorwissen)

Diese Anleitung ist für Windows 10/11. Du brauchst etwa 15 Minuten. Am Ende läuft das Spiel auf deinem PC, und du kannst den Code selbst ändern.

### Schritt 1: Ein Terminal öffnen
Ein **Terminal** ist ein Fenster, in das du Befehle tippst statt zu klicken. So öffnest du eins:
1. Drücke die **Windows-Taste**, tippe `PowerShell` und drücke **Enter**.
2. Es öffnet sich ein blaues oder schwarzes Fenster mit einer Zeile wie `PS C:\Users\DeinName>`. Das ist die Stelle, an der du tippst.

**So gibst du einen Befehl ein:** Befehl aus dieser Anleitung kopieren, im Terminal mit **Rechtsklick** einfügen, **Enter** drücken und warten, bis wieder `PS C:\...>` erscheint.

### Schritt 2: Die drei nötigen Programme installieren
Gib diese drei Befehle nacheinander ein, also immer einen Befehl, Enter, warten:
```
winget install Git.Git
```
```
winget install Microsoft.DotNet.SDK.8
```
```
winget install GodotEngine.GodotEngine.Mono
```
Was das ist:
- **Git** lädt das Projekt herunter und hält es später aktuell.
- **.NET SDK 8** übersetzt den C#-Code des Spiels.
- **Godot (.NET-Version)** ist die Spiele-Engine, in der das Spiel läuft und bearbeitet wird.

Beim ersten `winget` fragt Windows eventuell, ob du den Bedingungen zustimmst. Tippe dann `Y` und Enter. Falls ein Fenster fragt, ob das Programm Änderungen vornehmen darf, klickst du **Ja**.

**Danach das Terminal schließen und neu öffnen** (wie in Schritt 1). Sonst findet es die neuen Programme noch nicht.

Prüfen, ob alles geklappt hat:
```
git --version
```
```
dotnet --version
```
Beide sollten eine Versionsnummer anzeigen, bei `dotnet` beginnend mit `8.`. Erscheint stattdessen „wurde nicht als Name eines Cmdlet … erkannt“, hast du das Terminal nicht neu geöffnet, oder die Installation ist fehlgeschlagen.

> **Kein winget?** Lade die Programme von Hand und installiere sie mit den Standardeinstellungen: [Git](https://git-scm.com/downloads), [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0) (wichtig: **SDK**, nicht „Runtime“) und [Godot](https://godotengine.org/download) (wichtig: die Variante **„.NET“**; das ist eine ZIP-Datei, einfach entpacken). Die normale Godot-Version ohne „.NET“ kann kein C# und startet das Spiel nicht.

### Schritt 3: Das Projekt herunterladen („klonen“)
Lege fest, wo das Projekt liegen soll, z. B. in deinen Dokumenten. Gib im Terminal ein:
```
cd $HOME\Documents
```
```
git clone https://github.com/derlegendere187/pfand.git
```
Jetzt gibt es den Ordner **Dokumente\pfand** mit dem ganzen Spiel. „Klonen“ heißt: Du hast eine vollständige Kopie, inklusive aller bisherigen Änderungen.

> Es gibt auf GitHub auch den grünen Knopf **Code → Download ZIP**. Das funktioniert auch, aber dann kannst du das Projekt später nicht mit einem Befehl aktualisieren. Besser `git clone` nehmen.

### Schritt 4: Das Projekt in Godot öffnen und starten
1. Öffne **Godot** über das Startmenü (Windows-Taste, `Godot` tippen). Nimm den Eintrag **ohne** „console“ im Namen.
2. Im Fenster „Projektverwaltung“ klickst du auf **Importieren**.
3. Navigiere zu **Dokumente\pfand**, wähle die Datei **`project.godot`** und klicke auf **Öffnen** und dann **Importieren & Bearbeiten**.
4. Der Editor öffnet sich. Beim ersten Mal dauert das kurz, weil Godot alles vorbereitet und den C#-Code baut.
5. Klicke oben rechts auf **▶** (oder drücke **F5**). Das Spiel startet.

Ab jetzt steht das Projekt in der Projektverwaltung, und du kannst es mit einem Doppelklick öffnen.

### Schritt 5: Später die neueste Version holen
Wenn es im Projekt Änderungen gibt, holst du sie so: Terminal öffnen und eingeben:
```
cd $HOME\Documents\pfand
```
```
git pull
```
Danach in Godot einfach wieder ▶ drücken.

### Wenn etwas nicht klappt
- **„.NET SDK not found“** oder rote C#-Fehler in Godot: Das .NET SDK 8 fehlt. Wiederhole Schritt 2 und starte Godot danach neu.
- **Godot zeigt Fehler bei allen Skripten** oder es gibt oben keinen „Build“-Knopf: Du hast die normale statt der **.NET**-Version von Godot.
- **`git clone` sagt „already exists“:** Den Ordner gibt es schon. Dann einfach mit Schritt 4 weitermachen.
- **`git pull` meldet einen Konflikt:** Du hast selbst Dateien geändert, die auch im Projekt geändert wurden. Frag im Zweifel nach, bevor du etwas löschst.

### Für Fortgeschrittene
- **Klonen per SSH** (wenn du einen SSH-Schlüssel bei GitHub hinterlegt hast): `git clone git@github.com:derlegendere187/pfand.git`
- **Ohne Editor starten** (im Projektordner):
  ```
  dotnet build
  Godot_v4.7.2-stable_mono_win64_console.exe --path .
  ```
  Liegt Godot nicht im `PATH`, den vollen Pfad zur `.exe` angeben. Mit winget installiert, liegt sie unter `%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_…\`.

## Steuerung
- Beim ersten Start erklärt Kalle, ein alter Pfandsammler, wie alles funktioniert. Danach taucht er immer wieder auf und erklärt Neues (Laterne um 22 Uhr, Mülleimer, Kronkorken und Skilltree, Events …)
- Flaschen anklicken (oder mit gedrückter Maustaste drüberwischen) → in die Tasche. Drüberwischen leert auch Mülleimer und Kästen
- Flaschen, die zu lange liegen (60 s), nehmen andere Leute mit. Kurz vorher blinken sie. In Mülleimern und Kästen bleiben sie liegen
- Mülleimer anklicken → alle Flaschen daraus einsammeln
- „Einwerfen“ im Automaten → Geld. Solange der Automat zählt, kannst du nichts einsammeln
- **Kombo:** Ab der 10. Flasche in einem Einwurf gibt es +1 % auf den ganzen Bon, höchstens +50 % (mit „Langer Bon“ mehr). Eine große Tasche lohnt sich also
- Upgrades tauchen nach und nach auf: das nächste erst, wenn das vorherige oft genug gekauft ist (meist 2×)
- Am Anfang trinken alle nur **Bier**. Glasflaschen, Plastikflaschen und Dosen schaltest du nacheinander im Skilltree frei, jeweils mit eigenen Knoten für Wert und Häufigkeit
- **Kronkorken** stecken ab und zu in Flaschen, die du selbst einwirfst (Dosen haben keine). Sammler bringen erst mit „Ehrliche Sammler“ welche. Sie landen im **Album** und sind die Währung für den **Skilltree** und für Events
- Kronkorken sind verschieden selten (häufig, selten, sehr selten, legendär), spätere Kollektionen insgesamt seltener
- Im **PARK-Ast** des Skilltrees schaltest du neue Upgrades frei (Picknickdecke, Mülleimer, Bank, Sammler, Imbiss, Grill …) und baust alle Objekte einer Sorte auf einmal aus (Tische an Bänken, größere Eimer …). Danach kaufst du sie rechts mit Geld
- **Events** erscheinen erst, wenn sie im Skilltree gelernt sind. Sie kosten Kronkorken, werden mit jedem Mal etwas teurer (höchstens doppelt) und machen alle Flaschen mehr wert, solange sie laufen. Nach jedem Event haben alle Events 5 Minuten Pause. Jedes Event hat im Skilltree einen eigenen Ausbau (EVENT+)
- **Aktionen** (Pfandaktion, Happy Hour, Glückssträhne) sind kurze Gratis-Boosts. Nach dem Freischalten liegt ab und zu ein Flyer im Park, den man direkt anklicken muss, bevor er wegweht
- Skilltree: Maus über einen Knoten zeigt, was er bringt und kostet. Fläche mit der Maus ziehen, Knoten anklicken, „Lernen“ oder Doppelklick
- Bierkasten (am Grill oder von Freundesgruppen stehen gelassen) anklicken → Inhalt in die Tasche, so viel passt. Kästen von Gruppen, die noch picknicken, gehen erst, wenn die Gruppe weg ist. Mit Lastenrad nimmst du leere Kästen mit und bekommst € 1,50 Kastenpfand
- Je weiter ein Knoten im Skilltree von der Wurzel weg ist, desto teurer ist er
- **Stats** unten rechts zeigt die Statistik

## Zum Testen
Im Editor und in Debug-Builds stellen die Tasten **1–5** die Spielgeschwindigkeit ein (×1, ×2, ×5, ×10, ×20). In einem Release-Export gibt es das nicht.

## Debug-Parameter
Nach `--` anhängen, z. B. `... --path . -- --fresh --money=50`:

| Parameter | Wirkung |
|---|---|
| `--fresh` | Spielstand weder laden noch speichern (startet mit Kalles Einführung) |
| `--notut` | Einführung überspringen |
| `--autotut` | Einführung läuft von selbst durch (zum Testen) |
| `--hint=muelleimer` | einen Tipp von Kalle erneut zeigen (`tasche_voll`, `laterne`, `kronkorken`, `muelleimer`, `sammler`, `events`, `grill`) |
| `--money=50` | Startgeld setzen |
| `--late` | Park mit einigen Upgrades ausstatten |
| `--imbiss` | Imbissbude freischalten und bauen |
| `--upgrades=sammler:2,tischtennis:1` | Upgrade-Stufen setzen |
| `--allskills` | alle Skills voll lernen |
| `--skills=wert:2,aoe:1` | einzelne Skill-Stufen setzen |
| `--kk=20` | Kronkorken setzen |
| `--allcaps` | alle Kronkorken-Motive im Album als gefunden markieren |
| `--captoast` | Kronkorken-Meldung nach 1 s einmal zeigen |
| `--feedtest` | Tasche füllen und nach 1 s automatisch einwerfen |
| `--event=konzert` | Event gratis sofort starten (`geburtstag`, `bierball`, `konzert`, `feiertag`, `maennertag`, `oktoberfest`, Aktionen: `pfandaktion`, `happyhour`, `gluecksstraehne`) |
| `--flyer=happyhour` | nach 1 s einen Aktions-Flyer in den Park legen (`pfandaktion`, `happyhour`, `gluecksstraehne`) |
| `--open=skills` | Skilltree, Album (`album`), Statistik (`stats`), Events-Tab (`events`) öffnen |
| `--time=0.7` | Tageszeit setzen (0–1; ab 0.62 Abend, ab 0.82 Nacht) |
| `--speed=4` | Spiel schneller laufen lassen |
| `--screenshot=PFAD --shotdelay=3` | Bildschirmfoto speichern und beenden |

## Aufbau
- `scripts/Core` – Spielstand, Zahlen (`Balance.cs`), Upgrades, Deckel, Speichern
- `scripts/Park` – Spielfeld, Besucher-Simulation, Pixel-Grafiken
- `scripts/UI` – Kopfzeile, Automat, Upgrades, Album, Theme

Der Spielstand liegt unter `%APPDATA%\Godot\app_userdata\Pfand\save.json`.

## Schriftarten
Press Start 2P und VT323, beide unter der SIL Open Font License (siehe `assets/fonts/OFL-*.txt`).
