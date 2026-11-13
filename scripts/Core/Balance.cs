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
    public const double SpawnIntervalBase = 4.0;     // Sekunden zwischen neuen Besuchern
    public const double SpawnBonusPerPath = 0.5;     // +50 % Besucher pro Weg
    public const double SpawnBonusPerBin = 0.1;      // +10 % pro Mülleimer
    public const int MaxVisitorsBase = 6;
    public const int MaxVisitorsPerPath = 4;
    public const int MaxVisitorsPerBench = 2;
    public const double StayBase = 22;               // Sekunden, die jemand sitzen bleibt
    public const double StayBonusPerBench = 0.25;    // +25 % Verweildauer pro Bank
    public const double DrinkInterval = 5;           // alle X Sekunden ist eine Flasche leer
    public const double WalkSpeed = 45;              // Pixel pro Sekunde
    public const double EveningLeaveChancePerSecond = 0.1; // ohne Laternen

    // Imbissbude
    public const double ImbissSpawnBonus = 0.5;      // +50 % Besucher
    public const int ImbissMaxVisitors = 6;
    public const double ImbissDrinkFactor = 0.7;     // Leute trinken schneller
    public const double ImbissVisitChance = 0.4;     // Anteil, der erst zum Imbiss geht

    // Grillplatz: eine Grillrunde nach der anderen, Bier landet im Kasten
    public const int GrillGroupMin = 4;
    public const int GrillGroupMax = 6;
    public const double GrillStayFactor = 2.0;       // bleiben doppelt so lange
    public const double GrillDrinkFactor = 2.2;      // pro Kopf langsamer, aber viele Leute: Kasten ist nach ~1 Minute voll
    public const int KastenCapacity = 20;

    // Bierball (Flunkyball)
    public const double BierballHitChance = 0.4;
    public const double BierballDrinkRate = 1.2;    // Flaschen pro Sekunde, solange das eigene Team trinken darf
    public const double BierballRunSpeed = 75;

    // Männertag: Gruppen mit Bollerwagen ziehen durch den Park
    public const int MaennertagGroups = 3;
    public const double MaennertagGroupInterval = 15;
    public const double MaennertagStopSeconds = 5;
    public const double MaennertagDropPerSecondWalking = 0.25;

    // Flaschen
    public const int MaxGroundBottles = 120;
    public const int StartBottles = 10;
    public const double BinChancePerBin = 0.25;
    public const double BinChanceMax = 0.8;
    public const int BinCapacity = 15;
    public const int BinCapacityPerLevel = 3;        // Skill "Große Eimer"
    public const double BinRingChancePerLevel = 0.03; // Skill "Pfandringe": pro Eimer

    // Liegen gelassene Flaschen nehmen nach einer Weile andere Leute mit (Eimer und Kästen nicht)
    public const int BottleLifetime = 60;            // Sekunden
    public const int BottleLifetimePerLevel = 12;    // Skill "Ruhiges Revier"
    public const float BottleBlinkSeconds = 10;      // so lange vorher blinkt die Flasche

    // Flaschensorten: am Anfang nur Bier, die anderen schaltet der Skilltree frei (Gewichte beim Auswürfeln)
    public const double WeightBier = 1.0;
    public const double WeightGlas = 0.35;
    public const double WeightPlastik = 0.45;
    public const double WeightDose = 0.55;
    public const double WeightBierPerLevel = 0.10;   // "Mehr Bier"
    public const double WeightOtherPerLevel = 0.15;  // "Mehr Glas/Plastik/Dosen"

    // Seltene Flaschen (brauchen Skills)
    public const double RetroChancePerLevel = 0.005;
    public const double GoldChancePerLevel = 0.001;

    // Liegenlassen: Anfangs nehmen 40 % der Leute ihre Flaschen wieder mit
    public const double LeaveChanceBase = 0.6;
    public const double LeaveChancePerLevel = 0.04;   // Skill "Sorglose Gäste", Stufe 10 -> 100 %

    // Klicken
    public const float ClickRadiusBase = 18;
    public const float ClickRadiusPerLevel = 0.15f;   // Skill "Greifzange": +15 % pro Stufe

    // Lastenrad
    public const double KastenPfand = 1.50;

    // Freundeskreis (Gruppen)
    public const double GroupChancePerLevel = 0.25;  // Chance, dass jemand Freunde mitbringt
    public const int GroupExtraMax = 1;              // + Level = max. Freunde
    public const double GroupCrateChanceBase = 0.15; // Gruppe bringt einen Bierkasten mit
    public const double GroupCrateChancePerLevel = 0.05; // Skill "Bierkasten"

    // Skilltree: Preis steigt mit dem Abstand zur Wurzel
    public const double SkillDepthCost = 1.5;        // ×1,5 pro Knoten Abstand (weit außen wird es richtig teuer)

    // Tischtennis
    public const double TableSpawnBonus = 0.2;
    public const double TableDrinkFactor = 0.8;      // Spieler trinken schneller

    // Sammler (Angestellte)
    public const double CollectorSpeed = 55;
    public const int CollectorCarry = 4;

    // Automat
    public const double FeedInterval = 0.1;          // Sekunden pro eingeworfener Flasche
    public const double CapDropChance = 0.03;        // Chance auf einen Kronkorken (nicht bei Dosen)
    public const int FirstCapGuaranteedAt = 12;      // spätestens diese Flasche bringt den ersten Kronkorken

    // Kombo: langer Bon = mehr Geld. Ab der 10. Flasche eines Einwurfs +1 % auf den ganzen Bon
    public const int KomboStart = 10;
    public const double KomboPerBottle = 0.01;
    public const double KomboMax = 0.5;
    public const double KomboMaxPerLevel = 0.1;      // Skill "Langer Bon"

    // Kronkorken-Seltenheit: Gewicht je Stufe im Set (häufig, selten, sehr selten, legendär)
    public static readonly double[] CapTierWeight = { 10, 4, 1.5, 0.5 };
    public const double CapKennerPerLevel = 0.15;    // Skill "Kennerblick": seltene Stufen öfter
    public const double AlbumjaegerPerLevel = 0.1;

    // Picknickdecken: ohne Upgrade stehen die Leute nur kurz auf der Wiese herum
    public const double StandStayFactor = 0.85;
    public const double BlanketStayFactor = 1.3;
    public const double BlanketChanceBase = 0.5;     // Stufe 1: jeder Zweite hat eine Decke dabei
    public const double BlanketChancePerLevel = 0.25;

    // Events kosten Kronkorken. Nach jedem großen Event haben alle eine gemeinsame Pause
    public const double EventPause = 300;            // Sekunden
    public const double EventPauseCutPerLevel = 0.06; // Skill "Gute Kontakte"

    // Aktionen gibt es nur noch per Flyer, der ab und zu im Park liegt
    public const double FlyerMeanInterval = 240;     // im Schnitt alle 4 Minuten
    public const double FlyerLifetime = 8;           // so lange liegt er, dann weht er weg

    // Events werden mit jedem Mal teurer
    public const double EventCostGrowth = 0.25;      // +25 % des Grundpreises pro Durchführung
    public const int EventCostGrowthMax = 4;         // höchstens viermal (also maximal doppelter Preis)

    // Speichern
    public const double AutosaveSeconds = 30;
}
