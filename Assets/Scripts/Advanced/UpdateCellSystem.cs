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
    private EntityQuery m_CellGroup;
    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        var query = new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadWrite<Cell>(), ComponentType.ReadOnly<UpdateData>(), ComponentType.ReadWrite<Neighbors>(), ComponentType.ReadOnly<NeighborsIndex>() },
            None= new ComponentType[] { ComponentType.ReadOnly<NewDataTag>() }
        };
        m_CellGroup = GetEntityQuery(query);
    }

    /// <summary>
    /// 循环创建六边形单元，使其生成对应长宽的阵列
    /// </summary>
    struct CalculateJob : IJobForEachWithEntity<Cell, UpdateData,Neighbors,NeighborsIndex> {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [BurstCompile]
        public void Execute(Entity entity, int index, ref Cell cellData, [ReadOnly]ref UpdateData updata,ref Neighbors neighbors, [ReadOnly]ref NeighborsIndex neighborsIndex)
        {
            //0.获取更新列表
            NativeList<int> updateList = new NativeList<int>(7, Allocator.Temp);
            updateList.Add(updata.CellIndex);
            if (updata.NEIndex > int.MinValue) updateList.Add(updata.NEIndex);
            if (updata.EIndex > int.MinValue) updateList.Add(updata.EIndex);
            if (updata.SEIndex > int.MinValue) updateList.Add(updata.SEIndex);
            if (updata.SWIndex > int.MinValue) updateList.Add(updata.SWIndex);
            if (updata.WIndex > int.MinValue) updateList.Add(updata.WIndex);
            if (updata.NWIndex > int.MinValue) updateList.Add(updata.NWIndex);
            //1.判断并更新自身单元颜色以及相邻单元颜色
            Color color = updata.NewColor;

            //更新相邻单元的颜色
            if (updateList.Contains(neighborsIndex.NEIndex))
            {
                neighbors.NE = color;
                neighbors.NEElevation = updata.Elevation;
            }

            if (updateList.Contains(neighborsIndex.EIndex))
            {
                neighbors.E = color;
                neighbors.EElevation = updata.Elevation;
            }
            if (updateList.Contains(neighborsIndex.SEIndex)) {
                neighbors.SE = color;
                neighbors.SEElevation = updata.Elevation;
            }

            if (updateList.Contains(neighborsIndex.SWIndex))
            {
                neighbors.SW = color;
                neighbors.SWElevation = updata.Elevation;
            }

            if (updateList.Contains(neighborsIndex.WIndex) )
            {
                neighbors.W = color;
                neighbors.WElevation = updata.Elevation;
            }

            if (updateList.Contains(neighborsIndex.NWIndex) )
            {
                neighbors.NW = color;
                neighbors.NWElevation = updata.Elevation;
            }
            if (updateList.Contains(cellData.Index))//更新自身单元的颜色
            {
                cellData.Color = color;
                cellData.Position.y= updata.Elevation * HexMetrics.elevationStep;
                cellData.Elevation = updata.Elevation;
            }

            updateList.Dispose();
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

        }.Schedule(m_CellGroup, inputDeps);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;

    }
}
