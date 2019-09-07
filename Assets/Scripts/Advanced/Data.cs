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

public struct RiverRenderTag : IComponentData { }

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
    /// 水体高度
    /// </summary>
    public float WaterLevel;

    /// <summary>
    /// 当前单元是否有河流
    /// </summary>
    public bool HasRiver;

    /// <summary>
    /// 当前单元是否有路
    /// </summary>
    public bool HasRoad;

    /// <summary>
    /// 是否被水淹没
    /// </summary>
    public bool IsUnderWater;
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
    public Color NEColor;
    public Color EColor;
    public Color SEColor;
    public Color SWColor;
    public Color WColor;
    public Color NWColor;
    //六个方向相邻单元的海拔
    public int NEElevation;
    public int EElevation;
    public int SEElevation;
    public int SWElevation;
    public int WElevation;
    public int NWElevation;
    //索引
    public int NEIndex;
    public int EIndex;
    public int SEIndex;
    public int SWIndex;
    public int WIndex;
    public int NWIndex;
    //6个方向的位置
    public Vector3 NEPosition;
    public Vector3 EPosition;
    public Vector3 SEPosition;
    public Vector3 SWPosition;
    public Vector3 WPosition;
    public Vector3 NWPosition;
    //6个方向相邻的单元是否处于水下
    public bool NEIsUnderWater;
    public bool EIsUnderWater;
    public bool SEIsUnderWater;
    public bool SWIsUnderWater;
    public bool WIsUnderWater;
    public bool NWIsUnderWater;
}

/// <summary>
/// 单元的六个方向是否有道路通过
/// </summary>
public struct RoadBools : IComponentData {

    public bool NEHasRoad;
    public bool EHasRoad;
    public bool SEHasRoad;
    public bool SWHasRoad;
    public bool WHasRoad;
    public bool NWHasRoad;
}

/// <summary>
/// 动态缓存：颜色
/// </summary>
public struct ColorBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Color(ColorBuffer e) { return e.Value; }
    public static implicit operator ColorBuffer(Color e) { return new ColorBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Color Value;
}

/// <summary>
/// 顶点
/// </summary>
public struct VertexBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(VertexBuffer e) { return e.Value; }
    public static implicit operator VertexBuffer(Vector3 e) { return new VertexBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

/// <summary>
/// UV动态缓存
/// </summary>
public struct UvBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector2(UvBuffer e) { return e.Value; }
    public static implicit operator UvBuffer(Vector2 e) { return new UvBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector2 Value;
}

/// <summary>
/// 河流
/// </summary>
public struct RiverBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(RiverBuffer e) { return e.Value; }
    public static implicit operator RiverBuffer(Vector3 e) { return new RiverBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

/// <summary>
/// 道路顶点动态缓存
/// </summary>
public struct RoadBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(RoadBuffer e) { return e.Value; }
    public static implicit operator RoadBuffer(Vector3 e) { return new RoadBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

public struct RoadUvBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector2(RoadUvBuffer e) { return e.Value; }
    public static implicit operator RoadUvBuffer(Vector2 e) { return new RoadUvBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector2 Value;
}

/// <summary>
/// 水体顶点
/// </summary>
public struct WaterBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(WaterBuffer e) { return e.Value; }
    public static implicit operator WaterBuffer(Vector3 e) { return new WaterBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

/// <summary>
/// 水岸顶点
/// </summary>
public struct WaterShoreBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(WaterShoreBuffer e) { return e.Value; }
    public static implicit operator WaterShoreBuffer(Vector3 e) { return new WaterShoreBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

public struct ShoreUvBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector2(ShoreUvBuffer e) { return e.Value; }
    public static implicit operator ShoreUvBuffer(Vector2 e) { return new ShoreUvBuffer { Value = e }; }

    // Actual value each buffer element will store. 
    public Vector2 Value;
}

public struct EstuaryBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector3(EstuaryBuffer e) { return e.Value; }
    public static implicit operator EstuaryBuffer(Vector3 e) { return new EstuaryBuffer { Value = e }; }

    // Actual value each buffer element will store.
    public Vector3 Value;
}

public struct EstuaryUvBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector2(EstuaryUvBuffer e) { return e.Value; }
    public static implicit operator EstuaryUvBuffer(Vector2 e) { return new EstuaryUvBuffer { Value = e }; }

    // Actual value each buffer element will store. Estuary
    public Vector2 Value;
}

public struct EstuaryUvsBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator Vector2(EstuaryUvsBuffer e) { return e.Value; }
    public static implicit operator EstuaryUvsBuffer(Vector2 e) { return new EstuaryUvsBuffer { Value = e }; }

    // Actual value each buffer element will store. Estuary
    public Vector2 Value;
}

public struct DebugBuffer : IBufferElementData {
    // These implicit conversions are optional, but can help reduce typing.
    public static implicit operator int(DebugBuffer e) { return e.Value; }
    public static implicit operator DebugBuffer(int e) { return new DebugBuffer { Value = e }; }

    // Actual value each buffer element will store. Estuary
    public int Value;
}