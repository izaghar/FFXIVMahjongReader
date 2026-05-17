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

    public string? HoveredGameNotation { get; set; }

    private readonly Dictionary<string, ISharedImmediateTexture> mjaiNotationToTexture;

    public MainWindow(Plugin plugin, IPluginLog pluginLog, ITextureProvider textureProvider) : base(
        "Doma Helper", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(280, 220),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        this.pluginLog = pluginLog;
        this.textureProvider = textureProvider;
        ObservedTiles = new List<ObservedTile>();
        RemainingMap = TileTextureUtilities.TileCountTracker.RemainingFromObserved(ObservedTiles);
        SuitCounts = new Dictionary<string, int>();

        mjaiNotationToTexture = new();
        foreach (var notationToTextureId in TileTextureUtilities.NotationToTextureId) {
            mjaiNotationToTexture.Add(
                notationToTextureId.Key,
                textureProvider.GetFromGameIcon(new GameIconLookup(uint.Parse(notationToTextureId.Value))));
        }
    }

    public void Dispose() { }

    private Vector2 tileSize;

    private static Vector4 ColorForCount(int count) => count switch
    {
        >= 4 => ImGuiColors.HealerGreen,
        3    => ImGuiColors.DalamudYellow,
        2    => ImGuiColors.DalamudOrange,
        1    => ImGuiColors.DalamudRed,
        _    => ImGuiColors.DalamudGrey3,
    };

    public override void Draw()
    {
        var tileH = ImGui.GetTextLineHeight() * 2.6f;
        tileSize = new Vector2(tileH / 1.3f, tileH);

        DrawNumberSuitSection(Suit.MAN, "Man", "Characters");
        DrawNumberSuitSection(Suit.PIN, "Pin", "Dots");
        DrawNumberSuitSection(Suit.SOU, "Sou", "Bamboo");

        DrawHonorSection("Winds", new[] { 4, 1, 2, 3 });
        DrawHonorSection("Dragons", new[] { 6, 5, 7 });
    }

    private int SuitTotalRemaining(string suit)
    {
        var total = 0;
        for (var i = 1; i < 10; i++) total += RemainingMap[$"{i}{suit}"];
        total += RemainingMap[$"0{suit}"];
        return total;
    }

    private void DrawNumberSuitSection(string suit, string mainName, string subtitle)
    {
        ImGui.Text($"{mainName} ({subtitle})");
        ImGui.SameLine();
        ImGui.TextDisabled($"({SuitTotalRemaining(suit)} remaining)");

        if (ImGui.BeginTable($"##suit-{suit}", 9, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoBordersInBody)) {
            ImGui.TableNextRow();
            for (var i = 1; i < 10; i++) {
                var notation = $"{i}{suit}";
                var isDora = i == 5;
                var count = isDora ? RemainingMap[notation] + RemainingMap[$"0{suit}"] : RemainingMap[notation];
                var isDoraRemaining = isDora && RemainingMap[$"0{suit}"] > 0;
                DrawTileCell(notation, count, isDoraRemaining);
            }
            ImGui.EndTable();
        }

        ImGuiHelpers.ScaledDummy(0, 4);
    }

    private void DrawHonorSection(string title, int[] indices)
    {
        ImGui.Text(title);

        if (ImGui.BeginTable($"##honor-{title}", indices.Length, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoBordersInBody)) {
            ImGui.TableNextRow();
            foreach (var idx in indices) {
                var notation = $"{idx}{Suit.HONOR}";
                DrawTileCell(notation, RemainingMap[notation], false);
            }
            ImGui.EndTable();
        }

        ImGuiHelpers.ScaledDummy(0, 4);
    }

    private void DrawTileCell(string notation, int displayCount, bool isDoraRemaining)
    {
        var texture = mjaiNotationToTexture[notation].GetWrapOrEmpty();
        ImGui.TableNextColumn();
        ImGui.BeginGroup();
        ImGui.Image(texture.Handle, tileSize);
        var imgMin = ImGui.GetItemRectMin();
        var imgMax = ImGui.GetItemRectMax();
        var label = isDoraRemaining ? displayCount + "*" : displayCount.ToString();
        var labelW = ImGui.CalcTextSize(label).X;
        var offset = MathF.Max(0f, (tileSize.X - labelW) * 0.5f);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.TextColored(ColorForCount(displayCount), label);
        ImGui.EndGroup();

        if (notation == HoveredGameNotation) {
            var color = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.85f, 0.2f, 0.95f));
            ImGui.GetWindowDrawList().AddRect(
                new Vector2(imgMin.X - 2f, imgMin.Y - 2f),
                new Vector2(imgMax.X + 2f, imgMax.Y + 2f),
                color, 3f, ImDrawFlags.None, 2f);
        }
    }
}
