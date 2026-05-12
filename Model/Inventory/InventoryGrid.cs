namespace EFP.Model.Inventory;

public sealed class InventoryGrid
{
    private readonly List<InventorySlot> _slots = [];

    public InventoryGrid(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
    }

    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<InventorySlot> Slots => _slots;
    public int OccupiedCells => _slots.Sum(s => s.Item.Width * s.Item.Height);
    public int TotalCells => Width * Height;
    public int TotalValue => _slots.Sum(s => s.Item.Value);

    public event EventHandler<InventorySlot>? ItemAdded;
    public event EventHandler<InventorySlot>? ItemRemoved;
    public event EventHandler? Cleared;

    public bool TryAdd(InventoryItem item)
    {
        for (var y = 0; y <= Height - item.Height; y++)
        {
            for (var x = 0; x <= Width - item.Width; x++)
            {
                if (!CanPlace(item, x, y)) continue;
                var slot = new InventorySlot(item, x, y);
                _slots.Add(slot);
                ItemAdded?.Invoke(this, slot);
                return true;
            }
        }

        return false;
    }

    public bool Remove(InventorySlot slot)
    {
        if (!_slots.Remove(slot)) return false;
        ItemRemoved?.Invoke(this, slot);
        return true;
    }

    public void Clear()
    {
        _slots.Clear();
        Cleared?.Invoke(this, EventArgs.Empty);
    }

    public bool Contains(int x, int y, out InventorySlot? slot)
    {
        foreach (var existing in _slots)
        {
            if (x >= existing.X && x < existing.X + existing.Item.Width &&
                y >= existing.Y && y < existing.Y + existing.Item.Height)
            {
                slot = existing;
                return true;
            }
        }

        slot = null;
        return false;
    }

    private bool CanPlace(InventoryItem item, int originX, int originY)
    {
        for (var dy = 0; dy < item.Height; dy++)
            for (var dx = 0; dx < item.Width; dx++)
                if (Contains(originX + dx, originY + dy, out _))
                    return false;
        return true;
    }
}
