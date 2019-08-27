using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 地图数据
/// </summary>
public struct Data   : IComponentData
{
    public int Width;
    public int Height;
}

/// <summary>
/// 正在创建标签
/// </summary>
public struct OnCreateTag : IComponentData { }

/// <summary>
/// 开关
/// </summary>
public struct UpdateTag : IComponentData { }

/// <summary>
/// 单元数据
/// </summary>
public struct Cell : IComponentData
{
    public int Index;
    public Vector3 Position;
    public Color Color;
    public Color NE;
    public Color E;
    public Color SE;
    public Color SW;
    public Color W;
    public Color NW;
}


public struct ColorBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Color(ColorBuffer e) { return e.Value; }
    public static implicit operator ColorBuffer(Color e) { return new ColorBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Color Value;
}

public struct VertexBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(VertexBuffer e) { return e.Value; }
    public static implicit operator VertexBuffer(Vector3 e) { return new VertexBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

public struct TriangleBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator int(TriangleBuffer e) { return e.Value; }
    public static implicit operator TriangleBuffer(int e) { return new TriangleBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public int Value;
}