using Godot;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public class ArenaMath
{
    public static readonly float FPi = (float)Math.PI;
    public static Random Rng = new();
    private static readonly SHA256 HashComputer = SHA256.Create();
    public const float Epsilon = 1.0E-5F;
    public const float RadianToDegree = (float)(180.0 / Math.PI);
    private static readonly float[] CosLut = new float[360];
    private static readonly float[] SinLut = new float[360];

    static ArenaMath()
    {
        for (var i = 0; i < CosLut.Length; i++)
        {
            CosLut[i] = (float)Math.Cos(i * Math.PI / 180.0);
        }

        for (var i = 0; i < SinLut.Length; i++)
        {
            SinLut[i] = (float)Math.Sin(i * Math.PI / 180.0);
        }
    }

    public static void SetSeed(int seed)
    {
        Rng = new Random(seed);
    }

    public static int Random(int value)
    {
        return Rng.Next(value);
    }

    public static int Random()
    {
        return Rng.Next();
    }

    public static int RandomInt31()
    {
        return Rng.Next() >> 1;
    }

    public static int RandomRadius(int pos, int radius)
    {
        return pos - radius + Rng.Next(2 * radius);
    }

    public static void RandomBytes(byte[] bytesArray)
    {
        Rng.NextBytes(bytesArray);
    }

    public static bool RandomBoolean()
    {
        return Rng.Next(2) == 1;
    }

    public static float RandomFloat()
    {
        return (float)Rng.NextDouble();
    }

    public static double RandomDouble()
    {
        return Rng.NextDouble();
    }

    public static int Random(int valueMin, int valueMax)
    {
        return valueMin == valueMax ? valueMin : Rng.Next(valueMin, valueMax);
    }

    public static long Random(long valueMin, long valueMax)
    {
        if (valueMin == valueMax) return valueMin;
        var range = valueMax - valueMin;
        var randomLong = (long)(Rng.NextDouble() * range);
        return valueMin + randomLong;
    }

    public static float Random(float valueMin, float valueMax)
    {
        return Math.Abs(valueMin - valueMax) < Epsilon 
            ? valueMin 
            : (float)Rng.NextDouble() * (valueMax - valueMin) + valueMin;
    }

    public static float Lerp(float value1, float value2, float amount)
    {
        return value1 + amount * (value2 - value1);
    }

    public static double Lerp(double value1, double value2, double amount)
    {
        return value1 + amount * (value2 - value1);
    }

    public static short Clamp(short value, short min, short max)
    {
        return value <= min ? min : (value >= max ? max : value);
    }

    public static int Clamp(int value, int min, int max)
    {
        return value <= min ? min : (value >= max ? max : value);
    }

    public static long Clamp(long value, long min, long max)
    {
        return value <= min ? min : (value >= max ? max : value);
    }

    public static float Clamp(float value, float min, float max)
    {
        return value <= min ? min : (value >= max ? max : value);
    }

    public static double Clamp(double value, double min, double max)
    {
        return value <= min ? min : (value >= max ? max : value);
    }

    public static float DecimalPart(float value)
    {
        var absValue = Math.Abs(value);
        return Math.Sign(value) * (absValue - (float)Math.Floor(absValue));
    }

    public static double DecimalPart(double value)
    {
        var absValue = Math.Abs(value);
        return Math.Sign(value) * (absValue - Math.Floor(absValue));
    }

    public static float Round(float value, int decimals)
    {
        return (float)Math.Round(value, decimals);
    }

    public static long GetLongFromTwoInt(int a, int b)
    {
        var longA = (long)a & 0xFFFFFFFFL;
        var longB = (long)b & 0xFFFFFFFFL;
        return longA << 32 | longB;
    }

    public static int GetFirstIntFromLong(long value)
    {
        return (int)(value >> 32 & 0xFFFFFFFFL);
    }

    public static int GetSecondIntFromLong(long value)
    {
        return (int)(value & 0xFFFFFFFFL);
    }

    public static int GetIntFromFourByte(byte a, byte b, byte c, byte d)
    {
        var intA = a & 255;
        var intB = b & 255;
        var intC = c & 255;
        var intD = d & 255;
        return intA << 8 | intB << 16 | intC << 24 | intD;
    }

    public static short GetShortFromTwoBytes(byte a, byte b)
    {
        var shortA = (short)(a & 255);
        var shortB = (short)(b & 255);
        return (short)(shortA << 8 | (ushort)shortB);
    }

    public static byte GetFirstByteFromShort(short value)
    {
        return (byte)(value >> 8 & 0xFF);
    }

    public static byte GetSecondByteFromShort(short value)
    {
        return (byte)(value & 255);
    }

    public static int GetIntFromTwoShort(short a, short b)
    {
        var intA = a & 0x0000FFFF;
        var intB = b & 0x0000FFFF;
        return (intA << 16) | intB;
    }

    public static short GetFirstShortFromInt(int value)
    {
        return (short)(value >> 16 & 65535);
    }

    public static short GetSecondShortFromInt(int value)
    {
        return (short)(value & 65535);
    }

    public static bool IsEqual(float f1, float f2)
    {
        return Math.Abs(f1 - f2) < Epsilon;
    }

    public static bool IsEqual(double d1, double d2, double precision)
    {
        return Math.Abs(Math.Floor(d1 / precision) - Math.Floor(d2 / precision)) < Epsilon;
    }

    public static float Sin(float angle)
    {
        return (float)Math.Sin(angle);
    }

    public static float Cos(float angle)
    {
        return (float)Math.Cos(angle);
    }

    public static float SinF(float angle)
    {
        var index = (int)(angle * RadianToDegree) % 360;
        if (index < 0)
        {
            index += 360;
        }
        return SinLut[index];
    }

    public static float CosF(float angle)
    {
        var index = (int)(angle * RadianToDegree) % 360;
        if (index < 0)
        {
            index += 360;
        }
        return CosLut[index];
    }

    public static float Acos(float angle)
    {
        return (float)Math.Acos(angle);
    }

    public static float Atan2(float y, float x)
    {
        return (float)Math.Atan2(y, x);
    }

    public static float Sqrt(float a)
    {
        return (float)Math.Sqrt(a);
    }

    public static int ISqrt(int n)
    {
        var result = 0;
        while (n >= 2 * result + 1)
        {
            n -= 2 * result++ + 1;
        }
        return result;
    }

    public static int NearestGreatestPowOfTwo(int value)
    {
        if (value < 2)
        {
            return value;
        }

        value = --value | value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    public static double Pow2(double value)
    {
        return value * value;
    }

    public static float Pow2(float value)
    {
        return value * value;
    }

    public static double Pow3(double value)
    {
        return value * value * value;
    }

    public static float Pow3(float value)
    {
        return value * value * value;
    }

    public static float Log2(float value)
    {
        return (float)(Math.Log(value) / Math.Log(2.0));
    }

    public static double Log2(double value)
    {
        return Math.Log(value) / Math.Log(2.0);
    }

    public static int Log2I(int value)
    {
        var count = 1;
        var c = 0;
        while (value > count)
        {
            count *= 2;
            c++;
        }
        return c;
    }

    public static int Max(int first, int second, params int[] others)
    {
        var max = Math.Max(first, second);
        return others.Prepend(max).Max();
    }

    public static Random GetRandomGenerator()
    {
        return Rng;
    }

    public static int GetCRC(string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        var hash = HashComputer.ComputeHash(bytes);
        return BitConverter.ToInt32(hash, 0);
    }

    public static long GetCRCLong(string name)
    {
        var bytes = Encoding.UTF8.GetBytes(name);
        var hash = HashComputer.ComputeHash(bytes);
        return BitConverter.ToInt64(hash, 0);
    }

    public static bool GetBooleanAt(long booleanArray, int index)
    {
        return (booleanArray >> index & 1L) == 1L;
    }

    public static long SetBooleanAt(long booleanArray, int index, bool value)
    {
        if (value)
        {
            booleanArray |= 1L << index;
        }
        else
        {
            booleanArray &= ~(1L << index);
        }
        return booleanArray;
    }

    public static float EaseOut(float t, float b, float c, float d)
    {
        var normalizedTime = t / d - 1.0f;
        return -c * (normalizedTime * normalizedTime * normalizedTime * normalizedTime - 1.0f) + b;
    }

    public static int FastFloor(float value)
    {
        var v = (int)value;
        if (value >= 0)
            return v;
        return Math.Abs(v - value) < Epsilon 
            ? v 
            : v - 1;
    }

    public static int FastCeil(float value)
    {
        var v = (int)value;
        if (value <= 0.0f)
            return v;
        return Math.Abs(v - value) < Epsilon 
            ? v 
            : v + 1;
    }
}
