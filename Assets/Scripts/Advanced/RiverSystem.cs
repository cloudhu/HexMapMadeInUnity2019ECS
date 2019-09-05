using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// 更新六边形单元系统
/// </summary>
//[DisableAutoCreation]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public class RiverSystem : JobComponentSystem {

    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    private EntityQuery m_CellGroup;
    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        var query = new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<Cell>(), ComponentType.ReadOnly<RiverRenderTag>(), ComponentType.ReadOnly<Neighbors>(), ComponentType.ReadOnly<River>() },
            None = new ComponentType[] { ComponentType.ReadOnly<NewDataTag>() }
        };
        m_CellGroup = GetEntityQuery(query);
    }

    /// <summary>
    /// 循环创建六边形单元，使其生成对应长宽的阵列
    /// </summary>
    struct RiverCalculateJob : IJobForEachWithEntity<Cell, Neighbors,River,RiverRenderTag> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        public NativeArray<Vector3> Vertices;
        //public NativeArray<Vector2> Uvs;
        [BurstCompile]
        public void Execute(Entity entity, int index,[ReadOnly] ref Cell cellData, [ReadOnly]ref Neighbors neighbors,[ReadOnly]ref River river,[ReadOnly]ref RiverRenderTag renderTag)
        {
            CommandBuffer.RemoveComponent<RiverRenderTag>(index, entity);
            //Vertices[index] = cellData.Position;
            //2.remove RiverRenderTag after Update

        }

    }

    /// <summary>
    /// 如果有新地图,则启动任务
    /// </summary>
    /// <param name="inputDeps">依赖</param>
    /// <returns>任务句柄</returns>
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        NativeArray<Vector3> vertices = new NativeArray<Vector3>(600,Allocator.TempJob);
        var job = new RiverCalculateJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            Vertices=vertices

        }.Schedule(m_CellGroup, inputDeps);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
        job.Complete();
        if (job.IsCompleted)
        {
            Debug.Log("RiverCalculateJob IsCompleted :"+ vertices.Length);
            //for (int i = 0; i < vertices.Length; i++)
            //{
            //    Debug.Log(vertices[i]);
            //}

            vertices.Dispose();
        }
        return job;

    }
}
