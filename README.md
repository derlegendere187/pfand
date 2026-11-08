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
- Upgrades rechts kaufen, Album unten rechts

## Debug-Parameter
Nach `--` anhängen, z. B. `... --path . -- --fresh --money=50`:

| Parameter | Wirkung |
|---|---|
| `--fresh` | Spielstand weder laden noch speichern |
| `--money=50` | Startgeld setzen |
| `--late` | Park mit einigen Upgrades ausstatten |
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
