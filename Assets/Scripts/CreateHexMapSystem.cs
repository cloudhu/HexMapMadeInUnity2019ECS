using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
public class CreateHexMapSystem : JobComponentSystem
{
    private EntityQuery hexCells;
    private EntityQuery hexMesh;


    private Entity meshEntity;
    private HexMeshTag hexMeshTag;
    public bool bIfNewMap=false;
    //private NativeArray<Entity> vertexEntities;

    /// <summary>
    /// 构造函数,被World.CreateSystem调用
    /// </summary>
    public CreateHexMapSystem()
    {

    }

    protected override void OnCreate()
    {
        hexCells = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<HexCellData>(), ComponentType.ReadOnly<Translation>()

            },
        });
        /// <summary>
        /// 顶点实体原型
        /// </summary>
        //EntityArchetype vertexEntityArchetype;
        //vertexEntityArchetype = EntityManager.CreateArchetype(typeof(VertexData));
        //vertexEntities = new NativeArray<Entity>(HexMetrics.HexCelllCount*18, Allocator.TempJob);
        //EntityManager.CreateEntity(vertexEntityArchetype, vertexEntities);
        hexMesh = GetEntityQuery(typeof(HexMeshTag), typeof(RenderMesh));
        
        meshEntity = hexMesh.GetSingletonEntity();

        hexMeshTag = EntityManager.GetComponentData<HexMeshTag>(meshEntity);

    }

    /// <summary>
    /// 把所有六边形单元中心点作为所有顶点的起始点
    /// </summary>
    [BurstCompile]
    private struct CopyHexCellCenterPositionsToVerticesJob : IJobForEachWithEntity<Translation> {
        public NativeArray<Vector3> Vertices;
        public void Execute(Entity entity, int index, [ReadOnly]ref Translation position)
        {
            var center = position.Value;

            Vertices[index] = new Vector3
            {
                x = center.x,
                y = center.y,
                z = center.z
            };
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (bIfNewMap)
        {
            var vertices = new NativeArray<Vector3>(HexMetrics.HexCelllCount, Allocator.TempJob);
            var copyToVerticesJob = new CopyHexCellCenterPositionsToVerticesJob
            {
                Vertices = vertices

            }.Schedule(hexCells, inputDeps);
            copyToVerticesJob.Complete();
            //var buffer= EntityManager.AddBuffer<HexMeshData>(meshEntity);
            //Todo:this is too slow,should do it in a Job with Burst
            var NewVertices = new NativeList<Vector3>(HexMetrics.HexCelllCount*18, Allocator.TempJob);
            var Triangles = new NativeList<int>(HexMetrics.HexCelllCount * 18, Allocator.TempJob);

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 center = vertices[i];
                for (int j = 0; j < 6; j++)
                {
                    int verticesIndex = NewVertices.Length;
                    NewVertices.Add(center);
                    NewVertices.Add(center + HexMetrics.corners[j]);
                    NewVertices.Add(center + HexMetrics.corners[j + 1]);
                    Triangles.Add(verticesIndex);
                    Triangles.Add(verticesIndex + 1);
                    Triangles.Add(verticesIndex + 2);
                }
            }

            var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(meshEntity);

            //var newVertexArray = new Vector3[verticesAsNativeArray.Length];
            //verticesAsNativeArray.CopyTo(newVertexArray);

            renderMesh.mesh.vertices = NewVertices.ToArray();
            renderMesh.mesh.triangles = Triangles.ToArray();
            renderMesh.mesh.RecalculateNormals();
            //目前ECS还没有物理引擎支持，所以MeshCollider无效！Todo：添加物理特性
            //var meshColider = EntityManager.GetSharedComponentData<MeshColliderData>(meshEntity);
            //meshColider.HexMeshCollider.sharedMesh = renderMesh.mesh;
            vertices.Dispose();
            NewVertices.Dispose();
            Triangles.Dispose();
            hexMeshTag.bIfNewMap = false;
            bIfNewMap = false;

            return copyToVerticesJob;
        }

        return inputDeps;
    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();
        //verticesAsNativeArray.Dispose();
        //trianglesAsNativeArray.Dispose();
    }
}
