using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class AmbianceData
{
    public int Id { get; set; } = -1;
    public int AmbianceCount { get; set; }
    public Dictionary<int, AmbianceProperties> Properties { get; set; } = new();

    public AmbianceData(string id)
    {
        if (!int.TryParse(id, out var tempId))
            return;
        Id = tempId;
    }

    public void Load(BinaryReader reader)
    {
        AmbianceCount = reader.ReadInt32();
        Properties.EnsureCapacity(AmbianceCount);
        
        for (var i = 0; i < AmbianceCount; i++)
        {
            var properties = new AmbianceProperties();
            properties.Load(reader);
            Properties.TryAdd(i, properties);
        }
    }

    public void Save(BinaryWriter writer)
    {
        
    }

    public class AmbianceProperties
    {
        public int ZoneId { get; set; }
        public string Name { get; set; }
        public int SoundId { get; set; }
        public bool UseReverb { get; set; }
        public int SoundPreset1 { get; set; }
        public byte LightGreen { get; set; }
        public int SoundPreset2 { get; set; }
        
        public void Load(BinaryReader reader)
        {
            ZoneId = reader.ReadInt32();
            var nameSize = reader.ReadInt16();
            var nameBytes = reader.ReadBytes(nameSize);
            Name = Encoding.UTF8.GetString(nameBytes);
            SoundId = reader.ReadInt32();
            UseReverb = reader.ReadBoolean();
            SoundPreset1 = reader.ReadInt32();
            LightGreen = reader.ReadByte();
            SoundPreset2 = reader.ReadInt32();
        }

        public void Save(BinaryWriter writer)
        {
            
        }
    }
}
