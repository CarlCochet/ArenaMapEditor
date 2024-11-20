using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class MapInfo
{
    [JsonPropertyName("instanceId")] public int InstanceId { get; set; }
    [JsonPropertyName("partitions")] public List<MapPartition> Partitions { get; set; }
    
    public MapInfo() { }

    public class MapPartition
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("elements")] public List<PartitionElement> Elements { get; set; }
        
        public MapPartition() { }
    }

    public class PartitionElement
    {
        [JsonPropertyName("m_type")] public int Type { get; set; }
        [JsonPropertyName("m_cellZ")] public int CellZ { get; set; }
        [JsonPropertyName("m_cellX")] public int CellX { get; set; }
        [JsonPropertyName("m_cellY")] public int CellY { get; set; }
        [JsonPropertyName("m_top")] public int Top { get; set; }
        [JsonPropertyName("m_left")] public int Left { get; set; }
        [JsonPropertyName("m_altitudeOrder")] public int AltitudeOrder { get; set; }
        [JsonPropertyName("m_height")] public int Height { get; set; }
        [JsonPropertyName("m_groupKey")] public int GroupKey { get; set; }
        [JsonPropertyName("m_layerIndex")] public int LayerIndex { get; set; }
        [JsonPropertyName("m_groupId")] public int GroupId { get; set; }
        [JsonPropertyName("m_occluder")] public bool Occluder { get; set; }
        [JsonPropertyName("m_hashCode")] public long HashCode { get; set; }
        [JsonPropertyName("m_colors")] public List<float> Colors { get; set; }
        [JsonPropertyName("m_commonData")] public CommonElementData CommonData { get; set; }
        
        public PartitionElement() { }
    }

    public class CommonElementData
    {
        [JsonPropertyName("animData")] public object AnimData { get; set; }
        [JsonPropertyName("originX")] public int OriginX { get; set; }
        [JsonPropertyName("originY")] public int OriginY { get; set; }
        [JsonPropertyName("imgWidth")] public int ImgWidth { get; set; }
        [JsonPropertyName("imgHeight")] public int ImgHeight { get; set; }
        [JsonPropertyName("gfxId")] public int GfxId { get; set; }
        [JsonPropertyName("visualHeight")] public int VisualHeight { get; set; }
        [JsonPropertyName("visibilityMask")] public int VisibilityMask { get; set; }
        [JsonPropertyName("shader")] public int Shader { get; set; }
        [JsonPropertyName("propertiesFlag")] public int PropertiesFlag { get; set; }
        [JsonPropertyName("groundSoundType")] public int GroundSoundType { get; set; }
        [JsonPropertyName("slope")] public int Slope { get; set; }
        [JsonPropertyName("moveTop")] public bool MoveTop { get; set; }
        [JsonPropertyName("walkable")] public bool Walkable { get; set; }
        [JsonPropertyName("annimated")] public bool Animated { get; set; }
        [JsonPropertyName("beforeMobile")] public bool BeforeMobile { get; set; }
        [JsonPropertyName("flip")] public bool Flip { get; set; }
        
        public CommonElementData() { }
    }
}
