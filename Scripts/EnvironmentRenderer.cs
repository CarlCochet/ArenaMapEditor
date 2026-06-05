using Godot;
using System.Collections.Generic;

public partial class EnvironmentRenderer : Node2D
{
    private Texture2D _texture;

    public void Setup(Texture2D texture)
    {
        _texture = texture;
    }

    public void LoadElements(EnvData envData)
    {
        Clear();

        if (envData == null)
            return;

        var coords = new HashSet<(int x, int y, int z)>();
        foreach (var partition in envData.Partitions)
        {
            var baseX = partition.X * EnvData.Partition.ChunkSize;
            var baseY = partition.Y * EnvData.Partition.ChunkSize;

            if (partition.ParticleData != null)
                foreach (var p in partition.ParticleData)
                    coords.Add((baseX + p.X, baseY + p.Y, p.Z));
            if (partition.Sounds != null)
                foreach (var s in partition.Sounds)
                    coords.Add((baseX + s.X, baseY + s.Y, s.Z));
            if (partition.InteractiveElements != null)
                foreach (var ie in partition.InteractiveElements)
                    coords.Add((baseX + ie.X, baseY + ie.Y, ie.Z));
            if (partition.DynamicElements != null)
                foreach (var de in partition.DynamicElements)
                    coords.Add((baseX + de.X, baseY + de.Y, de.Z));
        }

        foreach (var (x, y, z) in coords)
        {
            var marker = new EnvMarker();
            marker.Texture = _texture;
            marker.Centered = true;
            marker.Position = CoordToIso(x, y, z);
            marker.CellX = x;
            marker.CellY = y;
            marker.CellZ = z;
            AddChild(marker);
        }
    }

    public static Vector2 CoordToIso(int x, int y, int z)
    {
        var screenX = (x - y) * GlobalData.CellWidth * 0.5f;
        var screenY = (x + y) * GlobalData.CellHeight * 0.5f - z * GlobalData.ElevationStep;
        return new Vector2(screenX, screenY);
    }

    public void Clear()
    {
        foreach (var child in GetChildren())
            child.QueueFree();
    }
}
