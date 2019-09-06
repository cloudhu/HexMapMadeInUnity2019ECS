using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterShore : MonoBehaviour {
    Mesh m_Mesh;

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = m_Mesh = new Mesh();
        m_Mesh.name = "Hex Mesh";
        m_Mesh.MarkDynamic();
    }

    public IEnumerator Triangulate(Entity[] cells)
    {
        yield return null;//new WaitForSeconds(0.01f);
        int totalCount = cells.Length;
        EntityManager m_EntityManager = MainWorld.Instance.GetEntityManager();
        NativeList<Vector3> Vertices = new NativeList<Vector3>(totalCount, Allocator.Temp);
        NativeList<int> Triangles = new NativeList<int>(totalCount, Allocator.Temp);
        NativeList<Vector2> uvs = new NativeList<Vector2>(totalCount, Allocator.Temp);
        for (int i = 0; i < totalCount; i++)
        {
            //0.取出实体，如果实体的索引为m_Builder则跳过
            Entity entity = cells[i];
            Cell cell = m_EntityManager.GetComponentData<Cell>(entity);
            if (cell.IsUnderWater)
            {
                DynamicBuffer<WaterShoreBuffer> riverBuffers = m_EntityManager.GetBuffer<WaterShoreBuffer>(entity);
                if (riverBuffers.Length > 0)
                {
                    DynamicBuffer<ShoreUvBuffer> uvBuffers = m_EntityManager.GetBuffer<ShoreUvBuffer>(entity);
                    for (int j = 0; j < riverBuffers.Length; j++)
                    {
                        Triangles.Add(Vertices.Length);
                        Vertices.Add(riverBuffers[j]);
                        uvs.Add(uvBuffers[j]);
                    }
                    uvBuffers.Clear();
                    riverBuffers.Clear();
                }
            }
        }

        Debug.Log("---------------------------------------WaterShore--------------------------------------------------");
        Debug.Log("Vertices=" + Vertices.Length + "----Triangles=" + Triangles.Length + "----UV=" + uvs.Length);

        if (Vertices.Length > 1)
        {
            m_Mesh.Clear();
            m_Mesh.vertices = Vertices.ToArray();
            m_Mesh.triangles = Triangles.ToArray();
            m_Mesh.uv = uvs.ToArray();
            m_Mesh.RecalculateNormals();
            m_Mesh.Optimize();
        }
        Vertices.Dispose();
        Triangles.Dispose();
        uvs.Dispose();
    }
}
