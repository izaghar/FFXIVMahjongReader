using GameModel;
using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace MahjongReader.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly IPluginLog pluginLog;
    private readonly ITextureProvider textureProvider;

    public List<ObservedTile> ObservedTiles { get; set; }
    public Dictionary<string, int> RemainingMap { get; set; }
    public Dictionary<string, int> SuitCounts { get; set; }

    private readonly Dictionary<string, ISharedImmediateTexture> mjaiNotationToTexture;
    private readonly Dictionary<string, ISharedImmediateTexture> suitToTexture;

    public MainWindow(Plugin plugin, IPluginLog pluginLog, ITextureProvider textureProvider) : base(
        "Mahjong Reader", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(220, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        this.pluginLog = pluginLog;
        this.textureProvider = textureProvider;
        ObservedTiles = new List<ObservedTile>();
        RemainingMap = TileTextureUtilities.TileCountTracker.RemainingFromObserved(ObservedTiles);
        SuitCounts = new Dictionary<string, int>();
        foreach (var kvp in RemainingMap) {
            var suit = kvp.Key.Substring(1, 1);
            if (suit == Suit.HONOR) continue;
            if (SuitCounts.ContainsKey(suit)) SuitCounts[suit] += kvp.Value;
            else SuitCounts.Add(suit, kvp.Value);
        }

        mjaiNotationToTexture = new();
        foreach (var notationToTextureId in TileTextureUtilities.NotationToTextureId) {
            mjaiNotationToTexture.Add(
                notationToTextureId.Key,
                textureProvider.GetFromGameIcon(new GameIconLookup(uint.Parse(notationToTextureId.Value))));
        }

        suitToTexture = new();
        suitToTexture.Add(Suit.MAN, textureProvider.GetFromGameIcon(new GameIconLookup(76001)));
        suitToTexture.Add(Suit.PIN, textureProvider.GetFromGameIcon(new GameIconLookup(76010)));
        suitToTexture.Add(Suit.SOU, textureProvider.GetFromGameIcon(new GameIconLookup(76019)));
    }

    public void Dispose() { }

    private Vector2 tileSize;
    private Vector2 suitSize;

    private static Vector4 ColorForCount(int count) => count switch
    {
        >= 4 => ImGuiColors.HealerGreen,
        3    => ImGuiColors.DalamudYellow,
        2    => ImGuiColors.DalamudOrange,
        1    => ImGuiColors.DalamudRed,
        _    => ImGuiColors.DalamudGrey3,
    };

    private void DrawTileRemaining(string suit, int number, bool isDora) {
        var notation = $"{number}{suit}";
        var count = isDora ? RemainingMap[notation] + RemainingMap[$"0{suit}"] : RemainingMap[notation];
        var isDoraRemaing = isDora && RemainingMap[$"0{suit}"] > 0;
        var texture = mjaiNotationToTexture[notation].GetWrapOrEmpty();
        ImGui.TableNextColumn();
        ImGui.Image(texture.Handle, tileSize);
        ImGui.SameLine();
        var label = isDoraRemaing ? "x" + count + "*" : "x" + count;
        ImGui.TextColored(ColorForCount(count), label);
    }

    private void DrawSuitRemaining(string suit) {
        var count = SuitCounts[suit];
        var texture = suitToTexture[suit].GetWrapOrEmpty();
        ImGui.TableNextColumn();
        ImGui.Image(texture.Handle, suitSize);
        ImGui.SameLine();
        ImGui.Text("x" + count);
    }

    public override void Draw()
    {
        var avail = ImGui.GetContentRegionAvail();
        var textW = ImGui.CalcTextSize("x4*").X;
        var perColWidth = avail.X / 4f;
        var maxTileH = ImGui.GetTextLineHeight() * 2.0f;
        var tileH = MathF.Max(16f, MathF.Min(maxTileH, MathF.Min(
            (avail.Y - 30f) / 11.5f,
            (perColWidth - textW - 12f) * 1.3f)));
        var tileW = tileH / 1.3f;
        tileSize = new Vector2(tileW, tileH);
        suitSize = new Vector2(tileH, tileH);

        var tableFlags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingFixedFit;

        if (ImGui.BeginTable("#Tiles", 4, tableFlags)) {
            for (var i = 1; i < 10; i++) {
                ImGui.TableNextRow();
                bool isDora = i == 5;

                DrawTileRemaining(Suit.SOU, i, isDora);
                DrawTileRemaining(Suit.PIN, i, isDora);
                DrawTileRemaining(Suit.MAN, i, isDora);

                if (i == 3) {
                    DrawSuitRemaining(Suit.SOU);
                } else if (i == 5) {
                    DrawSuitRemaining(Suit.PIN);
                } else if (i == 7) {
                    DrawSuitRemaining(Suit.MAN);
                } else {
                    ImGui.TableNextColumn();
                }
            }
            ImGui.EndTable();
        }

        ImGuiHelpers.ScaledDummy(0, 6);

        if (ImGui.BeginTable("#TilesWind", 4, tableFlags)) {
            ImGui.TableNextRow();
            for (var i = 4; i >= 1; i--) {
                DrawTileRemaining(Suit.HONOR, i, false);
            }
            ImGui.EndTable();
        }

        if (ImGui.BeginTable("#TilesDragon", 3, tableFlags)) {
            ImGui.TableNextRow();
            for (var i = 7; i >= 5; i--) {
                DrawTileRemaining(Suit.HONOR, i, false);
            }
            ImGui.EndTable();
        }
    }
}
