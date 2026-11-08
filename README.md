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
- Flaschen anklicken (oder mit gedrückter Maustaste drüberwischen) → in die Tasche
- Mülleimer anklicken → alle Flaschen daraus einsammeln
- „Einwerfen“ im Automaten → Geld
- Rechts zwischen **Upgrades** und **Events** umschalten; Events kosten Geld und haben einen Cooldown
- **Ausbau:** Rechtsklick auf Bank, Mülleimer, Imbiss, Tischtennisplatte oder Grill → einzelnes Objekt ausbauen
- **Kronkorken** (selten beim Einwerfen) landen im **Album** und sind die Währung für den **Skilltree**
- Skilltree: Fläche mit der Maus ziehen, Knoten anklicken, „Lernen“ oder Doppelklick
- Bierkasten am Grill anklicken → Flaschen in die Tasche (mit Lastenrad: ganzer Kasten direkt zum Automaten)
- **Stats** unten rechts zeigt die Statistik

## Debug-Parameter
Nach `--` anhängen, z. B. `... --path . -- --fresh --money=50`:

| Parameter | Wirkung |
|---|---|
| `--fresh` | Spielstand weder laden noch speichern |
| `--money=50` | Startgeld setzen |
| `--late` | Park mit einigen Upgrades ausstatten |
| `--imbiss` | Imbissbude freischalten und bauen |
| `--upgrades=sammler:2,tischtennis:1` | Upgrade-Stufen setzen |
| `--allskills` | alle Skills voll lernen |
| `--skills=wert:2,aoe:1` | einzelne Skill-Stufen setzen |
| `--kk=20` | Kronkorken setzen |
| `--feedtest` | Tasche füllen und nach 1 s automatisch einwerfen |
| `--ausbau=bank:0:2,muelleimer:1:1` | Ausbaustufen setzen (Sorte:Nummer:Stufe) |
| `--event=konzert` | Event gratis sofort starten (`geburtstag`, `bierball`, `konzert`, `feiertag`, `maennertag`, `oktoberfest`) |
| `--open=skills` | Skilltree, Album (`album`), Statistik (`stats`), Events-Tab (`events`) oder Ausbau (`ausbau:bank:0`) öffnen |
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
