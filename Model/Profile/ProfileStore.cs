using System.Text.Json;
using EFP.Model.Inventory;

namespace EFP.Model.Profile;

public static class ProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static PlayerProfile LoadOrCreate(string path)
    {
        if (!File.Exists(path))
        {
            var defaults = PlayerProfile.CreateDefault();
            Save(path, defaults);
            return defaults;
        }

        try
        {
            var snapshot = JsonSerializer.Deserialize<ProfileSnapshot>(File.ReadAllText(path), SerializerOptions);
            if (snapshot is null) return PlayerProfile.CreateDefault();
            return Materialize(snapshot);
        }
        catch
        {
            return PlayerProfile.CreateDefault();
        }
    }

    public static void Save(string path, PlayerProfile profile)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var snapshot = new ProfileSnapshot
        {
            Name = profile.Name,
            Level = profile.Level,
            Currency = profile.Currency,
            StashWidth = profile.Stash.Width,
            StashHeight = profile.Stash.Height,
            StashSlots = profile.Stash.Slots
                .Select(s => new StashSlotSnapshot { ItemId = s.Item.Id, X = s.X, Y = s.Y })
                .ToList()
        };

        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, SerializerOptions));
    }

    private static PlayerProfile Materialize(ProfileSnapshot snapshot)
    {
        var stash = new InventoryGrid(
            snapshot.StashWidth > 0 ? snapshot.StashWidth : 10,
            snapshot.StashHeight > 0 ? snapshot.StashHeight : 8);

        foreach (var slot in snapshot.StashSlots)
        {
            var def = InventoryCatalog.Resolve(slot.ItemId);
            if (def is not null) stash.TryAdd(def);
        }

        return new PlayerProfile(
            string.IsNullOrWhiteSpace(snapshot.Name) ? "Оперативник" : snapshot.Name,
            Math.Max(1, snapshot.Level),
            Math.Max(0, snapshot.Currency),
            stash);
    }

    private sealed class ProfileSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public long Currency { get; set; }
        public int StashWidth { get; set; }
        public int StashHeight { get; set; }
        public List<StashSlotSnapshot> StashSlots { get; set; } = [];
    }

    private sealed class StashSlotSnapshot
    {
        public string ItemId { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
    }
}
