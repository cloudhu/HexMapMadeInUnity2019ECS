using Unity.Entities;
using UnityEngine;

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

    public HexGridChunk chunkPrefab;

    /// <summary>
    /// 地图宽度（以六边形为基本单位）
    /// </summary>
    private int cellCountX;

    /// <summary>
    /// 地图长度（以六边形为基本单位）
    /// </summary>
    private int cellCountZ;
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

    public void AddCellToChunk(int chunkId,int chunkIndex,int cellIndex,Entity cell)
    {
        HexGridChunk chunk = chunks[chunkId];
        chunk.AddCell(chunkIndex,cellIndex, cell);
    }

    public void Refresh(int chunkId)
    {
        HexGridChunk chunk = chunks[chunkId];
        chunk.Refresh();
    }

    public void UpdateChunk( int chunkId, int cellIndex, Color color, int elevation,bool affected=false,int brushSize=0)
    {
        if (chunkId==int.MinValue)
        {
            return;
        }
        HexGridChunk chunk = chunks[chunkId];
        StartCoroutine(chunk.UpdateChunk(cellIndex,color,elevation,affected,brushSize));
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
                chunk.chunkId = i-1;
            }
        }
    }
}
