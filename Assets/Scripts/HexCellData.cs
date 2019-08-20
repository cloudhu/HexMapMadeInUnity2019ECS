using System;
using Unity.Entities;
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

    public float RadiansPerSecond;
}

public struct SwitchCreateCellData : IComponentData
{
    public bool bIfNewMap;
}

public struct SwitchRotateData : IComponentData {
    public bool bIfStartRotateSystem;
}