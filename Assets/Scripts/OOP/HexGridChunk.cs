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
    //private NativeList<int> affectList;

    void Awake()
    {

        hexMesh = GetComponentInChildren<HexMesh>();
        cellCount = HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ;
        cells = new Entity[cellCount];
        chunkMap = new int[cellCount];
        //affectList = new NativeList<int>(6,Allocator.Persistent);
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
   public IEnumerator UpdateCell(int cellIndex, Color color, int elevation,bool affected=false)
    {
        yield return null;
        EntityManager m_EntityManager = MainWorld.Instance.GetEntityManager();
        Debug.Log("UpdateChunk:" +chunkId);
        for (int i = 0; i < cellCount; i++)
        {
            Entity entity = cells[i];
            if (m_EntityManager.HasComponent<UpdateData>(entity)) continue;
            m_EntityManager.AddComponentData(entity, new UpdateData
            {
                CellIndex = cellIndex,
                NewColor = color,
                Elevation = elevation
            });
            if (affected) continue;//如果当前地图块是受影响的，则跳过
            Cell cell = m_EntityManager.GetComponentData<Cell>(entity);
            if (cell.Index==cellIndex)
            {
                //检测六个方向可能受影响的地图块，将变化传递过去
                if (cell.NEIndex!= int.MinValue && GetChunkId(cell.NEIndex)!= chunkId)
                {
                    MainWorld.Instance.AffectedChunk(cell.NEIndex);
                    Debug.Log(cellIndex + "影响：" + cell.NEIndex);
                }

                if (cell.EIndex != int.MinValue && GetChunkId(cell.EIndex) != chunkId)
                {
                    MainWorld.Instance.AffectedChunk(cell.EIndex);
                    Debug.Log(cellIndex + "影响：" + cell.EIndex);
                }

                if (cell.SEIndex != int.MinValue && GetChunkId(cell.SEIndex) != chunkId)
                {
                    MainWorld.Instance.AffectedChunk(cell.SEIndex);
                    Debug.Log(cellIndex + "影响：" + cell.SEIndex);
                }

                if (cell.SWIndex != int.MinValue && GetChunkId(cell.SWIndex) != chunkId)
                {
                    MainWorld.Instance.AffectedChunk(cell.SWIndex);
                    Debug.Log(cellIndex + "影响：" + cell.SWIndex);
                }
                if (cell.WIndex != int.MinValue && GetChunkId(cell.WIndex) != chunkId)
                {
                    MainWorld.Instance.AffectedChunk(cell.WIndex);
                    Debug.Log(cellIndex + "影响：" + cell.WIndex);
                }

                if (cell.NWIndex != int.MinValue && GetChunkId(cell.NWIndex) != chunkId)
                {
                    MainWorld.Instance.AffectedChunk(cell.NWIndex);
                    Debug.Log(cellIndex + "影响：" + cell.NWIndex);
                }
            }
        }
    }

    public int GetChunkId(int cellIndex)
    {
        for (int i = 0; i < cellCount; i++)
        {
            if (chunkMap[i]== cellIndex)
            {
                return chunkId;
            }
        }

        return int.MinValue;
    }

    private void OnDestroy()
    {
        //affectList.Dispose();
    }
}
