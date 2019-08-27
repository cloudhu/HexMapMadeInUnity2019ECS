using UnityEngine;

/// <summary>
/// 六边形常量
/// </summary>
public static class HexMetrics
{
    /// <summary>
    /// 方向，N=北，S=南，E=东，W=西
    /// </summary>
    public enum HexDirection {
        NE=0, E=1, SE=2, SW=3, W=4, NW=5
    }

    /// <summary>
    /// 六边形单元的六个方向
    /// </summary>
    public readonly static HexDirection[] hexDirections =
    {
        HexDirection.NE,
        HexDirection.E,
        HexDirection.SE,
        HexDirection.SW,
        HexDirection.W,
        HexDirection.NW
    };

    /// <summary>
    /// 总的顶点数
    /// </summary>
    public static int HexCelllCount = 0;

    /// <summary>
    /// 地图宽度（以单元为单位）
    /// </summary>
    public static int MapWidth = 0;

    /// <summary>
    /// 每个单元的顶点数量
    /// </summary>
    public const int CellVerticesCount = 42;//78;

    /// <summary>
    /// 六边形外半径=六边形边长
    /// </summary>
    public const float OuterRadius = 10f;

    /// <summary>
    /// 六边形内半径=0.8*外半径
    /// </summary>
    public const float InnerRadius = OuterRadius * 0.866025404f;

    /// <summary>
    /// 六边形单元中心本色区域占比
    /// </summary>
    public const float SolidFactor = 0.75f;

    /// <summary>
    /// 六边形单元外围混合区域占比
    /// </summary>
    public const float BlendFactor = 1f - SolidFactor;

    /// <summary>
    /// 六边形中心区域的六个角组成的数组
    /// </summary>
    public readonly static Vector3[] SolidCorners = {
		new Vector3(0f, 0f, OuterRadius)*SolidFactor,
		new Vector3(InnerRadius, 0f, 0.5f * OuterRadius)*SolidFactor,
		new Vector3(InnerRadius, 0f, -0.5f * OuterRadius)*SolidFactor,
		new Vector3(0f, 0f, -OuterRadius)*SolidFactor,
		new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius)*SolidFactor,
		new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius)*SolidFactor,
		new Vector3(0f, 0f, OuterRadius)*SolidFactor
    };

    /// <summary>
    /// 六边形的六个角
    /// </summary>
    public readonly static Vector3[] Corners = {
        new Vector3(0f, 0f, OuterRadius),
        new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(0f, 0f, -OuterRadius),
        new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
        new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(0f, 0f, OuterRadius)
    };

    /// <summary>
    /// 获取相邻六边形单元之间的矩形桥
    /// </summary>
    /// <param name="cellIndex">六边形单元索引</param>
    /// <returns>矩形桥</returns>
    public  static Vector3 GetBridge(int cellIndex)
    {
        //return (Corners[cellIndex] + Corners[cellIndex + 1]) * 0.5f * BlendFactor;
        return (Corners[cellIndex] + Corners[cellIndex + 1]) * BlendFactor;
    }
}