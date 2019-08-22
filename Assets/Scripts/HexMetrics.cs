using UnityEngine;

/// <summary>
/// 六边形常量
/// </summary>
public static class HexMetrics
{
    /// <summary>
    /// 总的顶点数
    /// </summary>
    public static int HexCelllCount = 0;

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