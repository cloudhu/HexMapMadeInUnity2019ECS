using System.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 地图块
/// </summary>
public class HexGridChunk : MonoBehaviour
{
    public HexMesh terrain;
    public HexRiver rivers;
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
        StartCoroutine(terrain.Triangulate(cells));
        StartCoroutine(rivers.Triangulate(cells));
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
            NeighborsIndex neighborsIndexs = m_EntityManager.GetComponentData<NeighborsIndex>(cells[chunkIndex]);
            int NEIndex = neighborsIndexs.NEIndex;
            if (NEIndex > int.MinValue )
            {
                if (GetChunkIndex(NEIndex) == int.MinValue)
                {
                    MainWorld.Instance.AffectedChunk(NEIndex, NEIndex, 0, false);
                }
            }
            if (neighborsIndexs.EIndex > int.MinValue && GetChunkIndex(neighborsIndexs.EIndex) == int.MinValue)
            {
                MainWorld.Instance.AffectedChunk(neighborsIndexs.EIndex, neighborsIndexs.EIndex, 0, false);
            }

            if (neighborsIndexs.SEIndex > int.MinValue && GetChunkIndex(neighborsIndexs.SEIndex) == int.MinValue)
            {
                MainWorld.Instance.AffectedChunk(neighborsIndexs.SEIndex, neighborsIndexs.SEIndex, 0, false);
            }

            if (neighborsIndexs.SWIndex > int.MinValue && GetChunkIndex(neighborsIndexs.SWIndex) == int.MinValue)
            {
                MainWorld.Instance.AffectedChunk(neighborsIndexs.SWIndex, neighborsIndexs.SWIndex, 0, false);
            }
            if (neighborsIndexs.WIndex > int.MinValue && GetChunkIndex(neighborsIndexs.WIndex) == int.MinValue)
            {
                MainWorld.Instance.AffectedChunk(neighborsIndexs.WIndex, neighborsIndexs.WIndex, 0, false);
            }

            if (neighborsIndexs.NWIndex > int.MinValue && GetChunkIndex(neighborsIndexs.NWIndex) == int.MinValue)
            {
                MainWorld.Instance.AffectedChunk(neighborsIndexs.NWIndex, neighborsIndexs.NWIndex, 0, false);
            }

            UpdateData data = new UpdateData
            {
                CellIndex = cellIndex,
                NewColor = color,
                Elevation = elevation,
                NEIndex=NEIndex,
                EIndex= neighborsIndexs.EIndex,
                SEIndex= neighborsIndexs.SEIndex,
                SWIndex= neighborsIndexs.SWIndex,
                WIndex= neighborsIndexs.WIndex,
                NWIndex= neighborsIndexs.NWIndex
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
                NeighborsIndex cell = m_EntityManager.GetComponentData<NeighborsIndex>(entity);
                if (chunkMap[i] == cellIndex)
                {
                    //检测六个方向可能受影响的地图块，将变化传递过去
                    if (cell.NEIndex>int.MinValue && GetChunkIndex(cell.NEIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(cell.NEIndex,cellIndex, 0, true);
                        Debug.Log(cellIndex + "影响NE：" + cell.NEIndex);
                    }

                    if (cell.EIndex > int.MinValue && GetChunkIndex(cell.EIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(cell.EIndex, cellIndex, 0, true);
                        Debug.Log(cellIndex + "影响E：" + cell.EIndex);
                    }

                    if (cell.SEIndex > int.MinValue && GetChunkIndex(cell.SEIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(cell.SEIndex, cellIndex, 0, true);
                        Debug.Log(cellIndex + "影响SE：" + cell.SEIndex);
                    }

                    if (cell.SWIndex > int.MinValue && GetChunkIndex(cell.SWIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(cell.SWIndex, cellIndex, 0, true);
                        Debug.Log(cellIndex + "影响SW：" + cell.SWIndex);
                    }
                    if (cell.WIndex > int.MinValue && GetChunkIndex(cell.WIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(cell.WIndex, cellIndex,0, true);
                        Debug.Log(cellIndex + "影响W：" + cell.WIndex);
                    }

                    if (cell.NWIndex > int.MinValue && GetChunkIndex(cell.NWIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(cell.NWIndex, cellIndex, 0, true);
                        Debug.Log(cellIndex + "影响NW：" + cell.NWIndex);
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
