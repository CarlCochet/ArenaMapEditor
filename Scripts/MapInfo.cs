using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class MapInfo
{
    [JsonPropertyName("instanceId")] private int _instanceId;
    [JsonPropertyName("partitions")] private List<MapPartition> _partitions;

    public class MapPartition
    {
        [JsonPropertyName("id")] private string _id;
        [JsonPropertyName("elements")] private List<PartitionElement> _elements;
    }

    public class PartitionElement
    {
        [JsonPropertyName("m_type")] private int _type;
        [JsonPropertyName("m_cellZ")] private int _cellZ;
        [JsonPropertyName("m_cellX")] private int _cellX;
        [JsonPropertyName("m_cellY")] private int _cellY;
        [JsonPropertyName("m_top")] private int _top;
        [JsonPropertyName("m_left")] private int _left;
        [JsonPropertyName("m_altitudeOrder")] private int _altitudeOrder;
        [JsonPropertyName("m_height")] private int _height;
        [JsonPropertyName("m_groupKey")] private int _groupKey;
        [JsonPropertyName("m_layerIndex")] private int _layerIndex;
        [JsonPropertyName("m_groupId")] private int _groupId;
        [JsonPropertyName("m_occluder")] private bool _occluder;
        [JsonPropertyName("m_hashCode")] private long _hashCode;
        [JsonPropertyName("m_colors")] private List<float> _colors;
        [JsonPropertyName("m_commonData")] private CommonElementData _commonData;
    }

    public class CommonElementData
    {
        [JsonPropertyName("animData")] private object _animData;
        [JsonPropertyName("originX")] private int _originX;
        [JsonPropertyName("originY")] private int _originY;
        [JsonPropertyName("imgWidth")] private int _imgWidth;
        [JsonPropertyName("imgHeight")] private int _imgHeight;
        [JsonPropertyName("gfxId")] private int _gfxId;
        [JsonPropertyName("visualHeight")] private int _visualHeight;
        [JsonPropertyName("visibilityMask")] private int _visibilityMask;
        [JsonPropertyName("shader")] private int _shader;
        [JsonPropertyName("propertiesFlag")] private int _propertiesFlag;
        [JsonPropertyName("groundSoundType")] private int _groundSoundType;
        [JsonPropertyName("slope")] private int _slope;
        [JsonPropertyName("moveTop")] private bool _moveTop;
        [JsonPropertyName("walkable")] private bool _walkable;
        [JsonPropertyName("annimated")] private bool _annimated;
        [JsonPropertyName("beforeMobile")] private bool _beforeMobile;
        [JsonPropertyName("flip")] private bool _flip;
    }
}
