using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Estuary : MonoBehaviour {
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
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> uvs2 = new List<Vector2>();
        for (int i = 0; i < totalCount; i++)
        {
            //0.取出实体，如果实体的索引为m_Builder则跳过
            Entity entity = cells[i];
            Cell cell = m_EntityManager.GetComponentData<Cell>(entity);
            if (cell.HasRiver)
            {
                DynamicBuffer<EstuaryBuffer> riverBuffers = m_EntityManager.GetBuffer<EstuaryBuffer>(entity);
                if (riverBuffers.Length > 0)
                {
                    DynamicBuffer<EstuaryUvBuffer> uvBuffers = m_EntityManager.GetBuffer<EstuaryUvBuffer>(entity);
                    DynamicBuffer<EstuaryUvsBuffer> uvsBuffers = m_EntityManager.GetBuffer<EstuaryUvsBuffer>(entity);
                    for (int j = 0; j < riverBuffers.Length; j++)
                    {
                        Triangles.Add(Vertices.Length);
                        Vertices.Add(riverBuffers[j]);
                        uvs.Add(uvBuffers[j]);
                        if (j<uvsBuffers.Length)
                        {
                            uvs2.Add(uvsBuffers[j]);
                        }
                    }
                    uvBuffers.Clear();
                    riverBuffers.Clear();
                    uvsBuffers.Clear();
                }
            }
        }

        Debug.Log("-----------------------------------------------------------------------------------------");
        Debug.Log("Vertices=" + Vertices.Length + "----Triangles=" + Triangles.Length + "----UV=" + uvs.Count);

        if (Vertices.Length > 1)
        {
            m_Mesh.Clear();
            m_Mesh.vertices = Vertices.ToArray();
            m_Mesh.triangles = Triangles.ToArray();
            m_Mesh.SetUVs(0, uvs);
            m_Mesh.SetUVs(1, uvs2);
            m_Mesh.RecalculateNormals();
            m_Mesh.Optimize();
        }
        Vertices.Dispose();
        Triangles.Dispose();
    }
}
