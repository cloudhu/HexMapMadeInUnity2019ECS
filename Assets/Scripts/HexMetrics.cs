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
        NE, E, SE, SW, W, NW
    }

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
    public const int CellVerticesCount = 18;

    /// <summary>
    /// 六边形外半径=六边形边长
    /// </summary>
    public const float outerRadius = 10f;

    /// <summary>
    /// 六边形内半径=0.8*外半径
    /// </summary>
    public const float innerRadius = outerRadius * 0.866025404f;

    /// <summary>
    /// 六边形的六个角组成的数组
    /// </summary>
    public readonly static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(0f, 0f, outerRadius)
	};
}