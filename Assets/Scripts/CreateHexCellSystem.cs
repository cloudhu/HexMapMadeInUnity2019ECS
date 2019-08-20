using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// 创建六边形单元系统
/// </summary>
public class CreateHexCellSystem : JobComponentSystem {

    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    bool bIfNewMap = true;
    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    /// <summary>
    /// 循环创建六边形单元，使其生成对应长宽的阵列
    /// </summary>
    struct SpawnJob : IJobForEachWithEntity<CreaterData,SwitchCreateCellData> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [BurstCompile]
        public void Execute(Entity entity, int index, [ReadOnly]ref CreaterData  createrData,ref SwitchCreateCellData switchCreateCell)
        {

            if (switchCreateCell.bIfNewMap)
            {
                for (int z = 0; z < createrData.Height; z++)
                {
                    for (int x = 0; x < createrData.Width; x++)
                    {
                        //1.实例化
                        var instance = CommandBuffer.Instantiate(index, createrData.Prefab);
                        //2.计算阵列坐标
                        float _x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
                        float _z = z * (HexMetrics.outerRadius * 1.5f);
                        //3.设置父组件
                        //CommandBuffer.SetComponent(index, instance, new Parent
                        //{
                        //    Value = entity

                        //});
                        //4.设置每个单元的数据
                        CommandBuffer.SetComponent(index, instance, new HexCellData
                        {
                            X = x - z / 2,
                            Y = 0,
                            Z = z,
                            color = createrData.Color,

                        });
                        //5.设置位置
                        CommandBuffer.SetComponent(index, instance, new Translation
                        {
                            Value = new float3(_x, 0F, _z)

                        });
                    }
                }
                CommandBuffer.SetComponent(index, entity, new SwitchCreateCellData
                {
                    bIfNewMap=false

                });

                CommandBuffer.SetComponent(index, entity, new SwitchRotateData
                {
                    bIfStartRotateSystem=true

                });

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


        var job = new SpawnJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),

        }.Schedule(this, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
        job.Complete();
        if (bIfNewMap)
        {
            World.CreateSystem<CreateHexMapSystem>(); //not working
            //var hexMesh = GetEntityQuery(typeof(HexMeshTag), typeof(RenderMesh));
            //var meshEntity = hexMesh.GetSingletonEntity();
            //var hexMeshTag = EntityManager.GetComponentData<HexMeshTag>(meshEntity);
            //hexMeshTag.bIfNewMap = true;
            bIfNewMap = false;
        }
        return job;

    }
}
