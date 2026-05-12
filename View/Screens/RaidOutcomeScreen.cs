using System.Numerics;
using EFP.GameState;
using EFP.Model.Raid;
using EFP.Utilities;
using RaidModel = EFP.Model.Raid.Raid;

namespace EFP.View.Screens;

public sealed class RaidOutcomeScreen(IGameStateContext context) : Screen(context)
{
    private RaidModel? _raid;
    private RaidViewModel? _viewModel;

    public void Configure(RaidModel raid, RaidViewModel viewModel)
    {
        _raid = raid;
        _viewModel = viewModel;
    }

    public override void Draw()
    {
        if (_raid is null || _viewModel is null) return;

        const float width = 520f;
        const float height = 252f;
        var rect = new RectF(
            (Context.Window.Size.X - width) * 0.5f,
            (Context.Window.Size.Y - height) * 0.5f,
            width,
            height);

        Fill(rect, ColorRgba.FromBytes(14, 17, 22, 236));
        DrawOutline(rect, 2f, _viewModel.Phase == RaidPhase.Extracted
            ? ColorRgba.FromBytes(190, 176, 108)
            : ColorRgba.FromBytes(174, 96, 96));

        var title = _viewModel.Phase == RaidPhase.Extracted ? "Raid Summary / Extracted" : "Raid Summary / Failed";
        DrawCenteredText(new RectF(rect.X, rect.Y + 20f, rect.Width, 32f), title, 0.52f,
            ColorRgba.FromBytes(236, 239, 244));

        DrawText(new Vector2(rect.X + 28f, rect.Y + 70f),
            $"Cargo recovered: {_viewModel.FinalCargoCount}", 0.38f, ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(rect.X + 28f, rect.Y + 94f),
            $"Cargo value: {_viewModel.FinalCargoValue}", 0.38f, ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(rect.X + 28f, rect.Y + 118f),
            $"Health left: {_viewModel.PlayerHealth:0}/{_viewModel.PlayerMaxHealth:0}", 0.38f,
            ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(rect.X + 28f, rect.Y + 142f),
            $"Medkits left: {_viewModel.MedkitCount}", 0.38f, ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(rect.X + 28f, rect.Y + 166f),
            $"Time spent: {_raid.ElapsedRaidSeconds:0.0}s", 0.38f, ColorRgba.FromBytes(214, 220, 227));
        DrawText(new Vector2(rect.X + 28f, rect.Y + 190f),
            $"Pressure end state: {_raid.PressureLabel}", 0.38f,
            GetPressureColor(_viewModel.Phase, _raid.PressureLevel));

        DrawCenteredText(new RectF(rect.X, rect.Bottom - 44f, rect.Width, 24f),
            "R — новый сектор   Escape — в меню", 0.38f, ColorRgba.FromBytes(187, 196, 205));
    }

    private static ColorRgba GetPressureColor(RaidPhase phase, RaidPressureLevel pressure)
    {
        return phase switch
        {
            RaidPhase.Extracted => ColorRgba.FromBytes(225, 214, 120),
            RaidPhase.Failed => ColorRgba.FromBytes(232, 122, 122),
            _ => pressure switch
            {
                RaidPressureLevel.Stable => ColorRgba.FromBytes(178, 202, 184),
                RaidPressureLevel.Pressure => ColorRgba.FromBytes(228, 191, 109),
                RaidPressureLevel.Critical => ColorRgba.FromBytes(230, 122, 102),
                _ => ColorRgba.FromBytes(187, 196, 205)
            }
        };
    }
}
