using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// C:保存六边形的坐标和颜色数据
/// </summary>
[Serializable]
public struct HexCellData : IComponentData {
    public int X;
    public int Y;
    public int Z;
    public Color color;
}

/// <summary>
/// 如果想生成新的地图,使用这个开关
/// </summary>
public struct SwitchCreateCellData : IComponentData
{
    public bool bIfNewMap;
}

/// <summary>
/// 用来储存顶点数据
/// </summary>
public struct VertexData : IComponentData
{
    public float3 Value;
}

/// <summary>
/// 这是个坑,列表根本不能使用,替代方案是创建更多的实体来记录数据
/// </summary>
//public struct HexMeshData : IComponentData {
//    //public float3[] Vertices;ArgumentException: HexMeshData contains a field of UnityEngine.float3[], which is neither primitive nor blittable.
//    //public Vector3[] Vertices;//ArgumentException: HexMeshData contains a field of UnityEngine.Vector3[], which is neither primitive nor blittable.
//    public NativeList<float3> Vertices;//ArgumentException: HexMeshData contains a field of Unity.Collections.LowLevel.Unsafe.DisposeSentinel, which is neither primitive nor blittable.
//}

/// <summary>
/// 这个用来做测试,暂时还不知道如何在Job里面使用
/// https://docs.unity3d.com/Packages/com.unity.entities@0.0/manual/dynamic_buffers.html
/// 有案例,但是没有详细完整的使用案例,不知道能不能在Job里面使用
/// 总感觉这又是一个鸡肋,没有List方便
/// </summary>
//[InternalBufferCapacity(HexMetrics.HexCelllCount)]
//public struct HexMeshData : IBufferElementData {
//    // These implicit conversions are optional, but can help reduce typing.
//    public static implicit operator float3(HexMeshData e) { return e.Value; }
//    public static implicit operator HexMeshData(float3 e) { return new HexMeshData { Value = e }; }

//    // Actual value each buffer element will store.
//    public float3 Value;
//}
/// <summary>
/// Sad:明明一个组件搞定，偏偏用了六个
/// 因为这里无法使用数据或列表
/// 东北
/// </summary>
//public struct NeighborNE : IComponentData {

//    public Entity NE;

//}

///// <summary>
///// 相邻：东
///// </summary>
//public struct NeighborE : IComponentData {

//    public Entity E;

//}

///// <summary>
///// 东南
///// </summary>
//public struct NeighborSE : IComponentData {

//    public Entity SE;

//}

///// <summary>
///// 西南
///// </summary>
//public struct NeighborSW : IComponentData {

//    public Entity SW;

//}

///// <summary>
///// 西
///// </summary>
//public struct NeighborW : IComponentData {

//    public Entity W;
//}

///// <summary>
///// 西北
///// </summary>
//public struct NeighborNW : IComponentData {

//    public Entity NW;
//}