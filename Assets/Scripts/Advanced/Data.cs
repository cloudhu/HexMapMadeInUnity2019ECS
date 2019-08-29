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
/// 新数据标签
/// </summary>
public struct NewDataTag : IComponentData { }

/// <summary>
/// 单元的更新数据
/// </summary>
public struct UpdateData : IComponentData
{
    /// <summary>
    /// 单元的索引
    /// </summary>
    public int CellIndex;
    /// <summary>
    /// 新的颜色
    /// </summary>
    public Color NewColor;
    /// <summary>
    /// 宽度
    /// </summary>
    public int Width;
    /// <summary>
    /// 海拔
    /// </summary>
    public int Elevation;
}

/// <summary>
/// 单元数据
/// </summary>
public struct Cell : IComponentData
{
    /// <summary>
    /// 单元索引
    /// </summary>
    public int Index;
    /// <summary>
    /// 位置
    /// </summary>
    public Vector3 Position;
    /// <summary>
    /// 颜色
    /// </summary>
    public Color Color;
    //六个方向相邻单元的颜色
    public Color NE;
    public Color E;
    public Color SE;
    public Color SW;
    public Color W;
    public Color NW;
    /// <summary>
    /// 六个方向相邻单元的索引
    /// </summary>
    public int NEIndex;
    public int EIndex;
    public int SEIndex;
    public int SWIndex;
    public int WIndex;
    public int NWIndex;
    /// <summary>
    /// 当前单元的海拔
    /// </summary>
    public int Elevation;
    //六个方向相邻单元的海拔
    public int NEElevation;
    public int EElevation;
    public int SEElevation;
    public int SWElevation;
    public int WElevation;
    public int NWElevation;
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



