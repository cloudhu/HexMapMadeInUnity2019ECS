using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 地图数据
/// </summary>
public struct Data   : IComponentData
{
    /// <summary>
    /// X方向的单元数量
    /// </summary>
    public int CellCountX;
    /// <summary>
    /// Z方向的单元数量
    /// </summary>
    public int CellCountZ;
    /// <summary>
    /// X方向的地图块数量
    /// </summary>
    public int ChunkCountX;
}

/// <summary>
/// 新数据标签
/// </summary>
public struct NewDataTag : IComponentData { }

//public struct RiverRenderTag : IComponentData { }

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
    /// 海拔
    /// </summary>
    public int Elevation;
    //需要更新的相邻单元索引
    public int NEIndex;
    public int EIndex;
    public int SEIndex;
    public int SWIndex;
    public int WIndex;
    public int NWIndex;
}

/// <summary>
/// 地图块数据
/// </summary>
public struct ChunkData : IComponentData
{

    /// <summary>
    /// 地图块编号
    /// </summary>
    public int ChunkId;

    /// <summary>
    /// 地图块内索引
    /// </summary>
    public int ChunkIndex;

    /// <summary>
    /// 单元索引
    /// </summary>
    public int CellIndex;

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

    /// <summary>
    /// 当前单元的海拔
    /// </summary>
    public int Elevation;

    /// <summary>
    /// 当前单元是否有河流
    /// </summary>
    public bool HasRiver;
}

/// <summary>
/// 河流数据
/// </summary>
public struct River : IComponentData
{
    /// <summary>
    /// 是否有河流进入
    /// </summary>
    public bool HasIncomingRiver;

    /// <summary>
    /// 是否有河流出
    /// </summary>
    public bool HasOutgoingRiver;

    /// <summary>
    /// 流入方向
    /// </summary>
    public int IncomingRiver;

    /// <summary>
    /// 流出方向
    /// </summary>
    public int OutgoingRiver;
}

public struct Neighbors : IComponentData
{
    //六个方向相邻单元的颜色
    public Color NE;
    public Color E;
    public Color SE;
    public Color SW;
    public Color W;
    public Color NW;
    //六个方向相邻单元的海拔
    public int NEElevation;
    public int EElevation;
    public int SEElevation;
    public int SWElevation;
    public int WElevation;
    public int NWElevation;
}

/// <summary>
/// 六个方向相邻单元的索引
/// </summary>
public struct NeighborsIndex : IComponentData {

    public int NEIndex;
    public int EIndex;
    public int SEIndex;
    public int SWIndex;
    public int WIndex;
    public int NWIndex;
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

public struct UvBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector2(UvBuffer e) { return e.Value; }
    public static implicit operator UvBuffer(Vector2 e) { return new UvBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector2 Value;
}

public struct RiverBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(RiverBuffer e) { return e.Value; }
    public static implicit operator RiverBuffer(Vector3 e) { return new RiverBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}