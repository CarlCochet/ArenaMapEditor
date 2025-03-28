using Godot;
using System;

public partial class Filter : Control
{
    public Enums.Biome Biome { get; set; } = Enums.Biome.Global;
    public Enums.Category Category { get; set; } = Enums.Category.Global;
    public Enums.Mode Mode { get; set; } = Enums.Mode.Gfx;

    public event EventHandler FilterUpdated; 
    public event EventHandler ModeUpdated; 
	
    public override void _Ready() { }
	
    private void _OnBiomeSelected(int index)
    {
        Biome = (Enums.Biome)index;
        FilterUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void _OnCategorySelected(int index)
    {
        Category = (Enums.Category)index;
        FilterUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void _OnModeSelected(int index)
    {
        Mode = (Enums.Mode)index;
        FilterUpdated?.Invoke(this, EventArgs.Empty);
        ModeUpdated?.Invoke(this, EventArgs.Empty);
    }
}