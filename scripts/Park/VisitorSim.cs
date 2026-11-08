using System;
using System.Collections.Generic;
using Godot;
using Pfand.Core;

namespace Pfand.Park;

public enum VisitorState { Kommt, Sitzt, Geht }

public class Visitor
{
    public Vector2 Pos, Target, Exit;
    public VisitorState State;
    public double StayTimer, DrinkTimer;
    public Color Shirt, Hair, BlanketColor;
    public bool HasBlanket;
    public Vector2 BlanketPos;
    public int Seat = -1;          // Bankplatz (Bank * 2 + Seite), -1 = Decke
    public float WalkPhase;
}

public class GroundBottle
{
    public Vector2 Pos;
    public BottleType Type;
    public float Rot;
    public int ColorIdx;
    public float Age;
}

/// <summary>
/// Besucher-Simulation: Leute kommen, setzen sich, trinken, lassen leere Flaschen liegen und gehen wieder.
/// Arbeitet in Pixel-Koordinaten des Spielfelds.
/// </summary>
public class VisitorSim
{
    public readonly List<Visitor> Visitors = new();
    public readonly List<GroundBottle> Bottles = new();
    public readonly List<List<BottleType>> BinContents = new();
    public Vector2 Size;

    readonly Random _rng = new();
    readonly Queue<double> _dropTimes = new();
    double _clock, _spawnAcc;
    bool _seeded;

    public int BottlesPerMinute => _dropTimes.Count;

    double R(double a, double b) => a + _rng.NextDouble() * (b - a);
    float Rf(float a, float b) => (float)R(a, b);

    public void Update(double dt, GameState gs)
    {
        if (Size.X < 50 || Size.Y < 50) return;
        _clock += dt;
        while (_dropTimes.Count > 0 && _clock - _dropTimes.Peek() > 60) _dropTimes.Dequeue();

        gs.TimeOfDay = (gs.TimeOfDay + dt / Balance.DayLengthSeconds) % 1.0;

        var paths = ParkLayout.Paths(Size, gs);
        var bins = ParkLayout.Bins(Size, gs);
        var benches = ParkLayout.Benches(Size, gs);
        int lamps = gs.Level("laterne");
        while (BinContents.Count < bins.Count) BinContents.Add(new List<BottleType>());

        if (!_seeded) Seed(gs, paths, benches);

        // --- Neue Besucher ---
        double lampFactor = lamps / 3.0;
        double phaseFactor = gs.Phase switch
        {
            DayPhase.Tag => 1.0,
            DayPhase.Abend => 0.5 + 0.5 * lampFactor,
            _ => lampFactor,
        };
        int maxVisitors = Balance.MaxVisitorsBase + Balance.MaxVisitorsPerPath * paths.Count + Balance.MaxVisitorsPerBench * benches.Count;
        double interval = Balance.SpawnIntervalBase / (1 + Balance.SpawnBonusPerPath * paths.Count + Balance.SpawnBonusPerBin * bins.Count);
        if (phaseFactor > 0)
        {
            _spawnAcc += dt * phaseFactor;
            if (_spawnAcc >= interval)
            {
                _spawnAcc = 0;
                if (Visitors.Count < maxVisitors) Spawn(gs, paths, benches, false);
            }
        }

        // --- Besucher bewegen ---
        for (int i = Visitors.Count - 1; i >= 0; i--)
        {
            var v = Visitors[i];
            switch (v.State)
            {
                case VisitorState.Kommt:
                    if (Walk(v, v.Target, dt))
                    {
                        v.State = VisitorState.Sitzt;
                        v.StayTimer = Balance.StayBase * (1 + Balance.StayBonusPerBench * benches.Count) * R(0.7, 1.3);
                        v.DrinkTimer = Balance.DrinkInterval * R(0.6, 1.2);
                    }
                    break;

                case VisitorState.Sitzt:
                    v.StayTimer -= dt;
                    v.DrinkTimer -= dt;
                    if (v.DrinkTimer <= 0)
                    {
                        DropBottle(v.Pos, bins, gs);
                        v.DrinkTimer = Balance.DrinkInterval * R(0.8, 1.2);
                    }
                    bool leave = v.StayTimer <= 0
                        || (gs.Phase == DayPhase.Nacht && lamps == 0)
                        || (gs.Phase == DayPhase.Abend && lamps == 0 && _rng.NextDouble() < Balance.EveningLeaveChancePerSecond * dt);
                    if (leave)
                    {
                        if (_rng.NextDouble() < 0.5) DropBottle(v.Pos, bins, gs);
                        v.State = VisitorState.Geht;
                        v.Seat = -1;
                    }
                    break;

                case VisitorState.Geht:
                    if (Walk(v, v.Exit, dt)) Visitors.RemoveAt(i);
                    break;
            }
        }

        foreach (var b in Bottles) b.Age += (float)dt;
    }

    bool Walk(Visitor v, Vector2 target, double dt)
    {
        var d = target - v.Pos;
        float step = (float)(Balance.WalkSpeed * dt);
        v.WalkPhase += (float)dt * 10;
        if (d.Length() <= step) { v.Pos = target; return true; }
        v.Pos += d.Normalized() * step;
        return false;
    }

    void Seed(GameState gs, List<Rect2> paths, List<Vector2> benches)
    {
        _seeded = true;
        if (gs.BottlesFed == 0 && gs.Bag.Count == 0)
        {
            for (int i = 0; i < Balance.StartBottles; i++)
                AddGroundBottle(new Vector2(Rf(40, Size.X - 40), Rf(40, Size.Y - 40)));
        }
        Spawn(gs, paths, benches, true);
        Spawn(gs, paths, benches, true);
    }

    void Spawn(GameState gs, List<Rect2> paths, List<Vector2> benches, bool alreadySitting)
    {
        var v = new Visitor
        {
            Shirt = Palette.Shirts[_rng.Next(Palette.Shirts.Length)],
            Hair = Palette.Hair[_rng.Next(Palette.Hair.Length)],
            BlanketColor = Palette.Blankets[_rng.Next(Palette.Blankets.Length)],
            Pos = EdgePoint(paths),
            Exit = EdgePoint(paths),
            State = VisitorState.Kommt,
        };

        // Platz suchen: lieber Bank, sonst Decke auf der Wiese
        int freeSeat = FreeSeat(benches);
        if (freeSeat >= 0 && _rng.NextDouble() < 0.6)
        {
            v.Seat = freeSeat;
            var bench = benches[freeSeat / 2];
            v.Target = bench + new Vector2(freeSeat % 2 == 0 ? -14 : 14, 0);
        }
        else
        {
            v.HasBlanket = true;
            v.BlanketPos = FreeGrassSpot(paths, benches);
            v.Target = v.BlanketPos + new Vector2(Rf(-12, 12), Rf(-4, 4));
        }

        if (alreadySitting)
        {
            v.Pos = v.Target;
            v.State = VisitorState.Sitzt;
            v.StayTimer = Balance.StayBase * R(0.5, 1.0);
            v.DrinkTimer = Balance.DrinkInterval * R(0.3, 1.0);
        }
        Visitors.Add(v);
    }

    int FreeSeat(List<Vector2> benches)
    {
        for (int seat = 0; seat < benches.Count * 2; seat++)
        {
            bool taken = false;
            foreach (var v in Visitors) if (v.Seat == seat) { taken = true; break; }
            if (!taken) return seat;
        }
        return -1;
    }

    Vector2 FreeGrassSpot(List<Rect2> paths, List<Vector2> benches)
    {
        Vector2 best = new(Size.X / 2, Size.Y / 4);
        for (int tries = 0; tries < 20; tries++)
        {
            var p = new Vector2(Rf(50, Size.X - 50), Rf(50, Size.Y - 50));
            bool ok = true;
            foreach (var r in paths) if (r.Grow(30).HasPoint(p)) { ok = false; break; }
            if (ok) foreach (var b in benches) if (b.DistanceTo(p) < 60) { ok = false; break; }
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

    BottleType RandomType()
    {
        double r = _rng.NextDouble();
        if (r < 0.35) return BottleType.Bier;
        if (r < 0.65) return BottleType.Dose;
        if (r < 0.85) return BottleType.Plastik;
        return BottleType.Wasser;
    }

    void DropBottle(Vector2 near, List<Vector2> bins, GameState gs)
    {
        _dropTimes.Enqueue(_clock);
        var type = RandomType();

        // Mit Mülleimern landen manche Flaschen im Eimer
        if (bins.Count > 0)
        {
            double chance = Math.Min(Balance.BinChanceMax, Balance.BinChancePerBin * bins.Count);
            if (_rng.NextDouble() < chance)
            {
                int best = -1; float bestD = float.MaxValue;
                for (int i = 0; i < bins.Count; i++)
                {
                    if (BinContents[i].Count >= Balance.BinCapacity) continue;
                    float d = bins[i].DistanceTo(near);
                    if (d < bestD) { bestD = d; best = i; }
                }
                if (best >= 0) { BinContents[best].Add(type); return; }
            }
        }

        var offset = new Vector2(Rf(-26, 26), Rf(10, 26) * (_rng.Next(2) == 0 ? -1 : 1));
        AddGroundBottle(near + offset, type);
    }

    void AddGroundBottle(Vector2 pos, BottleType? type = null)
    {
        if (Bottles.Count >= Balance.MaxGroundBottles) return;
        pos.X = Mathf.Clamp(pos.X, 12, Size.X - 12);
        pos.Y = Mathf.Clamp(pos.Y, 12, Size.Y - 12);
        Bottles.Add(new GroundBottle
        {
            Pos = pos,
            Type = type ?? RandomType(),
            Rot = Rf(-1.6f, 1.6f),
            ColorIdx = _rng.Next(4),
        });
    }

    /// <summary>Flasche nahe pos einsammeln. Gibt die Flasche zurück oder null.</summary>
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

    public int BinAt(Vector2 pos, List<Vector2> bins)
    {
        for (int i = 0; i < bins.Count; i++)
            if (bins[i].DistanceTo(pos) < 18) return i;
        return -1;
    }
}
