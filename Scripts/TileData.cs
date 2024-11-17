using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class TileData
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("biome")] public Enums.Biome Biome { get; set; }
    [JsonPropertyName("category")] public Enums.Category Category { get; set; }
    [JsonPropertyName("height")] public int Height { get; set; }
    [JsonPropertyName("default_offset")] public List<int> DefaultOffset { get; set; }
    
    public CompressedTexture2D Texture { get; set; }

    public void LoadTexture()
    {
        Texture = GD.Load<CompressedTexture2D>($"res://Assets/GFX/{Id}.tgam.png");
    }

    public TileData Copy()
    {
        return new TileData
        {
            Id = Id,
            Biome = Biome,
            Category = Category,
            Height = Height,
            DefaultOffset = DefaultOffset,
            Texture = Texture,
        };
    }
}
