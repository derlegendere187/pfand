namespace Pfand.Core;

/// <summary>
/// Alle Spielzahlen an einem Ort. Zum Balancing nur hier drehen.
/// </summary>
public static class Balance
{
    // Tasche
    public const int BagBase = 8;
    public const int BagPerLevel = 4;

    // Tag/Abend/Nacht (Anteile eines Tages, 0..1)
    public const double DayLengthSeconds = 180;
    public const double EveningStart = 0.62;
    public const double NightStart = 0.82;
    public const double StartTimeOfDay = 0.05;

    // Besucher
    public const double SpawnIntervalBase = 6.0;     // Sekunden zwischen neuen Besuchern
    public const double SpawnBonusPerPath = 0.5;     // +50 % Besucher pro Weg
    public const double SpawnBonusPerBin = 0.1;      // +10 % pro Mülleimer
    public const int MaxVisitorsBase = 5;
    public const int MaxVisitorsPerPath = 4;
    public const int MaxVisitorsPerBench = 2;
    public const double StayBase = 22;               // Sekunden, die jemand sitzen bleibt
    public const double StayBonusPerBench = 0.25;    // +25 % Verweildauer pro Bank
    public const double DrinkInterval = 8;           // alle X Sekunden ist eine Flasche leer
    public const double WalkSpeed = 45;              // Pixel pro Sekunde
    public const double EveningLeaveChancePerSecond = 0.1; // ohne Laternen

    // Imbissbude
    public const int ImbissUnlockVisitors = 20;      // so viele Leute gleichzeitig im Park
    public const double ImbissSpawnBonus = 0.5;      // +50 % Besucher
    public const int ImbissMaxVisitors = 6;
    public const double ImbissDrinkFactor = 0.7;     // Leute trinken schneller
    public const double ImbissVisitChance = 0.4;     // Anteil, der erst zum Imbiss geht

    // Flaschen
    public const int MaxGroundBottles = 80;
    public const int StartBottles = 4;
    public const double BinChancePerBin = 0.25;
    public const double BinChanceMax = 0.8;
    public const int BinCapacity = 15;

    // Seltene Flaschen (brauchen Skills)
    public const double RetroChance = 0.04;
    public const double GoldChance = 0.006;

    // Freundeskreis (Gruppen)
    public const double GroupChancePerLevel = 0.25;  // Chance, dass jemand Freunde mitbringt
    public const int GroupExtraMax = 1;              // + Level = max. Freunde

    // Tischtennis
    public const double TableSpawnBonus = 0.2;
    public const double TableDrinkFactor = 0.8;      // Spieler trinken schneller

    // Sammler (Angestellte)
    public const double CollectorSpeed = 55;
    public const int CollectorCarry = 4;

    // Automat
    public const double FeedInterval = 0.1;          // Sekunden pro eingeworfener Flasche
    public const double CapDropChance = 0.025;       // Chance auf einen Kronkorken (nicht bei Dosen)

    // Speichern
    public const double AutosaveSeconds = 30;
}
