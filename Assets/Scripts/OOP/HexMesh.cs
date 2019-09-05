using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    public bool useCollider, useColors;
    Mesh m_Mesh;
    MeshCollider m_MeshCollider;

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = m_Mesh = new Mesh();
        if(useCollider)m_MeshCollider = gameObject.AddComponent<MeshCollider>();
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
        totalCount = useColors ? totalCount : 0;
        NativeList<Color> Colors = new NativeList<Color>(totalCount, Allocator.Temp);
        for (int i = 0; i < totalCount; i++)
        {
            //0.取出实体，如果实体的索引为m_Builder则跳过
            Entity entity = cells[i];
            DynamicBuffer<VertexBuffer> vertexBuffer = m_EntityManager.GetBuffer<VertexBuffer>(entity);
            //Debug.Log(vertexBuffer.Length);
            //float elevationPerturb = 0f;
            if (vertexBuffer.Length > 0)
            {
                DynamicBuffer<ColorBuffer> colorBuffer = m_EntityManager.GetBuffer<ColorBuffer>(entity);

                for (int j = 0; j < vertexBuffer.Length; j++)
                {
                    Triangles.Add(Vertices.Length);
                    if (useColors) Colors.Add(colorBuffer[j]);
                    //Vector3 vertex = HexMetrics.Perturb(vertexBuffer[j]);
                    //if (j == 0)
                    //{
                    //    elevationPerturb= (HexMetrics.SampleNoise(vertex).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
                    //}
                    //vertex.y += elevationPerturb;
                    //Vertices.Add(vertex);
                    Vertices.Add(vertexBuffer[j]);
                }

                vertexBuffer.Clear();
                colorBuffer.Clear();
            }
        }

        Debug.Log("-----------------------------------------------------------------------------------------");
        Debug.Log("Vertices=" + Vertices.Length + "----Triangles=" + Triangles.Length + "----Colors=" + Colors.Length);

        if (Vertices.Length > 1)
        {
            m_Mesh.Clear();
            m_Mesh.vertices = Vertices.ToArray();
            m_Mesh.triangles = Triangles.ToArray();
            if(useColors)m_Mesh.colors = Colors.ToArray();
            m_Mesh.RecalculateNormals();
            m_Mesh.Optimize();
            if(useCollider)m_MeshCollider.sharedMesh = m_Mesh;
        }
        Vertices.Dispose();
        Triangles.Dispose();
        Colors.Dispose();
    }

}
