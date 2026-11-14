using System;
using System.Collections.Generic;
using Godot;
using Pfand.Core;

namespace Pfand.Park;

public enum VisitorState { Kommt, Imbiss, Sitzt, PacktEin, Geht, Wandert }

public class Visitor
{
    public Vector2 Pos, Target, Exit;
    public VisitorState State;
    public double StayTimer, DrinkTimer, SnackTimer;
    public Color Shirt, Hair, BlanketColor;
    public int BlanketStyle;
    public bool HasBlanket;
    public Vector2 BlanketPos;
    public float BlanketOpen;      // 0 = zusammengerollt, 1 = ausgebreitet
    public bool WantsSnack;
    public Vector2 SnackSpot;
    public int Seat = -1;          // Bankplatz (Bank * 4 + Platz), -1 = keiner
    public int TableSeat = -1;     // Tischtennis (Platte * 2 + Seite), -1 = keiner
    public Visitor Leader;         // gehört zu einer Gruppe; geht, wenn der Leader geht
    public string EventId;         // Gast eines Events
    public int GrillSeat = -1;     // Platz in der Grillrunde
    public int Team = -1;          // Bierball: 0 = links, 1 = rechts
    public float Lunge;            // Bierball: Ausholen beim Werfen (0..1)
    public float DrinkAnim;        // Flasche am Mund (0..1)
    public double DrinkProgress;   // Bierball: wie weit die Flasche leer ist
    public float WalkPhase;
    // Männertag: Gruppe zieht mit Bollerwagen von Halt zu Halt
    public bool Wagon;
    public List<Vector2> Waypoints;
    public int WaypointIdx;
    public double PauseTimer;
    public Vector2 FollowOffset;
    public bool Tutorial;          // der erste Gast der Einführung
    public GroupCrate Crate;       // Freundesgruppe: Bierkasten des Anführers
}

/// <summary>Bierkasten einer Freundesgruppe. Leere kommen wieder rein; beim Gehen bleibt er stehen.</summary>
public class GroupCrate
{
    public Vector2 Pos;
    public readonly List<BottleType> Bottles = new();
    public Visitor Owner;          // null = stehen gelassen
    public bool Visible => Owner == null || Owner.State == VisitorState.Sitzt || Owner.State == VisitorState.PacktEin;
}

/// <summary>Bierball wie im echten Leben: Ein Team wirft auf die Flasche in der Mitte.
/// Bei einem Treffer trinkt das Werferteam, bis ein Läufer der Gegner die Flasche wieder aufgestellt hat.</summary>
public class BierballGame
{
    public enum Step { Warten, Ausholen, Fliegt, Daneben, Hinlaufen, Aufstellen, Zuruecklaufen }
    public const double FlyTime = 0.45;
    public Vector2 FlyFrom, FlyTo;  // geworfene Flasche
    public bool Hit;
    public int Throwing;
    public Step Phase = Step.Warten;
    public double Timer = 1.5;
    public Visitor Thrower, Runner;
    public bool BottleDown;
    public float Wobble;
}

public class GroundBottle
{
    public Vector2 Pos;
    public BottleType Type;
    public float Rot;
    public int ColorIdx;
    public float Age;
    public bool Claimed;           // ein Sammler ist schon unterwegs
}

/// <summary>Flyer für eine Aktion: liegt kurz im Park, ein direkter Klick startet die Aktion.</summary>
public class Flyer
{
    public Vector2 Pos;
    public string EventId;
    public double Life;
    public static readonly Vector2 Size = new(22, 26);
    public Rect2 Rect => new(Pos - Size / 2, Size);
}

/// <summary>Fremder Flaschensammler: holt Flaschen, die zu lange liegen, und geht wieder.</summary>
public class Stranger
{
    public Vector2 Pos, Exit;
    public GroundBottle Target;
    public bool Leaving, Carrying;
    public float WalkPhase;
}

public enum CollectorState { Sucht, ZurFlasche, ZumEimer, Abliefern, Wartet }

public class Collector
{
    public Vector2 Pos, Target;
    public CollectorState State = CollectorState.Sucht;
    public GroundBottle TargetBottle;
    public int TargetBin = -1;
    public readonly List<BottleType> Carry = new();
    public float WalkPhase;
    public double Wait;
}

/// <summary>
/// Park-Simulation: Besucher kommen (allein oder in Gruppen), setzen sich, trinken, lassen leere Flaschen
/// liegen und gehen wieder. Dazu Tischtennis-Spieler, Event-Gäste und angestellte Sammler.
/// Arbeitet in Pixel-Koordinaten des Spielfelds.
/// </summary>
public class VisitorSim
{
    public readonly List<Visitor> Visitors = new();
    public readonly List<GroundBottle> Bottles = new();
    public readonly List<List<BottleType>> BinContents = new();
    public readonly List<Collector> Collectors = new();
    public readonly List<Stranger> Strangers = new();
    public readonly List<(Vector2 pos, string text, Color color)> PendingFloats = new();
    public readonly List<BottleType> Kasten = new();
    public readonly List<GroupCrate> Crates = new();
    public BierballGame Bierball;
    public Flyer Flyer;
    public Vector2 Size;

    readonly Random _rng = new();
    readonly Queue<double> _dropTimes = new();
    double _clock, _spawnAcc, _grillWait = 2, _mtTimer;
    int _mtGroups;
    bool _seeded, _tutGuestSpawned;
    string _eventSpawnedFor;

    public int BottlesPerMinute => _dropTimes.Count;

    double R(double a, double b) => a + _rng.NextDouble() * (b - a);
    float Rf(float a, float b) => (float)R(a, b);

    public void Update(double dt, GameState gs)
    {
        if (Size.X < 50 || Size.Y < 50) return;
        _clock += dt;
        while (_dropTimes.Count > 0 && _clock - _dropTimes.Peek() > 60) _dropTimes.Dequeue();

        // Während der Einführung bleibt die Uhr stehen, damit man in Ruhe lesen kann
        if (!gs.TutorialActive && !gs.ClockPaused) gs.TimeOfDay = (gs.TimeOfDay + dt / Balance.DayLengthSeconds) % 1.0;
        gs.UpdateEvents(dt);

        var paths = ParkLayout.Paths(Size, gs);
        var bins = ParkLayout.Bins(Size, gs);
        var benches = ParkLayout.Benches(Size, gs);
        var tables = ParkLayout.Tables(Size, gs);
        int lamps = gs.Level("laterne");
        bool imbiss = gs.Level("imbiss") > 0;
        while (BinContents.Count < bins.Count) BinContents.Add(new List<BottleType>());

        if (!_seeded) Seed(gs, paths, benches, tables);

        UpdateEventGuests(gs);
        if (gs.Tutorial == TutorialStep.WatchGuest && !_tutGuestSpawned) SpawnTutorialGuest(paths);

        // --- Neue Besucher ---
        double lampFactor = lamps / 3.0;
        double phaseFactor = gs.Phase switch
        {
            DayPhase.Tag => 1.0,
            DayPhase.Abend => 0.5 + 0.5 * lampFactor,
            _ => lampFactor,
        };
        var activeEvent = gs.ActiveEvent == null ? null : EventDefs.Get(gs.ActiveEvent);
        int maxVisitors = Balance.MaxVisitorsBase + Balance.MaxVisitorsPerPath * paths.Count + Balance.MaxVisitorsPerBench * benches.Count
            + (imbiss ? Balance.ImbissMaxVisitors + gs.ImbissExtraSlots : 0) + tables.Count * 2
            + (activeEvent?.Guests ?? 0) + (activeEvent?.ExtraVisitors ?? 0) + gs.ExtraVisitorSlots;
        double boost = 1 + Balance.SpawnBonusPerPath * paths.Count + Balance.SpawnBonusPerBin * bins.Count
            + (imbiss ? Balance.ImbissSpawnBonus : 0) + Balance.TableSpawnBonus * tables.Count + (activeEvent?.SpawnBoost ?? 0) + gs.VisitorBoost;
        double interval = Balance.SpawnIntervalBase / boost;
        if (phaseFactor > 0 && !gs.TutorialActive)
        {
            _spawnAcc += dt * phaseFactor;
            if (_spawnAcc >= interval)
            {
                _spawnAcc = 0;
                if (Visitors.Count < maxVisitors) Spawn(gs, paths, benches, tables, false);
            }
        }
        if (gs.Level("grill") > 0) UpdateGrill(dt, paths, phaseFactor);
        UpdateBierball(dt, gs, bins);
        UpdateMaennertag(dt, gs, paths);

        if (Visitors.Count > gs.PeakVisitors)
        {
            gs.PeakVisitors = Visitors.Count;
            if (gs.PeakVisitors % 5 == 0) gs.Notify();
        }
        double drinkInterval = Balance.DrinkInterval * (imbiss ? Balance.ImbissDrinkFactor * gs.ImbissDrinkBonus : 1) * gs.DrinkFactor;

        // --- Besucher bewegen ---
        for (int i = Visitors.Count - 1; i >= 0; i--)
        {
            var v = Visitors[i];
            switch (v.State)
            {
                case VisitorState.Kommt:
                    if (v.WantsSnack && imbiss)
                    {
                        // Erst zum Imbiss, dort kurz anstehen
                        if (Walk(v, v.SnackSpot, dt))
                        {
                            v.State = VisitorState.Imbiss;
                            v.SnackTimer = R(2, 4);
                        }
                        break;
                    }
                    if (Walk(v, v.Target, dt))
                    {
                        v.State = VisitorState.Sitzt;
                        v.StayTimer = Balance.StayBase * (1 + Balance.StayBonusPerBench * benches.Count) * R(0.7, 1.3);
                        v.StayTimer *= StayFactor(v, gs);
                        v.DrinkTimer = DrinkIntervalFor(v, drinkInterval) * R(0.6, 1.2);
                        if (v.Tutorial) { v.StayTimer = 999; v.DrinkTimer = 2.5; }
                    }
                    break;

                case VisitorState.Imbiss:
                    v.SnackTimer -= dt;
                    if (v.SnackTimer <= 0) { v.WantsSnack = false; v.State = VisitorState.Kommt; }
                    break;

                case VisitorState.Sitzt:
                    if (v.HasBlanket) v.BlanketOpen = Mathf.Min(1f, v.BlanketOpen + (float)dt * 3f);
                    v.StayTimer -= dt;
                    v.DrinkTimer -= dt;
                    if (v.DrinkTimer <= 0 && v.Tutorial)
                    {
                        // Der erste Gast lässt garantiert seine Flasche auf der Wiese liegen
                        AddGroundBottle(v.Pos + new Vector2(Rf(10, 22) * (_rng.Next(2) == 0 ? -1 : 1), Rf(10, 18)), BottleType.Bier);
                        _dropTimes.Enqueue(_clock);
                        v.DrinkTimer = double.MaxValue;
                        v.StayTimer = 2.5;
                        if (gs.Tutorial == TutorialStep.WatchGuest) { gs.Tutorial = TutorialStep.ClickBottle; gs.Notify(); }
                    }
                    else if (v.DrinkTimer <= 0)
                    {
                        Drop(v, bins, gs);
                        v.DrinkTimer = DrinkIntervalFor(v, drinkInterval) * R(0.8, 1.2);
                    }
                    if (ShouldLeave(v, gs, lamps, dt))
                    {
                        if (v.EventId != null || _rng.NextDouble() < 0.5) Drop(v, bins, gs);
                        v.State = v.HasBlanket ? VisitorState.PacktEin : VisitorState.Geht;
                        if (v.Crate != null) ReleaseCrate(v, gs);
                        v.Seat = -1;
                        v.TableSeat = -1;
                        v.GrillSeat = -1;
                        v.DrinkAnim = 0;
                        v.Lunge = 0;
                    }
                    break;

                case VisitorState.PacktEin:
                    // Decke wieder zusammenlegen, dann gehen
                    v.BlanketOpen -= (float)dt * 3f;
                    if (v.BlanketOpen <= 0) { v.BlanketOpen = 0; v.State = VisitorState.Geht; }
                    break;

                case VisitorState.Geht:
                    if (Walk(v, v.Exit, dt)) Visitors.RemoveAt(i);
                    break;

                case VisitorState.Wandert:
                    UpdateWanderer(v, dt, bins, gs);
                    break;
            }
        }

        UpdateCollectors(dt, gs, bins);
        foreach (var b in Bottles) b.Age += (float)dt;
        UpdateStrangers(dt, gs);
        UpdateFlyer(dt, gs, paths);
    }

    /// <summary>Freigeschaltete Aktionen kommen per Flyer: selten, kurz, nur mit direktem Klick.</summary>
    void UpdateFlyer(double dt, GameState gs, List<Rect2> paths)
    {
        if (Flyer != null)
        {
            if (!gs.ClockPaused) Flyer.Life -= dt;
            if (Flyer.Life <= 0) Flyer = null;
        }
        else if (!gs.TutorialActive && !gs.ClockPaused)
        {
            var choices = new List<string>();
            foreach (var e in EventDefs.All)
                if (e.Short && gs.EventUnlocked(e) && !gs.BoostActive(e.Id)) choices.Add(e.Id);
            if (choices.Count > 0 && _rng.NextDouble() < gs.FlyerRate * dt) SpawnFlyer(choices[_rng.Next(choices.Count)], paths);
        }
        bool visible = Flyer != null;
        if (visible != gs.FlyerVisible) { gs.FlyerVisible = visible; gs.Notify(); }
    }

    public void SpawnFlyer(string eventId, List<Rect2> paths = null)
    {
        // Auf der Wiese, nicht am Rand, nicht auf Wegen
        Vector2 pos = new(Size.X * 0.5f, Size.Y * 0.3f);
        for (int tries = 0; tries < 20; tries++)
        {
            var p = new Vector2(Rf(60, Size.X - 60), Rf(60, Size.Y - 60));
            bool ok = true;
            if (paths != null) foreach (var r in paths) if (r.Grow(16).HasPoint(p)) { ok = false; break; }
            if (ok) { pos = p; break; }
        }
        Flyer = new Flyer { Pos = pos.Round(), EventId = eventId, Life = Balance.FlyerLifetime };
    }

    bool ShouldLeave(Visitor v, GameState gs, int lamps, double dt)
    {
        if (v.EventId != null) return gs.ActiveEvent != v.EventId;
        if (v.Leader != null)
            return v.Leader.State == VisitorState.PacktEin || v.Leader.State == VisitorState.Geht || !Visitors.Contains(v.Leader);
        return v.StayTimer <= 0
            || (gs.Phase == DayPhase.Nacht && lamps == 0)
            || (gs.Phase == DayPhase.Abend && lamps == 0 && _rng.NextDouble() < Balance.EveningLeaveChancePerSecond * dt);
    }

    double DrinkIntervalFor(Visitor v, double normal)
    {
        var gs = GameState.I;
        if (v.EventId == "bierball") return double.MaxValue; // trinken nur nach Treffern
        if (v.EventId != null) return EventDefs.Get(v.EventId)?.GuestDrinkInterval ?? normal;
        if (v.GrillSeat >= 0) return normal * Balance.GrillDrinkFactor * gs.GrillDrinkBonus;
        if (v.TableSeat >= 0) return normal * Balance.TableDrinkFactor * gs.TableDrinkBonus;
        if (v.Seat >= 0) return normal * gs.BankDrinkFactor;
        return normal;
    }

    /// <summary>Wie viel länger jemand bleibt (Grill, ausgebaute Bänke und Platten).</summary>
    static double StayFactor(Visitor v, GameState gs)
    {
        if (v.GrillSeat >= 0) return Balance.GrillStayFactor * gs.GrillStayBonus;
        if (v.Seat >= 0) return gs.BankStayFactor;
        if (v.TableSeat >= 0) return gs.TableStayFactor;
        if (v.EventId != null || v.Wagon) return 1;
        return v.HasBlanket ? Balance.BlanketStayFactor : Balance.StandStayFactor;
    }

    BottleType BottleFor(Visitor v, GameState gs)
    {
        if (v.EventId == "bierball" || v.EventId == "oktoberfest") return BottleType.Bier;
        if (gs.ActiveEvent == "oktoberfest" && _rng.NextDouble() < 0.8) return BottleType.Bier; // ganzer Park auf der Wiesn
        if (v.EventId == "maennertag") return _rng.NextDouble() < 0.75 ? BottleType.Bier : gs.OrBier(BottleType.Dose);
        if (v.EventId == "konzert" && _rng.NextDouble() < 0.6) return gs.OrBier(BottleType.Dose);
        if (v.GrillSeat >= 0) return _rng.NextDouble() < 0.8 ? BottleType.Bier : gs.OrBier(BottleType.Dose);
        return RandomType(gs);
    }

    /// <summary>Freier Platz für einen Bierkasten neben der Decke: nicht auf Wegen, nicht an Eimern, Bänken & Co.</summary>
    Vector2? CrateSpot(Vector2 blanket, List<Rect2> paths, List<Vector2> benches, List<Vector2> tables, GameState gs)
    {
        var size = new Vector2(26, 32);
        var bins = ParkLayout.Bins(Size, gs);
        var offsets = new[] { new Vector2(34, -16), new Vector2(-60, -16), new Vector2(-13, 24), new Vector2(-13, -58), new Vector2(40, 14), new Vector2(-66, 14) };
        foreach (var off in offsets)
        {
            var r = new Rect2(blanket + off, size);
            if (r.Position.X < 4 || r.Position.Y < 4 || r.End.X > Size.X - 4 || r.End.Y > Size.Y - 4) continue;
            var c = r.GetCenter();
            bool ok = true;
            foreach (var p in paths) if (p.Grow(4).Intersects(r)) { ok = false; break; }
            if (ok) foreach (var b in bins) if (b.DistanceTo(c) < 36) { ok = false; break; }
            if (ok) foreach (var b in benches) if (b.DistanceTo(c) < 48) { ok = false; break; }
            if (ok) foreach (var t in tables) if (t.DistanceTo(c) < 56) { ok = false; break; }
            if (ok) foreach (var k in Crates) if (new Rect2(k.Pos, size).Grow(6).Intersects(r)) { ok = false; break; }
            if (ok && gs.Level("imbiss") > 0 && ParkLayout.ImbissRect(Size).Grow(8).Intersects(r)) ok = false;
            if (ok && gs.Level("grill") > 0 && ParkLayout.Grill(Size).DistanceTo(c) < 70) ok = false;
            if (ok) return r.Position;
        }
        return null;
    }

    void ReleaseCrate(Visitor v, GameState gs)
    {
        var c = v.Crate;
        v.Crate = null;
        c.Owner = null;
        if (c.Bottles.Count == 0) Crates.Remove(c);
        else gs.CrateLeftBehind = true;
    }

    /// <summary>Leere, stehen gelassene Kästen verschwinden.</summary>
    public void CleanupCrates() => Crates.RemoveAll(c => c.Owner == null && c.Bottles.Count == 0);

    /// <summary>Besucher hat ausgetrunken: Grill-Bier kommt in den Kasten, sonst Eimer oder Wiese.</summary>
    void Drop(Visitor v, List<Vector2> bins, GameState gs)
    {
        var type = BottleFor(v, gs);
        // Gruppen mit Bierkasten stellen ihre Leeren zurück in den Kasten
        var crate = v.Crate ?? v.Leader?.Crate;
        if (crate != null && crate.Owner != null && crate.Bottles.Count < Balance.KastenCapacity)
        {
            _dropTimes.Enqueue(_clock);
            crate.Bottles.Add(BottleType.Bier);
            return;
        }
        // Normale Besucher nehmen einen Teil ihrer Flaschen wieder mit
        if (v.EventId == null && v.GrillSeat < 0 && type != BottleType.Gold && type != BottleType.Retro
            && _rng.NextDouble() > gs.LeaveChance) return;
        if (v.GrillSeat >= 0 && type == BottleType.Bier && Kasten.Count < Balance.KastenCapacity)
        {
            _dropTimes.Enqueue(_clock);
            Kasten.Add(type);
            return;
        }
        DropBottle(v.Pos, bins, gs, type);
    }

    // --- Grillplatz ---
    void UpdateGrill(double dt, List<Rect2> paths, double phaseFactor)
    {
        foreach (var v in Visitors) if (v.GrillSeat >= 0) return; // Grill ist besetzt
        if (phaseFactor <= 0) return;
        _grillWait -= dt;
        if (_grillWait > 0) return;
        _grillWait = R(6, 12);

        var grill = ParkLayout.Grill(Size);
        int n = Balance.GrillGroupMin + _rng.Next(Balance.GrillGroupMax - Balance.GrillGroupMin + 1);
        var start = EdgePoint(paths);
        var exit = EdgePoint(paths);
        Visitor leader = null;
        for (int i = 0; i < n; i++)
        {
            var g = NewVisitor(paths);
            g.GrillSeat = i;
            g.Target = grill + ParkLayout.GrillSeats[i];
            g.Pos = start + new Vector2(Rf(-14, 14), Rf(-14, 14));
            g.Exit = exit;
            if (leader == null) leader = g; else g.Leader = leader;
            Visitors.Add(g);
        }
    }

    /// <summary>Sitzt gerade eine Grillrunde am Grill? (Dann glüht und raucht er.)</summary>
    public bool GrillBusy
    {
        get
        {
            foreach (var v in Visitors) if (v.GrillSeat >= 0 && v.State == VisitorState.Sitzt) return true;
            return false;
        }
    }

    // --- Bierball ---
    void UpdateBierball(double dt, GameState gs, List<Vector2> bins)
    {
        if (gs.ActiveEvent != "bierball") { Bierball = null; return; }
        var g = Bierball ??= new BierballGame { Throwing = _rng.Next(2) };
        var c = EventCenter;

        var guests = new List<Visitor>();
        foreach (var v in Visitors)
            if (v.EventId == "bierball" && v.State == VisitorState.Sitzt) guests.Add(v);

        bool drinking = g.Phase >= BierballGame.Step.Hinlaufen;
        foreach (var v in guests)
        {
            float lungeTarget = v == g.Thrower && g.Phase == BierballGame.Step.Ausholen ? 1 : 0;
            v.Lunge = Mathf.MoveToward(v.Lunge, lungeTarget, (float)dt * 5);
            bool drinks = drinking && v.Team == g.Throwing;
            v.DrinkAnim = Mathf.MoveToward(v.DrinkAnim, drinks ? 1 : 0, (float)dt * 4);
            if (drinks && v.DrinkAnim >= 1)
            {
                v.DrinkProgress += dt * Balance.BierballDrinkRate;
                if (v.DrinkProgress >= 1)
                {
                    v.DrinkProgress -= 1;
                    DropBottle(v.Pos, bins, gs, BottleType.Bier);
                }
            }
        }
        g.Wobble = Mathf.MoveToward(g.Wobble, 0, (float)dt * 1.5f);

        switch (g.Phase)
        {
            case BierballGame.Step.Warten:
                if (guests.Count < 4) return;
                g.Timer -= dt;
                if (g.Timer > 0) return;
                var throwers = guests.FindAll(v => v.Team == g.Throwing);
                if (throwers.Count == 0) { NextTurn(g); return; }
                g.Thrower = throwers[_rng.Next(throwers.Count)];
                g.Phase = BierballGame.Step.Ausholen;
                g.Timer = 0.7;
                break;

            case BierballGame.Step.Ausholen:
                g.Timer -= dt;
                if (g.Timer > 0) return;
                // Der Werfer schleudert eine leere Flasche auf die Zielflasche
                g.Hit = _rng.NextDouble() < Balance.BierballHitChance;
                g.FlyFrom = g.Thrower.Pos + new Vector2(g.Thrower.Team == 0 ? 10 : -10, -6);
                g.FlyTo = g.Hit ? c : c + new Vector2(Rf(-12, 12), Rf(18, 30) * (_rng.Next(2) == 0 ? -1 : 1));
                g.Phase = BierballGame.Step.Fliegt;
                g.Timer = BierballGame.FlyTime;
                break;

            case BierballGame.Step.Fliegt:
                g.Timer -= dt;
                if (g.Timer > 0) return;
                // Die Wurfflasche bleibt liegen und kann eingesammelt werden
                AddGroundBottle(g.FlyTo + new Vector2(Rf(-14, 14), Rf(10, 18)), BottleType.Bier);
                var runners = guests.FindAll(v => v.Team != g.Throwing);
                if (g.Hit && runners.Count > 0)
                {
                    g.BottleDown = true;
                    g.Runner = runners[_rng.Next(runners.Count)];
                    g.Phase = BierballGame.Step.Hinlaufen;
                    PendingFloats.Add((c + new Vector2(-26, -20), "TREFFER!", Palette.MoneyText));
                }
                else
                {
                    g.Wobble = 1;
                    g.Phase = BierballGame.Step.Daneben;
                    g.Timer = 0.9;
                }
                break;

            case BierballGame.Step.Daneben:
                g.Timer -= dt;
                if (g.Timer <= 0) NextTurn(g);
                break;

            case BierballGame.Step.Hinlaufen:
                if (!guests.Contains(g.Runner)) { g.BottleDown = false; NextTurn(g); break; }
                g.Runner.WalkPhase += (float)dt * 14;
                if (MoveTowards(ref g.Runner.Pos, c + new Vector2(g.Runner.Team == 0 ? -12 : 12, 0), Balance.BierballRunSpeed * dt))
                {
                    g.Phase = BierballGame.Step.Aufstellen;
                    g.Timer = 0.6;
                }
                break;

            case BierballGame.Step.Aufstellen:
                g.Timer -= dt;
                if (g.Timer > 0) return;
                g.BottleDown = false;
                g.Phase = BierballGame.Step.Zuruecklaufen;
                break;

            case BierballGame.Step.Zuruecklaufen:
                if (!guests.Contains(g.Runner)) { NextTurn(g); break; }
                g.Runner.WalkPhase += (float)dt * 14;
                if (MoveTowards(ref g.Runner.Pos, g.Runner.Target, Balance.BierballRunSpeed * dt)) NextTurn(g);
                break;
        }
    }

    // --- Männertag ---
    void UpdateMaennertag(double dt, GameState gs, List<Rect2> paths)
    {
        if (gs.ActiveEvent != "maennertag") { _mtGroups = 0; _mtTimer = 0; return; }
        if (_mtGroups >= Balance.MaennertagGroups) return;
        _mtTimer -= dt;
        if (_mtTimer > 0) return;
        _mtTimer = Balance.MaennertagGroupInterval;
        _mtGroups++;

        // Route: drei Halte quer durch den Park
        var start = EdgePoint(paths);
        var route = new List<Vector2>();
        for (int k = 0; k < 3; k++)
            route.Add(new Vector2(Rf(60, Size.X - 60), Rf(60, Size.Y - 60)));
        var exit = EdgePoint(paths);

        Visitor leader = null;
        var offsets = new[] { Vector2.Zero, new Vector2(-20, -14), new Vector2(20, -14), new Vector2(0, -26) };
        int n = 3 + _rng.Next(2);
        for (int i = 0; i < n; i++)
        {
            var g = NewVisitor(paths);
            g.EventId = "maennertag";
            g.State = VisitorState.Wandert;
            g.Pos = start + offsets[i];
            g.Exit = exit;
            g.FollowOffset = offsets[i];
            g.DrinkTimer = R(1, 3);
            if (leader == null) { leader = g; g.Wagon = true; g.Waypoints = route; }
            else g.Leader = leader;
            Visitors.Add(g);
        }
    }

    void UpdateWanderer(Visitor v, double dt, List<Vector2> bins, GameState gs)
    {
        bool pausing;
        if (v.Leader == null)
        {
            // Anführer zieht den Bollerwagen von Halt zu Halt
            pausing = v.PauseTimer > 0;
            if (pausing) v.PauseTimer -= dt;
            else if (v.WaypointIdx >= v.Waypoints.Count) { v.State = VisitorState.Geht; return; }
            else if (Walk(v, v.Waypoints[v.WaypointIdx], dt))
            {
                v.WaypointIdx++;
                v.PauseTimer = Balance.MaennertagStopSeconds;
            }
        }
        else
        {
            var l = v.Leader;
            if (!Visitors.Contains(l) || l.State == VisitorState.Geht) { v.State = VisitorState.Geht; v.Exit = l.Exit; return; }
            pausing = l.PauseTimer > 0;
            if (!MoveTowards(ref v.Pos, l.Pos + v.FollowOffset, Balance.WalkSpeed * 1.2 * dt)) v.WalkPhase += (float)dt * 10;
        }

        // Beim Halt wird getrunken, unterwegs fällt auch mal eine Flasche aus dem Wagen
        v.DrinkTimer -= dt;
        if (pausing && v.DrinkTimer <= 0)
        {
            DropBottle(v.Pos, bins, gs, BottleFor(v, gs));
            v.DrinkTimer = R(1.2, 2.2);
        }
        else if (!pausing && _rng.NextDouble() < Balance.MaennertagDropPerSecondWalking * dt)
            DropBottle(v.Pos, bins, gs, BottleFor(v, gs));
        v.DrinkAnim = Mathf.MoveToward(v.DrinkAnim, pausing ? 1 : 0, (float)dt * 3);
    }

    void NextTurn(BierballGame g)
    {
        g.Throwing = 1 - g.Throwing;
        g.Phase = BierballGame.Step.Warten;
        g.Timer = R(0.6, 1.2);
        g.Thrower = null;
        g.Runner = null;
    }

    bool Walk(Visitor v, Vector2 target, double dt)
    {
        v.WalkPhase += (float)dt * 10;
        return MoveTowards(ref v.Pos, target, Balance.WalkSpeed * dt);
    }

    static bool MoveTowards(ref Vector2 pos, Vector2 target, double step)
    {
        var d = target - pos;
        if (d.Length() <= step) { pos = target; return true; }
        pos += d.Normalized() * (float)step;
        return false;
    }

    void Seed(GameState gs, List<Rect2> paths, List<Vector2> benches, List<Vector2> tables)
    {
        _seeded = true;
        // Beim ersten Spielen ist die Wiese leer, der erste Gast kommt erst in der Einführung
        if (gs.TutorialActive) return;
        if (gs.BottlesFed == 0 && gs.Bag.Count == 0)
        {
            for (int i = 0; i < Balance.StartBottles; i++)
                AddGroundBottle(new Vector2(Rf(40, Size.X - 40), Rf(40, Size.Y - 40)), RandomType(gs));
        }
        Spawn(gs, paths, benches, tables, true);
        Spawn(gs, paths, benches, tables, true);
    }

    void SpawnTutorialGuest(List<Rect2> paths)
    {
        _tutGuestSpawned = true;
        var v = NewVisitor(paths);
        v.Tutorial = true;
        // oben in der Mitte, damit Kalles Sprechblase unten nichts verdeckt
        v.Target = new Vector2(Size.X * 0.5f, Size.Y * 0.36f);
        v.Pos = new Vector2(Size.X * 0.5f + Size.X * 0.45f, Size.Y * 0.36f + 30);
        v.Exit = new Vector2(-30, Size.Y * 0.3f);
        Visitors.Add(v);
    }

    Visitor NewVisitor(List<Rect2> paths)
    {
        return new Visitor
        {
            Shirt = Palette.Shirts[_rng.Next(Palette.Shirts.Length)],
            Hair = Palette.Hair[_rng.Next(Palette.Hair.Length)],
            BlanketColor = Palette.Blankets[_rng.Next(Palette.Blankets.Length)],
            BlanketStyle = _rng.Next(Sprites.BlanketStyles),
            Pos = EdgePoint(paths),
            Exit = EdgePoint(paths),
            State = VisitorState.Kommt,
        };
    }

    void Spawn(GameState gs, List<Rect2> paths, List<Vector2> benches, List<Vector2> tables, bool alreadySitting)
    {
        var v = NewVisitor(paths);
        v.WantsSnack = gs.Level("imbiss") > 0
            && _rng.NextDouble() < Balance.ImbissVisitChance + gs.ImbissVisitBonus;
        v.SnackSpot = ParkLayout.ImbissFront(Size) + new Vector2(Rf(-50, 50), Rf(0, 16));

        // Platz suchen: Tischtennis, Bank oder Decke auf der Wiese
        int freeTable = FreeTableSeat(tables);
        int freeSeat = FreeSeat(benches);
        bool wantsGroup = false;
        if (freeTable >= 0 && _rng.NextDouble() < 0.5)
        {
            v.TableSeat = freeTable;
            v.Target = TableSpot(tables, freeTable);
        }
        else if (freeSeat >= 0 && _rng.NextDouble() < 0.6)
        {
            v.Seat = freeSeat;
            v.Target = SeatSpot(benches, freeSeat);
        }
        else
        {
            // Ohne Picknickdecken stehen die Leute nur auf der Wiese herum
            int picknick = gs.Level("picknick");
            v.HasBlanket = picknick > 0
                && _rng.NextDouble() < Balance.BlanketChanceBase + Balance.BlanketChancePerLevel * (picknick - 1);
            var spot = FreeGrassSpot(paths, benches, tables);
            if (v.HasBlanket)
            {
                v.BlanketPos = spot;
                v.Target = v.BlanketPos + new Vector2(Rf(-12, 12), Rf(-4, 4));
                // Am Feiertag kommen viele Familien und Freundesgruppen
                double groupChance = Balance.GroupChancePerLevel * gs.Level("gruppen") + (gs.ActiveEvent == "feiertag" ? 0.5 : 0);
                wantsGroup = _rng.NextDouble() < groupChance;
            }
            else v.Target = spot;
        }

        if (alreadySitting) SitDown(v);
        Visitors.Add(v);

        // Freundeskreis: weitere Leute kommen mit auf dieselbe Decke
        if (wantsGroup)
        {
            if (_rng.NextDouble() < gs.GroupCrateChance && CrateSpot(v.BlanketPos, paths, benches, tables, gs) is { } cp)
            {
                v.Crate = new GroupCrate { Pos = cp, Owner = v };
                Crates.Add(v.Crate);
            }
            int friends = 1 + _rng.Next(Balance.GroupExtraMax + gs.Level("gruppen"));
            var offsets = new[] { new Vector2(-18, -8), new Vector2(18, -8), new Vector2(-18, 10), new Vector2(18, 10) };
            for (int f = 0; f < friends && f < offsets.Length; f++)
            {
                var friend = NewVisitor(paths);
                friend.Pos = v.Pos + new Vector2(Rf(-16, 16), Rf(-16, 16));
                friend.Exit = v.Exit;
                friend.Leader = v;
                friend.Target = v.BlanketPos + offsets[f];
                friend.WantsSnack = v.WantsSnack;
                friend.SnackSpot = v.SnackSpot + new Vector2(Rf(-16, 16), 0);
                if (alreadySitting) SitDown(friend);
                Visitors.Add(friend);
            }
        }
    }

    void SitDown(Visitor v)
    {
        v.Pos = v.Target;
        v.WantsSnack = false;
        v.BlanketOpen = 1;
        v.State = VisitorState.Sitzt;
        v.StayTimer = Balance.StayBase * R(0.5, 1.0);
        v.DrinkTimer = Balance.DrinkInterval * R(0.3, 1.0);
    }

    // --- Events ---
    void UpdateEventGuests(GameState gs)
    {
        if (gs.ActiveEvent == null) { _eventSpawnedFor = null; return; }
        if (_eventSpawnedFor == gs.ActiveEvent) return;
        _eventSpawnedFor = gs.ActiveEvent;
        var def = EventDefs.Get(gs.ActiveEvent);
        if (def == null) return;
        var paths = ParkLayout.Paths(Size, gs);
        var spots = EventSpots(def);
        foreach (var spot in spots)
        {
            var g = NewVisitor(paths);
            g.EventId = def.Id;
            g.Target = spot;
            if (def.Kind == EventKind.Bierball) g.Team = spot.X < EventCenter.X ? 0 : 1;
            g.Pos = g.Pos.Lerp(spot, 0.3f);
            Visitors.Add(g);
        }
    }

    public List<Vector2> EventSpots(EventDef def)
    {
        var c = EventCenter;
        var list = new List<Vector2>();
        switch (def.Kind)
        {
            case EventKind.Geburtstag:
                for (int i = 0; i < def.Guests; i++)
                {
                    float a = i / (float)def.Guests * Mathf.Tau;
                    list.Add(c + new Vector2(Mathf.Cos(a) * 46, Mathf.Sin(a) * 34));
                }
                break;
            case EventKind.Bierball:
                for (int i = 0; i < def.Guests; i++)
                {
                    bool teamA = i % 2 == 0;
                    list.Add(c + new Vector2(teamA ? -70 : 70, -36 + (i / 2) * 24));
                }
                break;
            case EventKind.Oktoberfest:
                // Zwei lange Tische vor dem Festzelt, Gäste auf beiden Seiten
                for (int i = 0; i < def.Guests; i++)
                {
                    int table = i / 10, side = i / 5 % 2, seat = i % 5; // je Tisch 10 Plätze
                    float tableY = c.Y + ParkLayout.OktoberfestTableY + table * ParkLayout.OktoberfestTableGap;
                    list.Add(new Vector2(c.X - 64 + seat * 32, tableY + (side == 0 ? -8 : 20)));
                }
                break;
            case EventKind.Feiertag:
            case EventKind.Maennertag:
                break; // keine festen Plätze
            default: // Konzert: Publikum vor der Bühne
                for (int i = 0; i < def.Guests; i++)
                    list.Add(c + new Vector2(-72 + (i % 6) * 29 + (i / 6 % 2) * 12, -6 + (i / 6) * 26));
                break;
        }
        return list;
    }

    public Vector2 EventCenter => ParkLayout.EventCenter(Size);

    // --- Plätze ---
    int FreeSeat(List<Vector2> benches)
    {
        var gs = GameState.I;
        for (int seat = 0; seat < benches.Count * 4; seat++)
        {
            if (seat % 4 >= gs.BenchSeats(seat / 4)) continue;
            bool taken = false;
            foreach (var v in Visitors) if (v.Seat == seat) { taken = true; break; }
            if (!taken) return seat;
        }
        return -1;
    }

    /// <summary>Plätze 0/1 auf der Bank, 2/3 auf der zweiten Bank gegenüber am Tisch.</summary>
    public static Vector2 SeatSpot(List<Vector2> benches, int seat)
    {
        var bench = benches[seat / 4];
        int k = seat % 4;
        return bench + new Vector2(k % 2 == 0 ? -14 : 14, 0) + (k >= 2 ? ParkLayout.BenchOpposite(seat / 4) : Vector2.Zero);
    }

    int FreeTableSeat(List<Vector2> tables)
    {
        for (int seat = 0; seat < tables.Count * 2; seat++)
        {
            bool taken = false;
            foreach (var v in Visitors) if (v.TableSeat == seat) { taken = true; break; }
            if (!taken) return seat;
        }
        return -1;
    }

    static Vector2 TableSpot(List<Vector2> tables, int seat)
        => tables[seat / 2] + new Vector2(0, seat % 2 == 0 ? -ParkLayout.TableSize.Y / 2 - 10 : ParkLayout.TableSize.Y / 2 + 10);

    /// <summary>Ist an Platte i gerade ein Spiel im Gange (beide Spieler da)?</summary>
    public bool TableActive(int table)
    {
        int n = 0;
        foreach (var v in Visitors)
            if (v.State == VisitorState.Sitzt && v.TableSeat >= 0 && v.TableSeat / 2 == table) n++;
        return n >= 2;
    }

    Vector2 FreeGrassSpot(List<Rect2> paths, List<Vector2> benches, List<Vector2> tables)
    {
        Vector2 best = new(Size.X / 2, Size.Y / 4);
        var gs = GameState.I;
        for (int tries = 0; tries < 24; tries++)
        {
            var p = new Vector2(Rf(50, Size.X - 50), Rf(50, Size.Y - 50));
            bool ok = true;
            foreach (var r in paths) if (r.Grow(30).HasPoint(p)) { ok = false; break; }
            if (ok) foreach (var b in benches) if (b.DistanceTo(p) < 60) { ok = false; break; }
            if (ok) foreach (var t in tables) if (t.DistanceTo(p) < 80) { ok = false; break; }
            if (ok && gs.Level("imbiss") > 0 && ParkLayout.ImbissRect(Size).Grow(50).HasPoint(p)) ok = false;
            if (ok && EventCenter.DistanceTo(p) < 130) ok = false;
            if (ok && ParkLayout.Depot(Size).DistanceTo(p) < 60) ok = false;
            if (ok && gs.Level("grill") > 0 && ParkLayout.Grill(Size).DistanceTo(p) < 100) ok = false;
            if (ok) foreach (var v in Visitors) if (v.HasBlanket && v.BlanketPos.DistanceTo(p) < 70) { ok = false; break; }
            if (ok) return p;
            best = p;
        }
        return best;
    }

    Vector2 EdgePoint(List<Rect2> paths)
    {
        // Mit Wegen kommen die meisten über einen Weg
        if (paths.Count > 0 && _rng.NextDouble() < 0.7)
        {
            var r = paths[_rng.Next(paths.Count)];
            bool horizontal = r.Size.X > r.Size.Y;
            if (horizontal)
            {
                float y = r.GetCenter().Y + Rf(-10, 10);
                if (r.Position.X <= 1) return new Vector2(-12, y);
                return new Vector2(Size.X + 12, y);
            }
            else
            {
                float x = r.GetCenter().X + Rf(-10, 10);
                if (r.Position.Y <= 1) return new Vector2(x, -12);
                return new Vector2(x, Size.Y + 12);
            }
        }
        return _rng.Next(4) switch
        {
            0 => new Vector2(Rf(0, Size.X), -12),
            1 => new Vector2(Rf(0, Size.X), Size.Y + 12),
            2 => new Vector2(-12, Rf(0, Size.Y)),
            _ => new Vector2(Size.X + 12, Rf(0, Size.Y)),
        };
    }

    // --- Flaschen ---
    BottleType RandomType(GameState gs)
    {
        if (_rng.NextDouble() < gs.GoldChance) return BottleType.Gold;
        if (_rng.NextDouble() < gs.RetroChance) return BottleType.Retro;
        // Nur freigeschaltete Sorten, nach Gewicht (am Anfang also nur Bier)
        var types = new[] { BottleType.Bier, BottleType.Wasser, BottleType.Plastik, BottleType.Dose };
        double total = 0;
        foreach (var t in types) total += gs.SpawnWeight(t);
        double r = _rng.NextDouble() * total;
        foreach (var t in types)
        {
            r -= gs.SpawnWeight(t);
            if (r <= 0) return t;
        }
        return BottleType.Bier;
    }

    void DropBottle(Vector2 near, List<Vector2> bins, GameState gs, BottleType type)
    {
        _dropTimes.Enqueue(_clock);

        // Mit Mülleimern landen manche Flaschen im Eimer (seltene nie – die sieht man gern liegen)
        if (bins.Count > 0 && type != BottleType.Gold && type != BottleType.Retro)
        {
            double chance = Math.Min(Balance.BinChanceMax + 0.1, (Balance.BinChancePerBin + gs.BinRingChance) * bins.Count);
            if (_rng.NextDouble() < chance)
            {
                int best = -1; float bestD = float.MaxValue;
                for (int i = 0; i < bins.Count; i++)
                {
                    if (BinContents[i].Count >= gs.BinCapacity(i)) continue;
                    float d = bins[i].DistanceTo(near);
                    if (d < bestD) { bestD = d; best = i; }
                }
                if (best >= 0) { BinContents[best].Add(type); return; }
            }
        }

        var offset = new Vector2(Rf(-26, 26), Rf(10, 26) * (_rng.Next(2) == 0 ? -1 : 1));
        AddGroundBottle(near + offset, type);
    }

    void AddGroundBottle(Vector2 pos, BottleType type)
    {
        if (Bottles.Count >= Balance.MaxGroundBottles) return;
        pos.X = Mathf.Clamp(pos.X, 12, Size.X - 12);
        pos.Y = Mathf.Clamp(pos.Y, 12, Size.Y - 12);
        Bottles.Add(new GroundBottle
        {
            Pos = pos,
            Type = type,
            Rot = Rf(-1.6f, 1.6f),
            ColorIdx = _rng.Next(4),
        });
    }

    // --- Fremde Sammler ---
    /// <summary>Liegt eine Flasche zu lange, kommt jemand vom nächsten Rand und nimmt sie mit (Eimer und Kästen nicht).</summary>
    void UpdateStrangers(double dt, GameState gs)
    {
        if (!gs.TutorialActive)
        {
            double life = gs.BottleLifetime;
            foreach (var b in Bottles)
            {
                if (b.Claimed || b.Age < life) continue;
                b.Claimed = true;
                var edge = NearestEdge(b.Pos);
                Strangers.Add(new Stranger { Pos = edge, Exit = edge, Target = b });
            }
        }

        double step = Balance.WalkSpeed * 1.3 * dt;
        foreach (var s in Strangers)
        {
            s.WalkPhase += (float)dt * 10;
            if (!s.Leaving)
            {
                // Schon weg (der Spieler war schneller): umdrehen
                if (!Bottles.Contains(s.Target)) { s.Leaving = true; continue; }
                if (MoveTowards(ref s.Pos, s.Target.Pos, step))
                {
                    Bottles.Remove(s.Target);
                    s.Carrying = true;
                    s.Leaving = true;
                    if (!gs.BottleTaken) { gs.BottleTaken = true; gs.TakenAt = s.Pos; gs.Notify(); }
                }
            }
            else MoveTowards(ref s.Pos, s.Exit, step);
        }
        Strangers.RemoveAll(s => s.Leaving && s.Pos.DistanceTo(s.Exit) < 1);
    }

    Vector2 NearestEdge(Vector2 p)
    {
        float l = p.X, r = Size.X - p.X, t = p.Y, b = Size.Y - p.Y;
        float m = Math.Min(Math.Min(l, r), Math.Min(t, b));
        if (m == l) return new Vector2(-20, p.Y);
        if (m == r) return new Vector2(Size.X + 20, p.Y);
        if (m == t) return new Vector2(p.X, -20);
        return new Vector2(p.X, Size.Y + 20);
    }

    // --- Sammler ---
    void UpdateCollectors(double dt, GameState gs, List<Vector2> bins)
    {
        var depot = ParkLayout.Depot(Size);
        int wanted = gs.Level("sammler");
        while (Collectors.Count < wanted)
            Collectors.Add(new Collector { Pos = depot + new Vector2(Rf(0, 20), Rf(-20, 20)) });

        double step = gs.CollectorSpeed * dt;
        foreach (var c in Collectors)
        {
            c.WalkPhase += (float)dt * 10;
            switch (c.State)
            {
                case CollectorState.Sucht:
                    if (c.Carry.Count >= gs.CollectorCarry) { c.State = CollectorState.Abliefern; break; }
                    var b = NearestFreeBottle(c.Pos);
                    if (b != null) { b.Claimed = true; c.TargetBottle = b; c.State = CollectorState.ZurFlasche; break; }
                    int bin = NearestFilledBin(c.Pos, bins);
                    if (bin >= 0) { c.TargetBin = bin; c.State = CollectorState.ZumEimer; break; }
                    if (c.Carry.Count > 0) { c.State = CollectorState.Abliefern; break; }
                    c.State = CollectorState.Wartet;
                    c.Wait = R(1, 2);
                    c.Target = depot + new Vector2(Rf(10, 60), Rf(-50, 50));
                    break;

                case CollectorState.ZurFlasche:
                    if (c.TargetBottle == null || !Bottles.Contains(c.TargetBottle)) { c.State = CollectorState.Sucht; break; }
                    if (MoveTowards(ref c.Pos, c.TargetBottle.Pos, step))
                    {
                        Bottles.Remove(c.TargetBottle);
                        c.Carry.Add(c.TargetBottle.Type);
                        c.TargetBottle = null;
                        c.State = CollectorState.Sucht;
                    }
                    break;

                case CollectorState.ZumEimer:
                    if (c.TargetBin < 0 || c.TargetBin >= bins.Count) { c.State = CollectorState.Sucht; break; }
                    if (MoveTowards(ref c.Pos, bins[c.TargetBin] + new Vector2(0, 16), step))
                    {
                        var content = BinContents[c.TargetBin];
                        while (content.Count > 0 && c.Carry.Count < gs.CollectorCarry)
                        {
                            c.Carry.Add(content[0]);
                            content.RemoveAt(0);
                        }
                        c.TargetBin = -1;
                        c.State = CollectorState.Sucht;
                    }
                    break;

                case CollectorState.Abliefern:
                    if (MoveTowards(ref c.Pos, depot + new Vector2(16, 0), step))
                    {
                        double sum = 0;
                        int caps = 0;
                        foreach (var t in c.Carry)
                        {
                            var (v, cap) = gs.FeedDirect(t, _rng, byCollector: true);
                            gs.CollectedByCollectors++;
                            sum += v;
                            if (cap >= 0) caps++;
                        }
                        c.Carry.Clear();
                        PendingFloats.Add((depot + new Vector2(24, -10), "+" + GameState.Euro(sum).Replace("€ ", ""), Palette.GoodText));
                        if (caps > 0) PendingFloats.Add((depot + new Vector2(24, -30), "+ Kronkorken!", Palette.MoneyText));
                        gs.Notify();
                        c.State = CollectorState.Sucht;
                    }
                    break;

                case CollectorState.Wartet:
                    MoveTowards(ref c.Pos, c.Target, step * 0.5);
                    c.Wait -= dt;
                    if (c.Wait <= 0) c.State = CollectorState.Sucht;
                    break;
            }
        }
    }

    GroundBottle NearestFreeBottle(Vector2 from)
    {
        GroundBottle best = null; float bestD = float.MaxValue;
        foreach (var b in Bottles)
        {
            if (b.Claimed || b.Age < 1.0f) continue; // dem Spieler einen Moment lassen
            float d = b.Pos.DistanceSquaredTo(from);
            if (d < bestD) { bestD = d; best = b; }
        }
        return best;
    }

    int NearestFilledBin(Vector2 from, List<Vector2> bins)
    {
        int best = -1; float bestD = float.MaxValue;
        for (int i = 0; i < bins.Count && i < BinContents.Count; i++)
        {
            if (BinContents[i].Count < 3) continue;
            bool claimed = false;
            foreach (var c in Collectors) if (c.TargetBin == i) { claimed = true; break; }
            if (claimed) continue;
            float d = bins[i].DistanceSquaredTo(from);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    // --- Klick-Helfer ---
    /// <summary>Flasche nahe pos. Gibt die Flasche zurück oder null.</summary>
    public GroundBottle BottleAt(Vector2 pos, float radius = 18)
    {
        GroundBottle best = null; float bestD = radius;
        foreach (var b in Bottles)
        {
            float d = b.Pos.DistanceTo(pos);
            if (d < bestD) { bestD = d; best = b; }
        }
        return best;
    }

    /// <summary>Alle Flaschen im Umkreis, nach Abstand sortiert.</summary>
    public List<GroundBottle> BottlesAround(Vector2 pos, float radius)
    {
        var list = new List<GroundBottle>();
        foreach (var b in Bottles) if (b.Pos.DistanceTo(pos) < radius) list.Add(b);
        list.Sort((a, b) => a.Pos.DistanceSquaredTo(pos).CompareTo(b.Pos.DistanceSquaredTo(pos)));
        return list;
    }

    public int BinAt(Vector2 pos, List<Vector2> bins)
    {
        for (int i = 0; i < bins.Count; i++)
            if (bins[i].DistanceTo(pos) < 18) return i;
        return -1;
    }
}
