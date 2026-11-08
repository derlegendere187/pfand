using System;
using System.Collections.Generic;
using Godot;
using Pfand.Core;
using Pfand.Park;

namespace Pfand.UI;

/// <summary>Der Pfandautomat: zeigt die Tasche, wirft Flaschen ein und druckt einen Bon.</summary>
public partial class AutomatPanel : PanelContainer
{
    Label _display, _status, _bagLabel;
    BagGrid _bag;
    Button _feedButton;
    VBoxContainer _bonLines;
    Label _bonTitle;

    bool _feeding;
    double _feedAcc, _feedTotal;
    readonly Dictionary<BottleType, (int count, double sum)> _receipt = new();
    readonly List<int> _capsFound = new();
    readonly Random _rng = new();

    public override void _Ready()
    {
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeStyleboxOverride("panel", PfandTheme.Box(Palette.Machine, Palette.Border, 4, 10));

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        AddChild(col);

        var sign = PfandTheme.Panel(Palette.Leergut, null, 8);
        var signText = PfandTheme.Title("LEERGUT", 8, Palette.PanelLight);
        signText.HorizontalAlignment = HorizontalAlignment.Center;
        sign.AddChild(signText);
        col.AddChild(sign);

        var display = PfandTheme.Panel(Palette.Display, Palette.Muted, 8);
        var dcol = new VBoxContainer();
        dcol.AddThemeConstantOverride("separation", 0);
        _display = PfandTheme.Text(GameState.Euro(0), 34, Palette.DisplayText);
        _status = PfandTheme.Text("BEREIT", 20, Palette.Gold);
        dcol.AddChild(_display);
        dcol.AddChild(_status);
        display.AddChild(dcol);
        col.AddChild(display);

        var bagRow = new HBoxContainer();
        bagRow.AddChild(PfandTheme.Text("Tasche", 22));
        bagRow.AddChild(PfandTheme.Spacer());
        _bagLabel = PfandTheme.Text("0/8", 22);
        bagRow.AddChild(_bagLabel);
        col.AddChild(bagRow);

        _bag = new BagGrid();
        col.AddChild(_bag);

        _feedButton = new Button { Text = "EINWERFEN", CustomMinimumSize = new Vector2(0, 48) };
        _feedButton.Pressed += StartFeeding;
        col.AddChild(_feedButton);

        var bon = PfandTheme.Panel(Palette.Paper, null, 10);
        bon.SizeFlagsVertical = SizeFlags.ExpandFill;
        var bcol = new VBoxContainer();
        bcol.AddThemeConstantOverride("separation", 2);
        _bonTitle = PfandTheme.Title("PFANDBON", 8);
        _bonTitle.HorizontalAlignment = HorizontalAlignment.Center;
        bcol.AddChild(_bonTitle);
        _bonLines = new VBoxContainer();
        _bonLines.AddThemeConstantOverride("separation", 0);
        bcol.AddChild(_bonLines);
        bon.AddChild(bcol);
        col.AddChild(bon);
        ShowEmptyBon();

        GameState.I.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree() => GameState.I.Changed -= Refresh;

    void Refresh()
    {
        var gs = GameState.I;
        _bagLabel.Text = $"{gs.Bag.Count}/{gs.BagCapacity}";
        _bagLabel.AddThemeColorOverride("font_color", gs.BagFull ? Palette.BadText : Palette.Ink);
        _feedButton.Disabled = _feeding || gs.Bag.Count == 0;
        _bag.QueueRedraw();
        _bag.UpdateMinSize();
    }

    void StartFeeding()
    {
        if (_feeding || GameState.I.Bag.Count == 0) return;
        _feeding = true;
        _feedAcc = 0;
        _feedTotal = 0;
        _receipt.Clear();
        _capsFound.Clear();
        _status.Text = "ZÄHLT …";
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (!_feeding) return;
        var gs = GameState.I;
        _feedAcc += delta;
        bool changed = false;
        while (_feedAcc >= Balance.FeedInterval && gs.Bag.Count > 0)
        {
            _feedAcc -= Balance.FeedInterval;
            var (type, value, cap) = gs.FeedOne(_rng);
            _feedTotal += value;
            var e = _receipt.TryGetValue(type, out var cur) ? cur : (0, 0.0);
            _receipt[type] = (e.Item1 + 1, e.Item2 + value);
            if (cap >= 0) _capsFound.Add(cap);
            changed = true;
        }
        _display.Text = GameState.Euro(_feedTotal);
        if (gs.Bag.Count == 0)
        {
            _feeding = false;
            _status.Text = "BEREIT";
            ShowBon();
        }
        if (changed || !_feeding) gs.Notify();
    }

    void ShowEmptyBon()
    {
        ClearBon();
        _bonLines.AddChild(PfandTheme.Text("Noch nichts eingeworfen.", 20, Palette.Muted));
    }

    void ShowBon()
    {
        ClearBon();
        foreach (var kv in _receipt)
            AddBonLine($"{kv.Value.count}× {BottleInfo.Name(kv.Key)}", Format(kv.Value.sum), Palette.Ink);
        _bonLines.AddChild(new HSeparator());
        AddBonLine("SUMME", GameState.Euro(_feedTotal), Palette.Ink, true);
        foreach (var cap in _capsFound)
        {
            bool isNew = GameState.I.Caps.TryGetValue(cap, out var n) && n == 1;
            AddBonLine("+ Kronkorken: " + Capitalize(CapData.All[cap].Name), isNew ? "NEU!" : "", Palette.GoodText);
        }
    }

    void ClearBon()
    {
        foreach (var c in _bonLines.GetChildren()) c.QueueFree();
    }

    void AddBonLine(string left, string right, Color color, bool bold = false)
    {
        var row = new HBoxContainer();
        var l = PfandTheme.Text(left, bold ? 24 : 20, color);
        l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        l.ClipText = true;
        row.AddChild(l);
        row.AddChild(PfandTheme.Text(right, bold ? 24 : 20, color));
        _bonLines.AddChild(row);
    }

    static string Format(double v) => GameState.Euro(v).Replace("€ ", "");

    static string Capitalize(string s) => s.Length == 0 ? s : s[0] + s.Substring(1).ToLowerInvariant();
}

/// <summary>Raster der Taschenplätze mit kleinen Flaschen.</summary>
public partial class BagGrid : Control
{
    const int Cols = 6, Slot = 22, Gap = 4;

    public void UpdateMinSize()
    {
        int rows = (GameState.I.BagCapacity + Cols - 1) / Cols;
        CustomMinimumSize = new Vector2(Cols * (Slot + Gap) - Gap, rows * (Slot + Gap) - Gap);
    }

    public override void _Ready() => UpdateMinSize();

    public override void _Draw()
    {
        var gs = GameState.I;
        float offsetX = Mathf.Floor((Size.X - (Cols * (Slot + Gap) - Gap)) / 2);
        for (int i = 0; i < gs.BagCapacity; i++)
        {
            var pos = new Vector2(offsetX + i % Cols * (Slot + Gap), i / Cols * (Slot + Gap));
            var r = new Rect2(pos, Slot, Slot);
            DrawRect(r, Palette.Border);
            DrawRect(r.Grow(-2), Palette.Panel);
            if (i < gs.Bag.Count)
                Sprites.Bottle(this, r.GetCenter(), gs.Bag[i], 0, i, 0.7f);
        }
    }
}
