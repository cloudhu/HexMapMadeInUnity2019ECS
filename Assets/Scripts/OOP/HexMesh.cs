using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{

    Mesh m_Mesh;
    MeshCollider m_MeshCollider;

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = m_Mesh = new Mesh();
        m_MeshCollider = gameObject.AddComponent<MeshCollider>();
        m_Mesh.name = "Hex Mesh";
        m_Mesh.MarkDynamic();
    }

    public IEnumerator Triangulate(Entity[] cells)
    {
        yield return new WaitForSeconds(0.01f);
        int totalCount = cells.Length;
        EntityManager m_EntityManager = MainWorld.Instance.GetEntityManager();
        NativeList<Vector3> Vertices = new NativeList<Vector3>(totalCount, Allocator.Temp);
        NativeList<int> Triangles = new NativeList<int>(totalCount, Allocator.Temp);
        NativeList<Color> Colors = new NativeList<Color>(totalCount, Allocator.Temp);

        for (int i = 0; i < totalCount; i++)
        {
            //0.取出实体，如果实体的索引为m_Builder则跳过
            Entity entity = cells[i];
            //if (!m_EntityManager.HasComponent<Cell>(entity)) continue;
            DynamicBuffer<ColorBuffer> colorBuffer = m_EntityManager.GetBuffer<ColorBuffer>(entity);
            //Debug.Log(colorBuffer.Length);
            //float elevationPerturb = 0f;
            if (colorBuffer.Length > 0)
            {
                DynamicBuffer<VertexBuffer> vertexBuffer = m_EntityManager.GetBuffer<VertexBuffer>(entity);
                for (int j = 0; j < colorBuffer.Length; j++)
                {
                    Triangles.Add(Vertices.Length);
                    Colors.Add(colorBuffer[j]);
                    Vector3 vertex = Perturb(vertexBuffer[j]);
                    //if (j == 0)
                    //{
                    //    elevationPerturb= (HexMetrics.SampleNoise(vertex).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
                    //}
                    //vertex.y += elevationPerturb;
                    Vertices.Add(vertex);
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
            m_Mesh.colors = Colors.ToArray();
            m_Mesh.RecalculateNormals();
            m_Mesh.Optimize();
            m_MeshCollider.sharedMesh = m_Mesh;
        }
        Vertices.Dispose();
        Triangles.Dispose();
        Colors.Dispose();
    }

    /// <summary>
    /// 噪声干扰
    /// </summary>
    /// <param name="position">顶点位置</param>
    /// <returns>被干扰的位置</returns>
    Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
        return position;
    }
}
