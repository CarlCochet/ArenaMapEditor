using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
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

    public void Load(ExtendedDataInputStream reader)
    {
        AmbianceCount = reader.ReadInt();
        Properties.EnsureCapacity(AmbianceCount);
        
        for (var i = 0; i < AmbianceCount; i++)
        {
            var properties = new AmbianceProperties();
            properties.Load(reader);
            Properties.TryAdd(properties.ZoneId, properties);
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
        public sbyte LightGreen { get; set; }
        public int SoundPreset2 { get; set; }
        
        public void Load(ExtendedDataInputStream reader)
        {
            ZoneId = reader.ReadInt();
            var nameSize = reader.ReadShort();
            var nameBytes = reader.ReadBytes(nameSize);
            Name = Encoding.UTF8.GetString(MemoryMarshal.Cast<sbyte, byte>(nameBytes.AsSpan()));
            SoundId = reader.ReadInt();
            UseReverb = reader.ReadBooleanBit();
            SoundPreset1 = reader.ReadInt();
            LightGreen = reader.ReadByte();
            SoundPreset2 = reader.ReadInt();
        }

        public void Save(BinaryWriter writer)
        {
            
        }
    }
}
