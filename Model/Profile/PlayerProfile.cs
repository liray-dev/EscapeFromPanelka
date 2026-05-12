using EFP.Model.Inventory;

namespace EFP.Model.Profile;

public sealed class PlayerProfile
{
    public PlayerProfile(string name, int level, long currency, InventoryGrid stash)
    {
        Name = name;
        Level = level;
        Currency = currency;
        Stash = stash;
    }

    public string Name { get; set; }
    public int Level { get; set; }
    public long Currency { get; set; }
    public InventoryGrid Stash { get; }

    public event EventHandler? CurrencyChanged;
    public event EventHandler? LevelChanged;

    public void AddCurrency(long amount)
    {
        if (amount == 0) return;
        Currency += amount;
        CurrencyChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetLevel(int level)
    {
        if (level == Level) return;
        Level = level;
        LevelChanged?.Invoke(this, EventArgs.Empty);
    }

    public static PlayerProfile CreateDefault()
    {
        return new PlayerProfile("Оперативник", 1, 0, new InventoryGrid(10, 8));
    }
}
