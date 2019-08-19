using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class CreateHexCellSystem : JobComponentSystem {
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    private bool bIfNewMap = true;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    struct SpawnJob : IJobForEachWithEntity<MapData> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [BurstCompile]
        public void Execute(Entity entity, int index,ref MapData  mapData)
        {
            if (mapData.bIsNewMap)
            {
                for (int z = 0, i = 0; z < mapData.Height; z++)
                {
                    for (int x = 0; x < mapData.Width; x++)
                    {
                        var instance = CommandBuffer.Instantiate(index, mapData.Prefab);
                        float _x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
                        float _z = z * (HexMetrics.outerRadius * 1.5f);
                        CommandBuffer.SetComponent(index, instance, new Parent
                        {
                            Value = entity

                        });
                        CommandBuffer.SetComponent(index, instance, new HexCellData
                        {
                            X = x - z / 2,
                            Y = 0,
                            Z = z,
                            color = mapData.Color,

                        });
                        CommandBuffer.SetComponent(index, instance, new Translation
                        {
                            Value = new float3(_x, 0F, _z)

                        });
                    }
                }

                CommandBuffer.SetComponent(index, entity, new MapData
                {
                    bIsNewMap = false

                });
            }
            else
                return;

            //CommandBuffer.DestroyEntity(index, entity);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        if (bIfNewMap)
        {
            var job = new SpawnJob
            {
                CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),

            }.Schedule(this, inputDeps);

            m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
            job.Complete();
            var mapSystem = World.GetOrCreateSystem<CreateHexMapSystem>();
            mapSystem.bIfNewMap = true;
            bIfNewMap = false;
            return job;
        }

        return inputDeps;
    }
}
