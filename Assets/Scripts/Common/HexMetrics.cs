using UnityEngine;

/// <summary>
/// 六边形常量
/// </summary>
public static class HexMetrics
{

    #region River河流 Water

    /// <summary>
    /// 河流源头的海拔值
    /// </summary>
    public const int RiverSourceElevation = 5;

    /// <summary>
    /// 河床海拔偏移量
    /// </summary>
    public const float StreamBedElevationOffset = -1.75f;

    /// <summary>
    /// 水面海拔偏移量
    /// </summary>
    public const float WaterSurfaceElevationOffset = -0.5f;

    /// <summary>
    /// 水体占比
    /// </summary>
    public const float WaterFactor = 0.6f;


    public static Vector3 GetFirstWaterCorner(int direction)
    {
        return Corners[direction] * WaterFactor;
    }

    public static Vector3 GetSecondWaterCorner(int direction)
    {
        return Corners[direction + 1] * WaterFactor;
    }

    public const float WaterBlendFactor = 1f - WaterFactor;

    public static Vector3 GetWaterBridge(int direction)
    {
        return (Corners[direction] + Corners[direction + 1]) * WaterBlendFactor;
    }
    #endregion

    #region Chunk单元块
    /// <summary>
    /// 单元块的大小:大的块更少draw calls，小块有利于裁剪，顶点数就会减少
    /// 注:Unity中Mesh数组最大能存储65000个顶点
    /// What is a good chunk size?
    /// It depends.Using larger chunks means that you'll have fewer but larger meshes.
    /// This leads to fewer draw calls. But smaller chunks work better with frustum
    /// culling, which leads to fewer triangles being drawn.
    /// The pragmatic approach is to just pick a size and fine-tune later.
    /// </summary>
    public const int ChunkSizeX = 5, ChunkSizeZ = 5;

    #endregion

    #region 噪声干扰

    /// <summary>
    /// 海拔干扰度
    /// </summary>
    public const float ElevationPerturbStrength = 1.5f;

    /// <summary>
    /// 噪源
    /// </summary>
    public static Texture2D NoiseSource;

    /// <summary>
    /// 噪声缩放
    /// </summary>
    public const float NoiseScale = 0.003f;

    /// <summary>
    /// 噪声采样
    /// </summary>
    /// <param name="position">顶点位置</param>
    /// <returns>双线性过滤</returns>
    public static Vector4 SampleNoise(Vector3 position)
    {
        return NoiseSource.GetPixelBilinear(position.x * NoiseScale, position.z * NoiseScale);
    }

    public const float cellPerturbStrength = 3f;

    /// <summary>
    /// 噪声干扰
    /// </summary>
    /// <param name="position">位置</param>
    /// <returns>干扰后的位置</returns>
    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
        return position;
    }

    #endregion


    /// <summary>
    /// 方向，N=北，S=南，EPosition=东，WPosition=西
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
    /// 六边形外半径=六边形边长
    /// </summary>
    public const float OuterRadius = 10f;

    /// <summary>
    /// 六边形内半径=0.8*外半径
    /// </summary>
    public const float InnerRadius = OuterRadius * OuterToInner;

    public const float OuterToInner = 0.866025404f;
    public const float InnerToOuter = 1f / OuterToInner;

    /// <summary>
    /// 六边形单元中心本色区域占比
    /// </summary>
    public const float SolidFactor = 0.8f;

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

    public static Vector3 GetFirstCorner(int direction)
    {
        return Corners[direction];
    }

    public static Vector3 GetSecondCorner(int direction)
    {
        return Corners[(direction + 1 > 5 ? 0 : direction + 1)];
    }

    public static Vector3 GetFirstSolidCorner(int direction)
    {
        return Corners[direction] * SolidFactor;
    }

    public static Vector3 GetSecondSolidCorner(int direction)
    {
        return Corners[(direction + 1>5?0:direction+1)] * SolidFactor;
    }

    public static Vector3 GetSolidEdgeMiddle(int direction)
    {
        return (Corners[direction] + Corners[(direction + 1 > 5 ? 0 : direction + 1)]) * (0.5f * SolidFactor);
    }

    /// <summary>
    /// 海拔步长
    /// </summary>
    public const float ElevationStep = 3f;

    /// <summary>
    /// 每个斜坡上的阶梯数
    /// </summary>
    public const int TerracesPerSlope = 5;

    /// <summary>
    /// 阶梯步长
    /// </summary>
    public const int TerraceSteps = TerracesPerSlope * 2 + 1;

    /// <summary>
    /// 水平阶梯步长
    /// </summary>
    public const float HorizontalTerraceStepSize = 1f / TerraceSteps;

    /// <summary>
    /// 垂直阶梯步长
    /// </summary>
    public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);

    /// <summary>
    /// 阶梯插值
    /// </summary>
    /// <param name="a">向量a</param>
    /// <param name="b">向量b</param>
    /// <param name="step">步数</param>
    /// <returns>渐变坡度</returns>
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HexMetrics.HorizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        float v = ((step + 1) / 2) * HexMetrics.VerticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    /// <summary>
    /// 颜色插值
    /// </summary>
    /// <param name="a">颜色A</param>
    /// <param name="b">颜色B</param>
    /// <param name="step">步长</param>
    /// <returns>渐变色</returns>
    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HexMetrics.HorizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    /// <summary>
    /// 获取相邻六边形单元之间的矩形桥
    /// </summary>
    /// <param name="cellIndex">六边形单元索引</param>
    /// <returns>矩形桥</returns>
    public  static Vector3 GetBridge(int cellIndex)
    {
        return (Corners[cellIndex] + Corners[cellIndex + 1]) * BlendFactor;
    }

    #region Perlin Noise
    /// <summary>
    /// Perlin哈希表
    /// </summary>
    private readonly static int[] hash = {
        151,160,137, 91, 90, 15,131, 13,201, 95, 96, 53,194,233,  7,225,
        140, 36,103, 30, 69,142,  8, 99, 37,240, 21, 10, 23,190,  6,148,
        247,120,234, 75,  0, 26,197, 62, 94,252,219,203,117, 35, 11, 32,
        57,177, 33, 88,237,149, 56, 87,174, 20,125,136,171,168, 68,175,
        74,165, 71,134,139, 48, 27,166, 77,146,158,231, 83,111,229,122,
        60,211,133,230,220,105, 92, 41, 55, 46,245, 40,244,102,143, 54,
        65, 25, 63,161,  1,216, 80, 73,209, 76,132,187,208, 89, 18,169,
        200,196,135,130,116,188,159, 86,164,100,109,198,173,186,  3, 64,
        52,217,226,250,124,123,  5,202, 38,147,118,126,255, 82, 85,212,
        207,206, 59,227, 47, 16, 58, 17,182,189, 28, 42,223,183,170,213,
        119,248,152,  2, 44,154,163, 70,221,153,101,155,167, 43,172,  9,
        129, 22, 39,253, 19, 98,108,110, 79,113,224,232,178,185,112,104,
        218,246, 97,228,251, 34,242,193,238,210,144, 12,191,179,162,241,
        81, 51,145,235,249, 14,239,107, 49,192,214, 31,181,199,106,157,
        184, 84,204,176,115,121, 50, 45,127,  4,150,254,138,236,205, 93,
        222,114, 67, 29, 24, 72,243,141,128,195, 78, 66,215, 61,156,180
    };

    /// <summary>
    /// 哈希遮罩
    /// </summary>
    private const int hashMask = 255;

    #region Perlin1D

    public static float Perlin1D(Vector3 point, float frequency)
    {
        point *= frequency;
        int i0 = Mathf.FloorToInt(point.x);
        float t0 = point.x - i0;
        float t1 = t0 - 1f;
        i0 &= hashMask;
        int i1 = i0 + 1;

        float g0 = gradients1D[hash[i0] & gradientsMask1D];
        float g1 = gradients1D[hash[i1] & gradientsMask1D];

        float v0 = g0 * t0;
        float v1 = g1 * t1;

        float t = Smooth(t0);
        return Mathf.Lerp(v0, v1, t) * 2f;
    }

    private static float[] gradients1D = {
        1f, -1f
    };

    private const int gradientsMask1D = 1;

    #endregion

    #region Perlin2D

    public static float Perlin2D(Vector3 point, float frequency)
    {
        point *= frequency;
        int ix0 = Mathf.FloorToInt(point.x);
        int iy0 = Mathf.FloorToInt(point.y);
        float tx0 = point.x - ix0;
        float ty0 = point.y - iy0;
        float tx1 = tx0 - 1f;
        float ty1 = ty0 - 1f;
        ix0 &= hashMask;
        iy0 &= hashMask;
        int ix1 = ix0 + 1;
        int iy1 = iy0 + 1;

        int h0 = hash[ix0];
        int h1 = hash[ix1];
        Vector2 g00 = gradients2D[hash[h0 + iy0] & gradientsMask2D];
        Vector2 g10 = gradients2D[hash[h1 + iy0] & gradientsMask2D];
        Vector2 g01 = gradients2D[hash[h0 + iy1] & gradientsMask2D];
        Vector2 g11 = gradients2D[hash[h1 + iy1] & gradientsMask2D];

        float v00 = Dot(g00, tx0, ty0);
        float v10 = Dot(g10, tx1, ty0);
        float v01 = Dot(g01, tx0, ty1);
        float v11 = Dot(g11, tx1, ty1);

        float tx = Smooth(tx0);
        float ty = Smooth(ty0);
        return Mathf.Lerp(
                   Mathf.Lerp(v00, v10, tx),
                   Mathf.Lerp(v01, v11, tx),
                   ty) * sqr2;
    }

    private readonly static Vector2[] gradients2D = {
        new Vector2( 1f, 0f),
        new Vector2(-1f, 0f),
        new Vector2( 0f, 1f),
        new Vector2( 0f,-1f),
        new Vector2( 1f, 1f).normalized,
        new Vector2(-1f, 1f).normalized,
        new Vector2( 1f,-1f).normalized,
        new Vector2(-1f,-1f).normalized
    };

    private const int gradientsMask2D = 7;
    private static float sqr2 = Mathf.Sqrt(2f);

    #endregion

    #region Perlin3D

    private static Vector3[] gradients3D = {
        new Vector3( 1f, 1f, 0f),
        new Vector3(-1f, 1f, 0f),
        new Vector3( 1f,-1f, 0f),
        new Vector3(-1f,-1f, 0f),
        new Vector3( 1f, 0f, 1f),
        new Vector3(-1f, 0f, 1f),
        new Vector3( 1f, 0f,-1f),
        new Vector3(-1f, 0f,-1f),
        new Vector3( 0f, 1f, 1f),
        new Vector3( 0f,-1f, 1f),
        new Vector3( 0f, 1f,-1f),
        new Vector3( 0f,-1f,-1f),

        new Vector3( 1f, 1f, 0f),
        new Vector3(-1f, 1f, 0f),
        new Vector3( 0f,-1f, 1f),
        new Vector3( 0f,-1f,-1f)
    };

    private const int gradientsMask3D = 15;

    public static float Perlin3D(Vector3 point, float frequency)
    {
        point *= frequency;
        int ix0 = Mathf.FloorToInt(point.x);
        int iy0 = Mathf.FloorToInt(point.y);
        int iz0 = Mathf.FloorToInt(point.z);
        float tx0 = point.x - ix0;
        float ty0 = point.y - iy0;
        float tz0 = point.z - iz0;
        float tx1 = tx0 - 1f;
        float ty1 = ty0 - 1f;
        float tz1 = tz0 - 1f;
        ix0 &= hashMask;
        iy0 &= hashMask;
        iz0 &= hashMask;
        int ix1 = ix0 + 1;
        int iy1 = iy0 + 1;
        int iz1 = iz0 + 1;

        int h0 = hash[ix0];
        int h1 = hash[ix1];
        int h00 = hash[h0 + iy0];
        int h10 = hash[h1 + iy0];
        int h01 = hash[h0 + iy1];
        int h11 = hash[h1 + iy1];
        Vector3 g000 = gradients3D[hash[h00 + iz0] & gradientsMask3D];
        Vector3 g100 = gradients3D[hash[h10 + iz0] & gradientsMask3D];
        Vector3 g010 = gradients3D[hash[h01 + iz0] & gradientsMask3D];
        Vector3 g110 = gradients3D[hash[h11 + iz0] & gradientsMask3D];
        Vector3 g001 = gradients3D[hash[h00 + iz1] & gradientsMask3D];
        Vector3 g101 = gradients3D[hash[h10 + iz1] & gradientsMask3D];
        Vector3 g011 = gradients3D[hash[h01 + iz1] & gradientsMask3D];
        Vector3 g111 = gradients3D[hash[h11 + iz1] & gradientsMask3D];

        float v000 = Dot(g000, tx0, ty0, tz0);
        float v100 = Dot(g100, tx1, ty0, tz0);
        float v010 = Dot(g010, tx0, ty1, tz0);
        float v110 = Dot(g110, tx1, ty1, tz0);
        float v001 = Dot(g001, tx0, ty0, tz1);
        float v101 = Dot(g101, tx1, ty0, tz1);
        float v011 = Dot(g011, tx0, ty1, tz1);
        float v111 = Dot(g111, tx1, ty1, tz1);

        float tx = Smooth(tx0);
        float ty = Smooth(ty0);
        float tz = Smooth(tz0);
        return Mathf.Lerp(
            Mathf.Lerp(Mathf.Lerp(v000, v100, tx), Mathf.Lerp(v010, v110, tx), ty),
            Mathf.Lerp(Mathf.Lerp(v001, v101, tx), Mathf.Lerp(v011, v111, tx), ty),
            tz);
    }

    #endregion

    private static float Dot(Vector3 g, float x, float y, float z)
    {
        return g.x * x + g.y * y + g.z * z;
    }

    private static float Dot(Vector2 g, float x, float y)
    {
        return g.x * x + g.y * y;
    }

    /// <summary>
    /// 平滑
    /// </summary>
    /// <param name="t">参数</param>
    /// <returns></returns>
    private static float Smooth(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
    #endregion
}