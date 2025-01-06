using Godot;
using System;
using System.Text.Json.Serialization;

public class Settings
{
    [JsonPropertyName("ArenaPath")] public string ArenaPath { get; set; }
}
