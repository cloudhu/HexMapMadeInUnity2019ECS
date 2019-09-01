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
    struct CalculateJob : IJobForEachWithEntity<Cell, UpdateData,Neighbors,NeighborsIndex> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [BurstCompile]
        public void Execute(Entity entity, int index, ref Cell cellData, [ReadOnly]ref UpdateData updata,ref Neighbors neighbors,ref NeighborsIndex neighborsIndex)
        {
            //0.获取单元索引，Execute的index顺序混乱
            int cellIndex = cellData.Index;
            int updateIndex = updata.CellIndex;
            //1.判断并更新自身单元颜色以及相邻单元颜色

            Color color = updata.NewColor;

            //更新相邻单元的颜色
            if (neighborsIndex.NEIndex == updateIndex)
            {
                neighbors.NE = color;
                neighbors.NEElevation = updata.Elevation;
            }

            if (neighborsIndex.EIndex == updateIndex)
            {
                neighbors.E = color;
                neighbors.EElevation = updata.Elevation;
            }
            if (neighborsIndex.SEIndex == updateIndex) {
                neighbors.SE = color;
                neighbors.SEElevation = updata.Elevation;
            }

            if (neighborsIndex.SWIndex == updateIndex)
            {
                neighbors.SW = color;
                neighbors.SWElevation = updata.Elevation;
            }

            if (neighborsIndex.WIndex == updateIndex)
            {
                neighbors.W = color;
                neighbors.WElevation = updata.Elevation;
            }

            if (neighborsIndex.NWIndex == updateIndex)
            {
                neighbors.NW = color;
                neighbors.NWElevation = updata.Elevation;
            }
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
