# Pfand

Ein Incremental-Spiel: Du schaust von oben auf einen Park, sammelst die Pfandflaschen der Besucher ein, wirfst sie in den Automaten und baust den Park mit dem Geld aus. Mehr Wege, Bänke und Laternen bringen mehr Leute und damit mehr Flaschen.

## Voraussetzungen
- .NET SDK 8
- Godot 4.7 (.NET/Mono-Version)

## Starten
- **Im Editor:** Godot öffnen → Projekt importieren → `project.godot` wählen → F5
- **Kommandozeile:**
  ```
  dotnet build
  Godot_v4.7.2-stable_mono_win64_console.exe --path .
  ```

## Steuerung
- Beim ersten Start erklärt Kalle, ein alter Pfandsammler, wie alles funktioniert. Danach taucht er immer wieder auf und erklärt Neues (Laterne um 22 Uhr, Mülleimer, Kronkorken und Skilltree, Events …)
- Flaschen anklicken (oder mit gedrückter Maustaste drüberwischen) → in die Tasche
- Mülleimer anklicken → alle Flaschen daraus einsammeln
- „Einwerfen“ im Automaten → Geld
- Upgrades tauchen nach und nach auf: das nächste erst, wenn das vorherige oft genug gekauft ist (meist 2×)
- **Kronkorken** stecken ab und zu in eingeworfenen Flaschen (Dosen haben keine). Sie landen im **Album** und sind die Währung für den **Skilltree**
- Im **PARK-Ast** des Skilltrees schaltest du neue Upgrades frei (Picknickdecke, Mülleimer, Bank, Sammler, Imbiss, Grill …) und baust alle Objekte einer Sorte auf einmal aus (Tische an Bänken, größere Eimer …). Danach kaufst du sie rechts mit Geld
- **Events** erscheinen erst, wenn sie im Skilltree gelernt sind. Sie kosten Geld, haben einen Cooldown und werden mit jedem Mal teurer
- Skilltree: Fläche mit der Maus ziehen, Knoten anklicken, „Lernen“ oder Doppelklick
- Bierkasten (am Grill oder von Freundesgruppen stehen gelassen) anklicken → eine Flasche in die Tasche (mit Lastenrad: ganzer Kasten direkt zum Automaten)
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
| `--hint=muelleimer` | einen Tipp von Kalle erneut zeigen (`tasche_voll`, `laterne`, `kronkorken`, `muelleimer`, `sammler`, `kasten`, `events`, `grill`) |
| `--money=50` | Startgeld setzen |
| `--late` | Park mit einigen Upgrades ausstatten |
| `--imbiss` | Imbissbude freischalten und bauen |
| `--upgrades=sammler:2,tischtennis:1` | Upgrade-Stufen setzen |
| `--allskills` | alle Skills voll lernen |
| `--skills=wert:2,aoe:1` | einzelne Skill-Stufen setzen |
| `--kk=20` | Kronkorken setzen |
| `--captoast` | Kronkorken-Meldung nach 1 s einmal zeigen |
| `--feedtest` | Tasche füllen und nach 1 s automatisch einwerfen |
| `--event=konzert` | Event gratis sofort starten (`geburtstag`, `bierball`, `konzert`, `feiertag`, `maennertag`, `oktoberfest`) |
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
