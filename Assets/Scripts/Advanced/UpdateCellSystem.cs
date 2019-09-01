using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// 更新六边形单元系统
/// </summary>
//[DisableAutoCreation]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class UpdateCellSystem : JobComponentSystem {

    BeginSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    /// <summary>
    /// 循环创建六边形单元，使其生成对应长宽的阵列
    /// </summary>
    struct CalculateJob : IJobForEachWithEntity<Cell, UpdateData> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [BurstCompile]
        public void Execute(Entity entity, int index, ref Cell cellData, [ReadOnly]ref UpdateData updata)
        {
            //0.获取单元索引，Execute的index顺序混乱
            int cellIndex = cellData.Index;
            int updateIndex = updata.CellIndex;

            //1.判断并更新自身单元颜色以及相邻单元颜色

            Color color = updata.NewColor;

            //更新相邻单元的颜色
            if (cellData.NEIndex == updateIndex)
            {
                cellData.NE = color;
                cellData.NEElevation = updata.Elevation;
            }
            if (cellData.EIndex == updateIndex){ cellData.E = color; cellData.EElevation = updata.Elevation; }
            if (cellData.SEIndex == updateIndex) {cellData.SE = color; cellData.SEElevation = updata.Elevation; }
            if (cellData.SWIndex == updateIndex){ cellData.SW = color; cellData.SWElevation = updata.Elevation; }
            if (cellData.WIndex == updateIndex) {cellData.W = color; cellData.WElevation = updata.Elevation; }
            if (cellData.NWIndex == updateIndex){ cellData.NW = color; cellData.NWElevation = updata.Elevation; }
            if (cellIndex == updateIndex)//更新自身单元的颜色
            {
                cellData.Color = color;
                cellData.Position.y= updata.Elevation * HexMetrics.elevationStep;
                cellData.Elevation = updata.Elevation;
            }

            //2.remove UpdateData after Update,therefor NewDataTag need to be added to active CellSystem
            CommandBuffer.RemoveComponent<UpdateData>(index, entity);
            CommandBuffer.AddComponent<NewDataTag>(index, entity);

        }

    }

    /// <summary>
    /// 如果有新地图,则启动任务
    /// </summary>
    /// <param name="inputDeps">依赖</param>
    /// <returns>任务句柄</returns>
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new CalculateJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),

        }.Schedule(this, inputDeps);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;

    }
}
