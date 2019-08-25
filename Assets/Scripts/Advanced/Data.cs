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
    public bool BIfNewMap;
}

/// <summary>
/// 地图网格数据
/// </summary>
public struct MapMesh : IComponentData {

}

/// <summary>
/// 顶点数据
/// </summary>
public struct Vertex : IComponentData
{
    public Vector3 Vector;
    public Color Color;
    public int Triangle;
    public bool Switcher;
}

/// <summary>
/// 单元数据
/// </summary>
public struct Cell : IComponentData
{
    public Vector3 Position;
    public Color Color;
    public bool Switcher;
}

//public struct ColorBuffer : IBufferElementData {
//    // These implicit conversions are optional, but can help reduce typing.
//    public static implicit operator float3(ColorBuffer e) { return e.Value; }
//    public static implicit operator ColorBuffer(float3 e) { return new ColorBuffer { Value = e }; }

//    // Actual value each buffer element will store.
//    public float3 Value;
//}

public struct ColorBuff : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Color(ColorBuff e) { return e.Value; }
    public static implicit operator ColorBuff(Color e) { return new ColorBuff { Value = e }; }

    // Actual value each buffer element will store.
    public Color Value;
}

//public struct ColorBuffer : IBufferElementData {
//    // These implicit conversions are optional, but can help reduce typing.
//    public static implicit operator Color(ColorBuffer e) { return e.Value; }
//    public static implicit operator ColorBuffer(Color e) { return new ColorBuffer { Value = e }; }

//    // Actual value each buffer element will store.
//    public Color Value;
//}

//public struct VertexBuffer : IBufferElementData {
//    // These implicit conversions are optional, but can help reduce typing.
//    public static implicit operator Vector3(VertexBuffer e) { return e.Value; }
//    public static implicit operator VertexBuffer(Vector3 e) { return new VertexBuffer { Value = e }; }

//    // Actual value each buffer element will store.
//    public Vector3 Value;
//}

//public struct TriangleBuffer : IBufferElementData {
//    // These implicit conversions are optional, but can help reduce typing.
//    public static implicit operator int(TriangleBuffer e) { return e.Value; }
//    public static implicit operator TriangleBuffer(int e) { return new TriangleBuffer { Value = e }; }

//    // Actual value each buffer element will store.
//    public int Value;
//}