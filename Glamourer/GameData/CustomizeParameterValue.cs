﻿using Newtonsoft.Json;

namespace Glamourer.GameData;

public readonly struct CustomizeParameterValue
{
    public static readonly CustomizeParameterValue Zero = default;

    private readonly Vector4 _data;

    public CustomizeParameterValue(Vector4 data)
        => _data = data;

    public CustomizeParameterValue(Vector3 data, float w = 0)
        => _data = new Vector4(data, w);

    public CustomizeParameterValue(FFXIVClientStructs.FFXIV.Common.Math.Vector4 data)
        => _data = new Vector4(Root(data.X), Root(data.Y), Root(data.Z), data.W);

    public CustomizeParameterValue(FFXIVClientStructs.FFXIV.Common.Math.Vector3 data)
        => _data = new Vector4(Root(data.X), Root(data.Y), Root(data.Z), 0);

    public CustomizeParameterValue(float value, float y = 0, float z = 0, float w = 0)
        => _data = new Vector4(value, y, z, w);

    public Vector3 InternalTriple
        => new(_data.X, _data.Y, _data.Z);

    public float Single
        => _data.X;

    public Vector4 InternalQuadruple
        => _data;

    public FFXIVClientStructs.FFXIV.Common.Math.Vector4 XivQuadruple
        => new(Square(_data.X), Square(_data.Y), Square(_data.Z), _data.W);

    public FFXIVClientStructs.FFXIV.Common.Math.Vector3 XivTriple
        => new(Square(_data.X), Square(_data.Y), Square(_data.Z));

    private static float Square(float x)
        => x < 0 ? -x * x : x * x;

    private static float Root(float x)
        => x < 0 ? -(float)Math.Sqrt(-x) : (float)Math.Sqrt(x);

    public float this[int idx]
        => _data[idx];

    public override string ToString()
        => _data.ToString();

    public string ToJson()
    {
        try
        {
            return JsonConvert.SerializeObject(_data, Formatting.None);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static bool FromJson(string input, out CustomizeParameterValue value)
    {
        try
        {
            value = new CustomizeParameterValue(JsonConvert.DeserializeObject<Vector4>(input));
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }
}
