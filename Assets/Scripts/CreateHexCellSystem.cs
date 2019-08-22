using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 创建六边形单元系统
/// </summary>
public class CreateHexCellSystem : JobComponentSystem {

    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    /// <summary>
    /// 新地图开关
    /// </summary>
    bool bIfNewMap = true;
    private CreateHexMapSystem createHexMapSystem;
    /// <summary>
    /// 实体原型
    /// </summary>
    //private EntityArchetype m_simPointArchetype;
    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        //m_simPointArchetype = EntityManager.CreateArchetype(
        //    typeof(VertexData),
        //    typeof(Translation));
    }

    /// <summary>
    /// 循环创建六边形单元，使其生成对应长宽的阵列
    /// </summary>
    struct SpawnJob : IJobForEachWithEntity<CreaterData,SwitchCreateCellData> {
        public EntityCommandBuffer.Concurrent CommandBuffer;

        //public NativeArray<float3> Vertices;
        [BurstCompile]
        public void Execute(Entity entity, int index, [ReadOnly]ref CreaterData  createrData,ref SwitchCreateCellData switchCreateCell)
        {

            if (switchCreateCell.bIfNewMap)
            {
                //int cellIndex = 0;
                for (int z = 0; z < createrData.Height; z++)
                {
                    for (int x = 0; x < createrData.Width; x++)
                    {
                        //1.实例化
                        var instance = CommandBuffer.Instantiate(index, createrData.Prefab);
                        //cellIndex++;
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
                        float3 center = new float3(_x, 0F, _z);
                        //5.设置位置
                        CommandBuffer.SetComponent(index, instance, new Translation
                        {
                            Value = center

                        });
                        //6.保存中心顶点
                        //Vector3 newCenter = new Vector3
                        //{
                        //    x = _x,
                        //    y = 0F,
                        //    z = _z
                        //};
                        //Vertices[cellIndex] = center;

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
                //保存生成的Mesh数据

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

        if (bIfNewMap)
        {
            //var vertices = new NativeArray<float3>(HexMetrics.HexCelllCount, Allocator.TempJob);
            var job = new SpawnJob
            {
                CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
                //Vertices=vertices

            }.Schedule(this, inputDeps);

            m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
            job.Complete();
            //for (int i = 0; i < vertices.Length; i++)
            //{
            //    Debug.Log(vertices[i]);

            //}
            createHexMapSystem =World.CreateSystem<CreateHexMapSystem>(); //not working Fixed:CreateSystem需要构造函数才能生效
            createHexMapSystem.bIfNewMap = true;
            //var hexMesh = GetEntityQuery(typeof(HexMeshTag), typeof(RenderMesh));
            //var meshEntity = hexMesh.GetSingletonEntity();
            //var hexMeshTag = EntityManager.GetComponentData<HexMeshTag>(meshEntity);
            //hexMeshTag.bIfNewMap = true;

            bIfNewMap = false;
            //vertices.Dispose();
            return job;
        }
        else
            createHexMapSystem.Update();

        return inputDeps;

    }
}
