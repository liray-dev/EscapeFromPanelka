using System.Numerics;
using EFP.Model.WorldGen;

namespace EFP.Model.Raid;

public sealed class SectorGrower
{
    private const float TriggerDistance = 14f;
    private const float CooldownSeconds = 0.85f;
    private const int ModulesPerGrowth = 8;

    private readonly ModuleLibrary _library;
    private readonly Random _rng;
    private readonly SectorBuildState _state;
    private readonly int _moduleCap;
    private float _cooldownRemaining;

    public SectorGrower(SectorBuildState state, ModuleLibrary library, int seed, int moduleCap)
    {
        _state = state;
        _library = library;
        _rng = new Random(seed ^ 0x517CC1B7);
        _moduleCap = Math.Max(state.Blueprint.Steps.Count + ModulesPerGrowth, moduleCap);
    }

    public int ModuleCap => _moduleCap;
    public int CurrentModules => _state.Blueprint.Steps.Count;
    public int RemainingBudget => Math.Max(0, _moduleCap - _state.Blueprint.Steps.Count);

    public bool TryGrow(ProceduralSector sector, Vector3 playerPosition, float deltaTime)
    {
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining = MathF.Max(0f, _cooldownRemaining - deltaTime);
            return false;
        }

        if (RemainingBudget <= 0) return false;

        var budget = Math.Min(ModulesPerGrowth, RemainingBudget);
        var newSteps = StructureBlueprintGenerator.Grow(_state, _library, _rng, playerPosition, TriggerDistance, budget);
        if (newSteps.Count == 0) return false;

        StructureAssembler.AppendSteps(sector, _library, _state.Blueprint, newSteps, _rng);
        _cooldownRemaining = CooldownSeconds;
        return true;
    }
}
