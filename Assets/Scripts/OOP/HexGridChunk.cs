using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    HexMesh hexMesh;
    private Entity[] cells;
    private int cellCount = 0;

    public int chunkId = int.MinValue;
    private int[] chunkMap;

    private EntityManager m_EntityManager;

    void Awake()
    {

        hexMesh = GetComponentInChildren<HexMesh>();
        cellCount = HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ;
        cells = new Entity[cellCount];
        chunkMap = new int[cellCount];

    }

    private void Start()
    {
        m_EntityManager = MainWorld.Instance.GetEntityManager();
    }

    public void AddCell(int chunkIndex,int cellIndex,Entity cell)
    {
        cells[chunkIndex] = cell;
        chunkMap[chunkIndex] = cellIndex;
        if (chunkIndex+1==cellCount)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        StartCoroutine(hexMesh.Triangulate(cells));
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
        
        Debug.Log("UpdateChunk:" +chunkId);
        if (brushSize > 0)
        {
            NativeList<int> updateList = new NativeList<int>((brushSize * (1 + brushSize) * 3 + 1),Allocator.Temp);
            NativeList<int> affectList = new NativeList<int>((1 + brushSize) * 6, Allocator.Temp);
            int chunkIndex = GetChunkIndex(cellIndex);
            if (chunkIndex!=int.MinValue)
            {
                updateList.Add(cellIndex);
                NeighborsIndex cell = m_EntityManager.GetComponentData<NeighborsIndex>(cells[chunkIndex]);
                Brush(cell,brushSize,ref updateList,ref affectList);
            }

            //Debug.Log("UpdateChunkList:" + updateList.Length);
            if (updateList.Length > 0)
            {
                for (int i = 0; i < cellCount; i++)
                {
                    Entity entity = cells[i];
                    NeighborsIndex neighborsIndex = m_EntityManager.GetComponentData<NeighborsIndex>(entity);
                    Cell cell = m_EntityManager.GetComponentData<Cell>(entity);
                    if (updateList.Contains(chunkMap[i]))
                    {
                        Vector3 position = cell.Position;
                        position.y=elevation * HexMetrics.elevationStep;
                        m_EntityManager.SetComponentData(entity, new Cell
                        {
                            Index = chunkMap[i],
                            Color = color,
                            Elevation = elevation,
                            Position=position
                        });
                        //更新相邻单元的颜色
                        if (updateList.Contains(neighborsIndex.NEIndex))
                        {
                            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                            m_EntityManager.SetComponentData(entity, new Neighbors
                            {
                                NE = color,
                                E = neighbors.E,
                                SE = neighbors.SE,
                                SW = neighbors.SW,
                                W = neighbors.W,
                                NW = neighbors.NW,
                                NEElevation = elevation,
                                EElevation = neighbors.EElevation,
                                SEElevation = neighbors.SEElevation,
                                SWElevation = neighbors.SWElevation,
                                WElevation = neighbors.WElevation,
                                NWElevation = neighbors.NWElevation
                            });
                        }

                        if (updateList.Contains(neighborsIndex.EIndex))
                        {
                            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                            m_EntityManager.SetComponentData(entity, new Neighbors
                            {
                                NE = neighbors.NE,
                                E = color,
                                SE = neighbors.SE,
                                SW = neighbors.SW,
                                W = neighbors.W,
                                NW = neighbors.NW,
                                NEElevation = neighbors.NEElevation,
                                EElevation = elevation,
                                SEElevation = neighbors.SEElevation,
                                SWElevation = neighbors.SWElevation,
                                WElevation = neighbors.WElevation,
                                NWElevation = neighbors.NWElevation
                            });
                        }
                        if (updateList.Contains(neighborsIndex.SEIndex))
                        {
                            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                            m_EntityManager.SetComponentData(entity, new Neighbors
                            {
                                NE = neighbors.NE,
                                E = neighbors.E,
                                SE = color,
                                SW = neighbors.SW,
                                W = neighbors.W,
                                NW = neighbors.NW,
                                NEElevation = neighbors.NEElevation,
                                EElevation = neighbors.EElevation,
                                SEElevation = elevation,
                                SWElevation = neighbors.SWElevation,
                                WElevation = neighbors.WElevation,
                                NWElevation = neighbors.NWElevation
                            });
                        }

                        if (updateList.Contains(neighborsIndex.SWIndex))
                        {
                            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                            m_EntityManager.SetComponentData(entity, new Neighbors
                            {
                                NE = neighbors.NE,
                                E = neighbors.E,
                                SE = neighbors.SE,
                                SW = color,
                                W = neighbors.W,
                                NW = neighbors.NW,
                                NEElevation = neighbors.NEElevation,
                                EElevation = neighbors.EElevation,
                                SEElevation = neighbors.SEElevation,
                                SWElevation = elevation,
                                WElevation = neighbors.WElevation,
                                NWElevation = neighbors.NWElevation
                            });
                        }

                        if (updateList.Contains(neighborsIndex.WIndex))
                        {
                            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                            m_EntityManager.SetComponentData(entity, new Neighbors
                            {
                                NE = neighbors.NE,
                                E = neighbors.E,
                                SE = neighbors.SE,
                                SW = neighbors.SW,
                                W = color,
                                NW = neighbors.NW,
                                NEElevation = neighbors.NEElevation,
                                EElevation = neighbors.EElevation,
                                SEElevation = neighbors.SEElevation,
                                SWElevation = neighbors.SWElevation,
                                WElevation = elevation,
                                NWElevation = neighbors.NWElevation
                            });
                        }

                        if (updateList.Contains(neighborsIndex.NWIndex))
                        {
                            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                            m_EntityManager.SetComponentData(entity, new Neighbors
                            {
                                NE = neighbors.NE,
                                E = neighbors.E,
                                SE = neighbors.SE,
                                SW = neighbors.SW,
                                W = neighbors.W,
                                NW = color,
                                NEElevation = neighbors.NEElevation,
                                EElevation = neighbors.EElevation,
                                SEElevation = neighbors.SEElevation,
                                SWElevation = neighbors.SWElevation,
                                WElevation = neighbors.WElevation,
                                NWElevation = elevation
                            });
                        }
                    }
                    else if (affectList.Contains(chunkMap[i]))
                    {

                        //更新相邻单元的颜色
                        if (updateList.Contains(neighborsIndex.NEIndex))
                        {
                            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                            m_EntityManager.SetComponentData(entity, new Neighbors
                            {
                                NE = color,
                                E = neighbors.E,
                                SE = neighbors.SE,
                                SW = neighbors.SW,
                                W = neighbors.W,
                                NW = neighbors.NW,
                                NEElevation = elevation,
                                EElevation = neighbors.EElevation,
                                SEElevation = neighbors.SEElevation,
                                SWElevation = neighbors.SWElevation,
                                WElevation = neighbors.WElevation,
                                NWElevation = neighbors.NWElevation
                            });
                        }

                        if (updateList.Contains(neighborsIndex.EIndex))
                        {
                            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                            m_EntityManager.SetComponentData(entity, new Neighbors
                            {
                                NE = neighbors.NE,
                                E = color,
                                SE = neighbors.SE,
                                SW = neighbors.SW,
                                W = neighbors.W,
                                NW = neighbors.NW,
                                NEElevation = neighbors.NEElevation,
                                EElevation =elevation,
                                SEElevation = neighbors.SEElevation,
                                SWElevation = neighbors.SWElevation,
                                WElevation = neighbors.WElevation,
                                NWElevation = neighbors.NWElevation
                            });
                        }
                        if (updateList.Contains(neighborsIndex.SEIndex))
                        {
                            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                            m_EntityManager.SetComponentData(entity, new Neighbors
                            {
                                NE = neighbors.NE,
                                E = neighbors.E,
                                SE = color,
                                SW = neighbors.SW,
                                W = neighbors.W,
                                NW = neighbors.NW,
                                NEElevation = neighbors.NEElevation,
                                EElevation = neighbors.EElevation,
                                SEElevation = elevation,
                                SWElevation = neighbors.SWElevation,
                                WElevation = neighbors.WElevation,
                                NWElevation = neighbors.NWElevation
                            });
                        }

                        if (updateList.Contains(neighborsIndex.SWIndex))
                        {
                            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                            m_EntityManager.SetComponentData(entity, new Neighbors
                            {
                                NE = neighbors.NE,
                                E = neighbors.E,
                                SE = neighbors.SE,
                                SW =color ,
                                W = neighbors.W,
                                NW = neighbors.NW,
                                NEElevation = neighbors.NEElevation,
                                EElevation = neighbors.EElevation,
                                SEElevation = neighbors.SEElevation,
                                SWElevation = elevation,
                                WElevation = neighbors.WElevation,
                                NWElevation = neighbors.NWElevation
                            });
                        }

                        if (updateList.Contains(neighborsIndex.WIndex))
                        {
                            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                            m_EntityManager.SetComponentData(entity, new Neighbors
                            {
                                NE = neighbors.NE,
                                E = neighbors.E,
                                SE = neighbors.SE,
                                SW = neighbors.SW,
                                W = color,
                                NW = neighbors.NW,
                                NEElevation = neighbors.NEElevation,
                                EElevation = neighbors.EElevation,
                                SEElevation = neighbors.SEElevation,
                                SWElevation = neighbors.SWElevation,
                                WElevation = elevation,
                                NWElevation = neighbors.NWElevation
                            });
                        }

                        if (updateList.Contains(neighborsIndex.NWIndex))
                        {
                            Neighbors neighbors = m_EntityManager.GetComponentData<Neighbors>(entity);
                            m_EntityManager.SetComponentData(entity, new Neighbors
                            {
                                NE = neighbors.NE,
                                E = neighbors.E,
                                SE = neighbors.SE,
                                SW = neighbors.SW,
                                W = neighbors.W,
                                NW =color ,
                                NEElevation = neighbors.NEElevation,
                                EElevation = neighbors.EElevation,
                                SEElevation = neighbors.SEElevation,
                                SWElevation = neighbors.SWElevation,
                                WElevation = neighbors.WElevation,
                                NWElevation = elevation
                            });
                        }
                    }
                    else
                    {
                        
                    }

                    m_EntityManager.AddComponent<NewDataTag>(entity);
                }
            }
            updateList.Dispose();
            affectList.Dispose();
        }
        else
        {
            for (int i = 0; i < cellCount; i++)
            {
                Entity entity = cells[i];

                if (!m_EntityManager.HasComponent<UpdateData>(entity)) m_EntityManager.AddComponent<UpdateData>(entity);
                m_EntityManager.SetComponentData(entity, new UpdateData
                {
                    CellIndex = cellIndex,
                    NewColor = color,
                    Elevation = elevation
                });
                if (affected) continue;//如果当前地图块是受影响的，则跳过
                NeighborsIndex cell = m_EntityManager.GetComponentData<NeighborsIndex>(entity);
                if (chunkMap[i] == cellIndex)
                {
                    //检测六个方向可能受影响的地图块，将变化传递过去
                    if (cell.NEIndex>int.MinValue && GetChunkIndex(cell.NEIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(cell.NEIndex, 0, true);
                        Debug.Log(cellIndex + "影响NE：" + cell.NEIndex);
                    }

                    if (cell.EIndex > int.MinValue && GetChunkIndex(cell.EIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(cell.EIndex, 0, true);
                        Debug.Log(cellIndex + "影响E：" + cell.EIndex);
                    }

                    if (cell.SEIndex > int.MinValue && GetChunkIndex(cell.SEIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(cell.SEIndex, 0, true);
                        Debug.Log(cellIndex + "影响SE：" + cell.SEIndex);
                    }

                    if (cell.SWIndex > int.MinValue && GetChunkIndex(cell.SWIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(cell.SWIndex, 0, true);
                        Debug.Log(cellIndex + "影响SW：" + cell.SWIndex);
                    }
                    if (cell.WIndex > int.MinValue && GetChunkIndex(cell.WIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(cell.WIndex, 0, true);
                        Debug.Log(cellIndex + "影响W：" + cell.WIndex);
                    }

                    if (cell.NWIndex > int.MinValue && GetChunkIndex(cell.NWIndex) == int.MinValue)
                    {
                        MainWorld.Instance.AffectedChunk(cell.NWIndex, 0, true);
                        Debug.Log(cellIndex + "影响NW：" + cell.NWIndex);
                    }
                }
            }
        }


    }

    void Brush(NeighborsIndex cell,int brushSize,ref NativeList<int> updateList, ref NativeList<int> affectList)
    {
        Debug.Log("Brush brushSize:"+ brushSize+ " )))Brush updateList:" + updateList.Length);
        NativeList<int> tempArr = new NativeList<int>(6,Allocator.Temp); 
        //东南西北六个方向相邻的单元都加进列表
        if (cell.NEIndex > 0 && !updateList.Contains(cell.NEIndex))
        {
            tempArr.Add(cell.NEIndex);
        }
        if (cell.EIndex > 0 && !updateList.Contains(cell.EIndex))
        {
            tempArr.Add(cell.EIndex);
        }
        if (cell.SEIndex > 0 && !updateList.Contains(cell.SEIndex))
        {
            tempArr.Add(cell.SEIndex);
        }
        if (cell.SWIndex > 0 && !updateList.Contains(cell.SWIndex))
        {
            tempArr.Add(cell.SWIndex);
        }
        if (cell.WIndex > 0 && !updateList.Contains(cell.WIndex))
        {
            tempArr.Add(cell.WIndex);
        }
        if (cell.NWIndex > 0 && !updateList.Contains(cell.NWIndex))
        {
            tempArr.Add(cell.NWIndex);
        }
        //循环递归，对不同的单元进行分别处理
        for (int i = 0; i < tempArr.Length; i++)
        {
            int tmpIndex = tempArr[i];
            int chunkIndex = GetChunkIndex(tmpIndex);
            if (chunkIndex > 0)
            {
                if (brushSize==0)
                {
                    affectList.Add(tmpIndex);
                }
                else
                {
                    updateList.Add(tmpIndex);
                    NeighborsIndex cellNext = m_EntityManager.GetComponentData<NeighborsIndex>(cells[chunkIndex]);
                    Brush(cellNext, brushSize - 1, ref updateList,ref affectList);
                }
            }
            else
            {
                MainWorld.Instance.AffectedChunk(tmpIndex, brushSize - 1, brushSize == 0);
            }
        }
        tempArr.Dispose();

    }

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
