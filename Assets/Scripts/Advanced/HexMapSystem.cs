using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 六边形地图系统
/// </summary>
[DisableAutoCreation]
public class HexMapSystem : JobComponentSystem
{
    private bool bIfRendered = false;
    /// <summary>
    /// 顶点
    /// </summary>
    //NativeArray<Vector3> Vertices;
    /// <summary>
    /// 三角
    /// </summary>
    //NativeArray<int> Triangles;
    /// <summary>
    /// 颜色
    /// </summary>
    //NativeArray<Color> Colors;
    /// <summary>
    /// 实体命令缓存系统--阻塞
    /// </summary>
    //EntityCommandBufferSystem m_Barrier;

    protected override void OnCreate()
    {
        //int totalCount = HexMetrics.HexCelllCount * HexMetrics.CellVerticesCount;
        //Vertices = new NativeArray<Vector3>(totalCount, Allocator.Persistent);
        //Triangles = new NativeArray<int>(totalCount, Allocator.Persistent);
        //Colors = new NativeArray<Color>(totalCount, Allocator.Persistent);
        //m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    private struct CalculateDataForRenderMeshJob : IJobForEachWithEntity<Vertex> {

        public NativeArray<Vector3> Vertices;

        public NativeArray<Color> Colors;

        public NativeArray<int> Triangles;
        //[WriteOnly]//只写
        //public EntityCommandBuffer.Concurrent CommandBuffer;

        public void Execute(Entity entity, int index, ref Vertex vertexData)
        {
            if (vertexData.Switcher)
            {
                Vertices[index] = vertexData.Vector;
                Triangles[index] = vertexData.Triangle;
                Colors[index] = vertexData.Color;

                vertexData.Switcher = false;
                //考虑摧毁顶点的实体来优化性能，以后的顶点数量可能会非常多，也可以不生成顶点实体！
                //CommandBuffer.DestroyEntity(index, entity);
            }

        }
    }

    /// <summary>
    /// 如果有新地图,则启动任务
    /// </summary>
    /// <param name="inputDeps">依赖</param>
    /// <returns>任务句柄</returns>
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (!bIfRendered)
        {
            int totalCount = HexMetrics.HexCelllCount * HexMetrics.CellVerticesCount;
            var Vertices = new NativeArray<Vector3>(totalCount, Allocator.TempJob);
            var Triangles = new NativeArray<int>(totalCount, Allocator.TempJob);
            var Colors = new NativeArray<Color>(totalCount, Allocator.TempJob);

            var CalculateJob = new CalculateDataForRenderMeshJob
            {
                Vertices = Vertices,
                Triangles = Triangles,
                Colors = Colors,
                //CommandBuffer= m_Barrier.CreateCommandBuffer().ToConcurrent()

            }.Schedule(this, inputDeps);
            //m_Barrier.AddJobHandleForProducer(CalculateJob);

            CalculateJob.Complete();
            Debug.Log(Vertices.Length / 36);
            if (CalculateJob.IsCompleted)
            {
                if (!bIfRendered)
                {

                    var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(MainWorld.Instance.GetMeshEntity());

                    renderMesh.mesh.vertices = Vertices.ToArray();
                    renderMesh.mesh.triangles = Triangles.ToArray();
                    renderMesh.mesh.colors = Colors.ToArray();
                    renderMesh.mesh.RecalculateNormals();

                    bIfRendered = true;
                }

            }
            Vertices.Dispose();
            Triangles.Dispose();
            Colors.Dispose();
            return CalculateJob;
        }

        return inputDeps;

    }
    protected override void OnStopRunning()
    {
        base.OnStopRunning();

    }
}
