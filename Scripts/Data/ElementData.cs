using Godot;
using System;
using System.IO;

public class ElementData
{
    public int Id { get; private set; }
    public AnimationData AnimData { get; set; }
    public short OriginX { get; set; }
    public short OriginY { get; set; }
    public short ImgWidth { get; set; }
    public short ImgHeight { get; set; }
    public int GfxId { get; set; }
    public byte VisualHeight { get; set; }
    public byte VisibilityMask { get; set; }
    public byte ShaderId { get; set; }
    public byte PropertiesFlag { get; set; }
    public byte GroundSoundType { get; set; }
    public byte Slope { get; set; }
    public bool MoveTop { get; set; }
    public bool Walkable { get; set; }
    public bool Animated { get; set; }
    public bool BeforeMobile { get; set; }
    public bool Flip { get; set; }

    public ElementData(BinaryReader reader)
    {
        Id = reader.ReadInt32();
        OriginX = reader.ReadInt16();
        OriginY = reader.ReadInt16();
        ImgWidth = reader.ReadInt16();
        ImgHeight = reader.ReadInt16();
        GfxId = reader.ReadInt32();
            
        PropertiesFlag = reader.ReadByte();
            
        VisualHeight = reader.ReadByte();
        VisibilityMask = reader.ReadByte();
        ShaderId = reader.ReadByte();
            
        Slope = (byte)(PropertiesFlag & 15);
        Flip = (PropertiesFlag & 16) == 16;
        MoveTop = (PropertiesFlag & 32) == 32;
        BeforeMobile = (PropertiesFlag & 64) == 64;
        Walkable = (PropertiesFlag & 128) == 128;

        AnimData = new AnimationData(reader, Flip, false); 
        Animated = AnimData != null;

        GroundSoundType = reader.ReadByte();
    }
    
    public class AnimationData
    {
        public int Duration { get; set; }
        public short[] AnimationTimes { get; set; }
        public short[] TextureOffsets { get; set; }
        public short ImageWidth { get; set; }
        public short ImageHeight { get; set; }
        public short ImageWidthTotal { get; set; }
        public short ImageHeightTotal { get; set; }

        public AnimationData(BinaryReader reader, bool flip, bool export)
        {
            var animCount = reader.ReadByte() & 0xFF;
            if (animCount == 0)
                return;
            
            Duration = reader.ReadInt32();
            
            ImageWidth = reader.ReadInt16();
            ImageHeight = reader.ReadInt16();
            ImageWidthTotal = reader.ReadInt16();
            ImageHeightTotal = reader.ReadInt16();
            
            AnimationTimes = new short[animCount];
            TextureOffsets = new short[animCount * 2];

            for (var i = 0; i < animCount; ++i)
            {
                AnimationTimes[i] = reader.ReadInt16();
            }

            for (var i = 0; i < animCount * 2; ++i)
            {
                TextureOffsets[i] = reader.ReadInt16();
            }
        }
    }
}
