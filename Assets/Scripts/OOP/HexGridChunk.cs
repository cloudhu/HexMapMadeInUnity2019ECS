using System.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 地图块
/// </summary>
public class HexGridChunk : MonoBehaviour
{
    public HexMesh Terrain;
    public HexRiver River;
    public HexRoad Road;
    public HexWater Water;
    public WaterShore Shore;
    public Estuary Estuary;
    private Entity[] cells;
    private int cellCount = 0;
    //地图块和总地图索引配对表
    private int[] chunkMap;
    //实体管理器缓存
    private EntityManager m_EntityManager;

    void Awake()
    {
        //初始化
        cellCount = HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ;
        cells = new Entity[cellCount];
        chunkMap = new int[cellCount];
    }

    private void Start()
    {
        m_EntityManager = MainWorld.Instance.GetEntityManager();
    }

    /// <summary>
    /// 添加单元
    /// </summary>
    /// <param name="chunkIndex">块内索引</param>
    /// <param name="cellIndex">单元索引</param>
    /// <param name="cell">单元</param>
    public void AddCell(int chunkIndex,int cellIndex,Entity cell)
    {
        cells[chunkIndex] = cell;
        chunkMap[chunkIndex] = cellIndex;
        if (chunkIndex+1==cellCount)
        {
            Refresh();
        }
    }

    /// <summary>
    /// 刷新地图块
    /// </summary>
    public void Refresh()
    {
        StartCoroutine(Terrain.Triangulate(cells));
        StartCoroutine(River.Triangulate(cells));
        StartCoroutine(Road.Triangulate(cells));
        StartCoroutine(Water.Triangulate(cells));
        StartCoroutine(Shore.Triangulate(cells));
        StartCoroutine(Estuary.Triangulate(cells));
    }

    /// <summary>
    /// 更新单元的颜色
    /// </summary>
    /// <param name="cellIndex">单元索引</param>
    /// <param name="color">颜色</param>
    /// <returns></returns>
   public IEnumerator UpdateChunk(int cellIndex, Color color, int elevation,bool affected=false,int brushSize=0)
    {
        yield return null;
        
        if (brushSize > 0)
        {
            int chunkIndex = GetChunkIndex(cellIndex);
            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(cells[chunkIndex]);
            int NEIndex = neighbors.NEIndex;
            if (NEIndex > int.MinValue )
            {
                if (GetChunkIndex(NEIndex) == int.MinValue)
                {
                    MainWorld.Instance.AffectedChunk(NEIndex, NEIndex, 0, false);
                }
            }
            if (neighbors.EIndex > int.MinValue && GetChunkIndex(neighbors.EIndex) == int.MinValue)
            {
                MainWorld.Instance.AffectedChunk(neighbors.EIndex, neighbors.EIndex, 0, false);
            }

            if (neighbors.SEIndex > int.MinValue && GetChunkIndex(neighbors.SEIndex) == int.MinValue)
            {
                MainWorld.Instance.AffectedChunk(neighbors.SEIndex, neighbors.SEIndex, 0, false);
            }

            if (neighbors.SWIndex > int.MinValue && GetChunkIndex(neighbors.SWIndex) == int.MinValue)
            {
                MainWorld.Instance.AffectedChunk(neighbors.SWIndex, neighbors.SWIndex, 0, false);
            }
            if (neighbors.WIndex > int.MinValue && GetChunkIndex(neighbors.WIndex) == int.MinValue)
            {
                MainWorld.Instance.AffectedChunk(neighbors.WIndex, neighbors.WIndex, 0, false);
            }

            if (neighbors.NWIndex > int.MinValue && GetChunkIndex(neighbors.NWIndex) == int.MinValue)
            {
                MainWorld.Instance.AffectedChunk(neighbors.NWIndex, neighbors.NWIndex, 0, false);
            }

            UpdateData data = new UpdateData
            {
                CellIndex = cellIndex,
                NewColor = color,
                Elevation = elevation,
                NEIndex=NEIndex,
                EIndex= neighbors.EIndex,
                SEIndex= neighbors.SEIndex,
                SWIndex= neighbors.SWIndex,
                WIndex= neighbors.WIndex,
                NWIndex= neighbors.NWIndex
            };
            for (int i = 0; i < cellCount; i++)
            {
                Entity entity = cells[i];
                if (!m_EntityManager.HasComponent<UpdateData>(entity)) m_EntityManager.AddComponent<UpdateData>(entity);
                m_EntityManager.SetComponentData(entity, data);
            }
        }
        else
        {
            UpdateData data = new UpdateData
            {
                CellIndex = cellIndex,
                NewColor = color,
                Elevation = elevation,
                NEIndex = int.MinValue,
                EIndex = int.MinValue,
                SEIndex = int.MinValue,
                SWIndex = int.MinValue,
                WIndex = int.MinValue,
                NWIndex = int.MinValue
            };
            for (int i = 0; i < cellCount; i++)
            {
                Entity entity = cells[i];

                if (!m_EntityManager.HasComponent<UpdateData>(entity)) m_EntityManager.AddComponent<UpdateData>(entity);
                m_EntityManager.SetComponentData(entity, data);
                if (affected) continue;//如果当前地图块是受影响的，则跳过
                Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                if (chunkMap[i] == cellIndex)
                {
                    //检测六个方向可能受影响的地图块，将变化传递过去
                    if (neighbors.NEIndex>int.MinValue && GetChunkIndex(neighbors.NEIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(neighbors.NEIndex,cellIndex, 0, true);
                        Debug.Log(cellIndex + "影响NE：" + neighbors.NEIndex);
                    }

                    if (neighbors.EIndex > int.MinValue && GetChunkIndex(neighbors.EIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(neighbors.EIndex, cellIndex, 0, true);
                        Debug.Log(cellIndex + "影响E：" + neighbors.EIndex);
                    }

                    if (neighbors.SEIndex > int.MinValue && GetChunkIndex(neighbors.SEIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(neighbors.SEIndex, cellIndex, 0, true);
                        Debug.Log(cellIndex + "影响SE：" + neighbors.SEIndex);
                    }

                    if (neighbors.SWIndex > int.MinValue && GetChunkIndex(neighbors.SWIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(neighbors.SWIndex, cellIndex, 0, true);
                        Debug.Log(cellIndex + "影响SW：" + neighbors.SWIndex);
                    }
                    if (neighbors.WIndex > int.MinValue && GetChunkIndex(neighbors.WIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(neighbors.WIndex, cellIndex,0, true);
                        Debug.Log(cellIndex + "影响W：" + neighbors.WIndex);
                    }

                    if (neighbors.NWIndex > int.MinValue && GetChunkIndex(neighbors.NWIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(neighbors.NWIndex, cellIndex, 0, true);
                        Debug.Log(cellIndex + "影响NW：" + neighbors.NWIndex);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取块内索引
    /// </summary>
    /// <param name="cellIndex">单元索引</param>
    /// <returns></returns>
    private int GetChunkIndex(int cellIndex)
    {
        if (cellIndex!=int.MinValue)
        {
            for (int i = 0; i < cellCount; i++)
            {
                if (chunkMap[i] == cellIndex)
                {
                    return i;
                }
            }
        }

        return int.MinValue;
    }

    private void OnDestroy()
    {
        cells = null;
        chunkMap = null;
    }
}
