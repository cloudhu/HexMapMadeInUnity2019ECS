using System.ComponentModel;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


/// <summary>
/// S:这里暂时只做旋转，后面会变色等
/// </summary>
public class HexCellSystem : JobComponentSystem {
    EntityQuery m_Group;//查询到特定组件的实体，将其放入这个组中

    /// <summary>
    /// 这里根据类型来查询到特定的实体
    /// </summary>
    protected override void OnCreate()
    {
        ///typeof(Rotation)=带有Rotation组件的；ComponentType=对应HexCellData组件类型的
        /// ReadOnly=只读会加快获取实体的速度，ReadWrite=读写 则相对较慢
        m_Group = GetEntityQuery(typeof(Rotation), ComponentType.ReadOnly<HexCellData>());
    }

    [BurstCompile]//同样使用Burst编译器来加速，区别是使用了块接口：IJobChunk
    struct RotationSpeedJob : IJobChunk {
        /// <summary>
        /// 时间
        /// </summary>
        public float DeltaTime;

        /// <summary>
        /// 原型块组件类型=Rotation
        /// </summary>
        public ArchetypeChunkComponentType<Rotation> RotationType;

        /// <summary>
        /// 只读 原型块组件类型=HexCellData
        /// </summary>
        public ArchetypeChunkComponentType<HexCellData> RotationSpeedType;

        /// <summary>
        /// 找出满足条件的实体来执行
        /// </summary>
        /// <param name="chunk"><原型块/param>
        /// <param name="chunkIndex">块索引</param>
        /// <param name="firstEntityIndex">第一个实体索引</param>
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var chunkRotations = chunk.GetNativeArray(RotationType);
            var chunkRotationSpeeds = chunk.GetNativeArray(RotationSpeedType);
            for (var i = 0; i < chunk.Count; i++)
            {
                var rotation = chunkRotations[i];
                var rotationSpeed = chunkRotationSpeeds[i];

                chunkRotations[i] = new Rotation
                {
                    Value = math.mul(math.normalize(rotation.Value),
                        quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * DeltaTime))
                };
            }
        }
    }

    /// <summary>
    /// 这个方法在主线程上每帧运行
    /// </summary>
    /// <param name="inputDependencies">输入依赖</param>
    /// <returns></returns>
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var rotationType = GetArchetypeChunkComponentType<Rotation>();
        var rotationSpeedType = GetArchetypeChunkComponentType<HexCellData>();

        var job = new RotationSpeedJob()
        {
            RotationType = rotationType,
            RotationSpeedType = rotationSpeedType,
            DeltaTime = Time.deltaTime
        };

        return job.Schedule(m_Group, inputDependencies);
    }
}


