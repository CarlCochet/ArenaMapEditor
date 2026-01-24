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
    public sbyte VisualHeight { get; set; }
    public sbyte VisibilityMask { get; set; }
    public sbyte ShaderId { get; set; }
    public sbyte PropertiesFlag { get; set; }
    public sbyte GroundSoundType { get; set; }
    public sbyte Slope { get; set; }
    public bool MoveTop { get; set; }
    public bool Walkable { get; set; }
    public bool Animated { get; set; }
    public bool BeforeMobile { get; set; }
    public bool Flip { get; set; }

    public void Load(ExtendedDataInputStream reader)
    {
        Id = reader.ReadInt();
        OriginX = reader.ReadShort();
        OriginY = reader.ReadShort();
        ImgWidth = reader.ReadShort();
        ImgHeight = reader.ReadShort();
        GfxId = reader.ReadInt();
        PropertiesFlag = reader.ReadByte();
        VisualHeight = reader.ReadByte();
        VisibilityMask = reader.ReadByte();
        ShaderId = reader.ReadByte();
            
        Slope = (sbyte)(PropertiesFlag & 15);
        Flip = (PropertiesFlag & 16) == 16;
        MoveTop = (PropertiesFlag & 32) == 32;
        BeforeMobile = (PropertiesFlag & 64) == 64;
        Walkable = (PropertiesFlag & 128) == 128;

        AnimData = new AnimationData(Flip, false); 
        AnimData.Load(reader);
        Animated = AnimData != null;

        GroundSoundType = reader.ReadByte();
    }

    public void Save(OutputBitStream writer)
    {
        writer.WriteInt(Id);
        writer.WriteShort(OriginX);
        writer.WriteShort(OriginY);
        writer.WriteShort(ImgWidth);
        writer.WriteShort(ImgHeight);
        writer.WriteInt(GfxId);
        writer.WriteByte(PropertiesFlag);
        writer.WriteByte(VisualHeight);
        writer.WriteByte(VisibilityMask);
        writer.WriteByte(ShaderId);
        if (AnimData != null)
            AnimData.Save(writer);
        else
            writer.WriteByte(0);
        writer.WriteByte(GroundSoundType);
    }
    
    public class AnimationData(bool flip, bool export)
    {
        public int Duration { get; set; }
        public short[] AnimationTimes { get; set; }
        public short[] TextureOffsets { get; set; }
        public short ImageWidth { get; set; }
        public short ImageHeight { get; set; }
        public short ImageWidthTotal { get; set; }
        public short ImageHeightTotal { get; set; }

        private bool _flip = flip;
        private bool _export = export;

        public void Load(ExtendedDataInputStream reader)
        {
            var animCount = reader.ReadByte() & 0xFF;
            if (animCount == 0)
                return;
            
            Duration = reader.ReadInt();
            
            ImageWidth = reader.ReadShort();
            ImageHeight = reader.ReadShort();
            ImageWidthTotal = reader.ReadShort();
            ImageHeightTotal = reader.ReadShort();
            
            AnimationTimes = new short[animCount];
            TextureOffsets = new short[animCount * 2];

            for (var i = 0; i < animCount; ++i)
            {
                AnimationTimes[i] = reader.ReadShort();
            }

            for (var i = 0; i < animCount * 2; ++i)
            {
                TextureOffsets[i] = reader.ReadShort();
            }
        }

        public void Save(OutputBitStream writer)
        {
            if (AnimationTimes == null || AnimationTimes.Length == 0)
            {
                writer.WriteByte(0);
                return;
            }

            writer.WriteByte((sbyte)AnimationTimes.Length); 
            writer.WriteInt(Duration);
            writer.WriteShort(ImageWidth);
            writer.WriteShort(ImageHeight);
            writer.WriteShort(ImageWidthTotal);
            writer.WriteShort(ImageHeightTotal);

            foreach (var time in AnimationTimes)
            {
                writer.WriteShort(time);
            }

            foreach (var offset in TextureOffsets)
            {
                writer.WriteShort(offset);
            }
        }
    }
}
