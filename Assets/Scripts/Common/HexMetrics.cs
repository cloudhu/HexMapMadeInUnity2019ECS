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
    /// 单元边界类型
    /// </summary>
    public enum HexEdgeType {
        Flat, Slope, Cliff
    }


    /// <summary>
    /// 获取边缘的类型
    /// </summary>
    /// <param name="elevation1">海拔1</param>
    /// <param name="elevation2">海拔2</param>
    /// <returns>边缘类型</returns>
    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)
        {
            return HexEdgeType.Flat;
        }
        int delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1)
        {
            return HexEdgeType.Slope;
        }
        return HexEdgeType.Cliff;
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
    public const int CellVerticesCount = 120;//78;

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
    /// 海拔步长
    /// </summary>
    public const float elevationStep = 5f;

    /// <summary>
    /// 每个斜坡上的阶梯数
    /// </summary>
    public const int terracesPerSlope = 5;

    /// <summary>
    /// 阶梯步长
    /// </summary>
    public const int terraceSteps = terracesPerSlope * 2 + 1;

    /// <summary>
    /// 水平阶梯步长
    /// </summary>
    public const float horizontalTerraceStepSize = 1f / terraceSteps;

    /// <summary>
    /// 垂直阶梯步长
    /// </summary>
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

    /// <summary>
    /// 阶梯插值
    /// </summary>
    /// <param name="a">向量a</param>
    /// <param name="b">向量b</param>
    /// <param name="step">步数</param>
    /// <returns></returns>
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

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