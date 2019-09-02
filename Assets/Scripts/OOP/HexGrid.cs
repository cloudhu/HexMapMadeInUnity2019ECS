using Unity.Entities;
using UnityEngine;

/// <summary>
/// 地图网格，管理地图块
/// </summary>
public class HexGrid : MonoBehaviour
{

    /// <summary>
    /// 地图块的数量
    /// </summary>
    public int chunkCountX = 4, chunkCountZ = 3;

    /// <summary>
    /// 噪声采样纹理图
    /// </summary>
    public Texture2D noiseSource;

    /// <summary>
    /// 地图块预设
    /// </summary>
    public HexGridChunk chunkPrefab;

    /// <summary>
    /// 地图宽度（以六边形为基本单位）
    /// </summary>
    private int cellCountX;

    /// <summary>
    /// 地图长度（以六边形为基本单位）
    /// </summary>
    private int cellCountZ;
    /// <summary>
    /// 地图块数组
    /// </summary>
    HexGridChunk[] chunks;

    #region Mono

    private void Awake()
    {
        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;
        HexMetrics.noiseSource = noiseSource;
        CreateChunks();
    }

    void OnEnable()
    {
        HexMetrics.noiseSource = noiseSource;
    }

    // Start is called before the first frame update
    void Start()
    {
        MainWorld.Instance.SetupMap(cellCountX, cellCountZ,chunkCountX);
    }

    #endregion
    /// <summary>
    /// 添加单元到地图块
    /// </summary>
    /// <param name="chunkId">地图块编号</param>
    /// <param name="chunkIndex">地图块索引</param>
    /// <param name="cellIndex">单元索引</param>
    /// <param name="cell">单元</param>
    public void AddCellToChunk(int chunkId,int chunkIndex,int cellIndex,Entity cell)
    {
        HexGridChunk chunk = chunks[chunkId];
        chunk.AddCell(chunkIndex,cellIndex, cell);
    }

    /// <summary>
    /// 刷新地图块
    /// </summary>
    /// <param name="chunkId">地图块编号</param>
    public void Refresh(int chunkId)
    {
        HexGridChunk chunk = chunks[chunkId];
        chunk.Refresh();
    }

    /// <summary>
    /// 更新地图块
    /// </summary>
    /// <param name="chunkId">地图块编号</param>
    /// <param name="cellIndex">单元索引</param>
    /// <param name="color">颜色</param>
    /// <param name="elevation">海拔</param>
    /// <param name="affected">是否受影响</param>
    /// <param name="brushSize">刷子大小</param>
    public void UpdateChunk( int chunkId, int cellIndex, Color color, int elevation,bool affected=false,int brushSize=0)
    {
        if (chunkId==int.MinValue)
        {
            return;
        }
        HexGridChunk chunk = chunks[chunkId];
        StartCoroutine(chunk.UpdateChunk(cellIndex,color,elevation,affected,brushSize));
    }

    /// <summary>
    /// 创建地图块
    /// </summary>
    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }
}
