using Godot;
using System;
using System.Text;

public class ArenaColor
{
    public static readonly ArenaColor Alpha = new(0.0f, 0.0f, 0.0f, 0.0f);
    public static readonly ArenaColor WhiteAlpha = new(1.0f, 1.0f, 1.0f, 0.0f);
    public static readonly ArenaColor WhiteSemiAlpha = new(1.0f, 1.0f, 1.0f, 0.5f);
    public static readonly ArenaColor WhiteQuarterAlpha = new(1.0f, 1.0f, 1.0f, 0.25f);
    public static readonly ArenaColor White = new(1.0f, 1.0f, 1.0f, 1.0f);
    public static readonly ArenaColor Black = new(0.0f, 0.0f, 0.0f, 1.0f);
    public static readonly ArenaColor Red = new(1.0f, 0.0f, 0.0f, 1.0f);
    public static readonly ArenaColor Green = new(0.0f, 1.0f, 0.0f, 1.0f);
    public static readonly ArenaColor Blue = new(0.0f, 0.0f, 1.0f, 1.0f);
    public static readonly ArenaColor Cyan = new(0.0f, 1.0f, 1.0f, 1.0f);
    public static readonly ArenaColor Grey = new(128, 128, 128, 255);
    public static readonly ArenaColor DarkGray = new(64, 64, 64, 255);
    public static readonly ArenaColor LightGray = new(192, 192, 192, 255);
    public static readonly ArenaColor VeryLightGray = new(224, 224, 224, 255);
    public static readonly ArenaColor Purple = new(0.57f, 0.2f, 0.75f, 0.66f);
    public static readonly ArenaColor Gray = new(0.95f, 0.64f, 0.25f, 1.0f);

    private static readonly Random Rng = new();

    private int _abgr;

    public ArenaColor()
    {
        _abgr = 0;
    }

    public ArenaColor(ArenaColor color)
    {
        _abgr = color._abgr;
    }

    public ArenaColor(float red, float green, float blue, float alpha)
    {
        SetFromFloat(red, green, blue, alpha);
    }

    public ArenaColor(int abgr)
    {
        Set(abgr);
    }

    public ArenaColor(sbyte red, sbyte green, sbyte blue, sbyte alpha)
    {
        SetFromByte(red, green, blue, alpha);
    }

    public ArenaColor(int red, int green, int blue, int alpha)
    {
        SetFromInt(red, green, blue, alpha);
    }

    public int Get()
    {
        return _abgr;
    }

    public int GetRGBA()
    {
        return _abgr >> 24 | _abgr << 8;
    }

    public sbyte GetAlphaByte()
    {
        return (sbyte)(_abgr >> 24 & 0xFF);
    }

    public sbyte GetRedByte()
    {
        return (sbyte)(_abgr & 0xFF);
    }

    public sbyte GetGreenByte()
    {
        return (sbyte)(_abgr >> 8 & 0xFF);
    }

    public sbyte GetBlueByte()
    {
        return (sbyte)(_abgr >> 16 & 0xFF);
    }

    public int GetAlphaInt()
    {
        return _abgr >> 24 & 0xFF;
    }

    public int GetRedInt()
    {
        return _abgr & 0xFF;
    }

    public int GetGreenInt()
    {
        return _abgr >> 8 & 0xFF;
    }

    public int GetBlueInt()
    {
        return _abgr >> 16 & 0xFF;
    }

    public float GetAlpha()
    {
        var alphaInt = GetAlphaInt();
        if (alphaInt < 0)
        {
            alphaInt += 256;
        }
        return alphaInt / 255.0f;
    }

    public float GetRed()
    {
        return GetRedInt() / 255.0f;
    }

    public float GetGreen()
    {
        return GetGreenInt() / 255.0f;
    }

    public float GetBlue()
    {
        return GetBlueInt() / 255.0f;
    }

    public float GetMaxRGBComponent()
    {
        return Math.Max(GetRed(), Math.Max(GetBlue(), GetGreen()));
    }

    public float GetIntensity()
    {
        return (GetRed() + GetGreen() + GetBlue()) / 3.0f;
    }

    public void SetIntensity(float intensity)
    {
        if (intensity is < 0.0f or > 1.0f)
        {
            throw new ArgumentException($"Invalid intensity value {intensity}");
        }

        var maxComponent = Math.Max(GetRed(), Math.Max(GetBlue(), GetGreen()));
        if (maxComponent == 0.0f)
        {
            SetFromFloat(intensity, intensity, intensity, GetAlpha());
            return;
        }
        
        var ratio = intensity / maxComponent;
        var red = Math.Min(1.0f, GetRed() * ratio);
        var blue = Math.Min(1.0f, GetBlue() * ratio);
        var green = Math.Min(1.0f, GetGreen() * ratio);
        SetFromFloat(red, green, blue, GetAlpha());
    }

    public void SetFromFloat(float red, float green, float blue, float alpha)
    {
        _abgr = GetFromFloat(red, green, blue, alpha);
    }

    public void SetAlpha(float alpha)
    {
        alpha = Clamp(alpha, 0.0f, 1.0f);
        _abgr = _abgr & 16777215 | (int)(alpha * 255.0f) << 24;
    }

    public void Set(int abgr)
    {
        _abgr = abgr;
    }

    public void SetFromByte(sbyte red, sbyte green, sbyte blue, sbyte alpha)
    {
        _abgr = GetFromByte(red, green, blue, alpha);
    }

    public void SetFromInt(int red, int green, int blue, int alpha)
    {
        _abgr = GetFromInt(red, green, blue, alpha);
    }

    public void Mult(ArenaColor c)
    {
        SetFromFloat(GetRed() * c.GetRed(), GetGreen() * c.GetGreen(), 
            GetBlue() * c.GetBlue(), GetAlpha() * c.GetAlpha());
    }

    public void Random()
    {
        SetFromInt(Rng.Next(0, 256), Rng.Next(0, 256), Rng.Next(0, 256), Rng.Next(0, 256));
    }

    public static ArenaColor Mult(ArenaColor c1, ArenaColor c2)
    {
        var result = new ArenaColor(c1);
        result.Mult(c2);
        return result;
    }

    public static float GetAlphaFromARGB(int argb)
    {
        return (argb >> 24 & 0xFF) / 255.0f;
    }

    public static float GetBlueFromARGB(int argb)
    {
        return (argb >> 16 & 0xFF) / 255.0f;
    }

    public static float GetGreenFromARGB(int argb)
    {
        return (argb >> 8 & 0xFF) / 255.0f;
    }

    public static float GetRedFromARGB(int argb)
    {
        return (argb & 0xFF) / 255.0f;
    }

    public static int GetFromFloat(float red, float green, float blue, float alpha)
    {
        return (int)(Clamp(alpha, 0.0f, 1.0f) * 255.0f) << 24
               | (int)(Clamp(red, 0.0f, 1.0f) * 255.0f)
               | (int)(Clamp(green, 0.0f, 1.0f) * 255.0f) << 8
               | (int)(Clamp(blue, 0.0f, 1.0f) * 255.0f) << 16;
    }

    public static int GetFromByte(sbyte red, sbyte green, sbyte blue, sbyte alpha)
    {
        return ToUnsignedByte(alpha) << 24 | ToUnsignedByte(red) | 
               ToUnsignedByte(green) << 8 | ToUnsignedByte(blue) << 16;
    }

    public static int GetFromInt(int red, int green, int blue, int alpha)
    {
        return Clamp(alpha, 0, 255) << 24
               | Clamp(red, 0, 255)
               | Clamp(green, 0, 255) << 8
               | Clamp(blue, 0, 255) << 16;
    }

    public override string ToString()
    {
        return GetRedFromARGB(_abgr) + ", " +
               GetGreenFromARGB(_abgr) + ", " +
               GetBlueFromARGB(_abgr) + ", " +
               GetAlphaFromARGB(_abgr);
    }

    public string GetRGBtoHex()
    {
        var sb = new StringBuilder();
        var red = (GetRedInt() < 16 ? "0" : "") + GetRedInt().ToString("x");
        var green = (GetGreenInt() < 16 ? "0" : "") + GetGreenInt().ToString("x");
        var blue = (GetBlueInt() < 16 ? "0" : "") + GetBlueInt().ToString("x");
        sb.Append(red).Append(green).Append(blue);
        return sb.ToString();
    }

    public string GetRGBAtoHex()
    {
        var sb = new StringBuilder();
        var red = (GetRedInt() < 16 ? "0" : "") + GetRedInt().ToString("x");
        var green = (GetGreenInt() < 16 ? "0" : "") + GetGreenInt().ToString("x");
        var blue = (GetBlueInt() < 16 ? "0" : "") + GetBlueInt().ToString("x");
        var alpha = (GetAlphaInt() < 16 ? "0" : "") + GetAlphaInt().ToString("x");
        sb.Append(red).Append(green).Append(blue).Append(alpha);
        return sb.ToString();
    }

    public static ArenaColor GetRGBAFromHex(string hex)
    {
        var red = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        var green = int.Parse(hex.Substring(2, 4), System.Globalization.NumberStyles.HexNumber);
        var blue = int.Parse(hex.Substring(4, 6), System.Globalization.NumberStyles.HexNumber);
        var alpha = 1;
        if (hex.Length == 8)
        {
            alpha = int.Parse(hex.Substring(6, 8), System.Globalization.NumberStyles.HexNumber);
        }
        return new ArenaColor(red, green, blue, alpha);
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static short ToUnsignedByte(sbyte b)
    {
        return b < 0 ? (short)(256 + b) : b;
    }

}
