using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexWall : MonoBehaviour {
    Mesh m_Mesh;

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = m_Mesh = new Mesh();
        m_Mesh.name = "Hex Mesh";
        m_Mesh.MarkDynamic();
    }

    public IEnumerator Triangulate(Entity[] cells)
    {
        yield return null;
        int totalCount = cells.Length;
        EntityManager m_EntityManager = MainWorld.Instance.GetEntityManager();
        NativeList<Vector3> Vertices = new NativeList<Vector3>(totalCount, Allocator.Temp);
        NativeList<int> Triangles = new NativeList<int>(totalCount, Allocator.Temp);
        for (int i = 0; i < totalCount; i++)
        {
            //0.取出实体，如果实体的索引为m_Builder则跳过
            Entity entity = cells[i];
            Cell cell = m_EntityManager.GetComponentData<Cell>(entity);
            if (cell.HasWall)
            {
                DynamicBuffer<WallBuffer> wallBuffers = m_EntityManager.GetBuffer<WallBuffer>(entity);
                if (wallBuffers.Length > 0)
                {
                    for (int j = 0; j < wallBuffers.Length; j++)
                    {
                        Triangles.Add(Vertices.Length);
                        Vertices.Add(wallBuffers[j]);
                    }
                    wallBuffers.Clear();
                }
            }
        }

        Debug.Log("-------------------------------------HexWall----------------------------------------------------");
        Debug.Log("Vertices=" + Vertices.Length + "----Triangles=" + Triangles.Length);

        if (Vertices.Length > 1)
        {
            m_Mesh.Clear();
            m_Mesh.vertices = Vertices.ToArray();
            m_Mesh.triangles = Triangles.ToArray();
            m_Mesh.RecalculateNormals();
            m_Mesh.Optimize();
        }
        Vertices.Dispose();
        Triangles.Dispose();
    }
}

