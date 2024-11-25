using Godot;
using System;
using System.Collections.Generic;

public partial class PartitionData : Node
{
    public int MapId { get; set; }
    public List<ElementData> Elements { get; set; }
    public TopologyData Topology { get; set; }
    public LightData Light { get; set; }
    public GfxData Gfx { get; set; }
    public FightData Fight { get; set; }
    public EnvData Env { get; set; }
    public CoordsData Coords { get; set; }

    public PartitionData(string path, int id)
    {
        MapId = id;
    }

    public void LoadData()
    {
        
    }
}
