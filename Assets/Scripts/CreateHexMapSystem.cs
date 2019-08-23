using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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

        hexMesh = GetEntityQuery(typeof(HexMeshTag), typeof(RenderMesh));
        
        meshEntity = hexMesh.GetSingletonEntity();

        hexMeshTag = EntityManager.GetComponentData<HexMeshTag>(meshEntity);

    }

    /// <summary>
    /// 把所有六边形单元实体的数据传递出去
    /// </summary>
    [BurstCompile]
    private struct GetHexCellDataForRenderMeshJob : IJobForEachWithEntity<Translation, HexCellData> {
        public NativeArray<Vector3> Vertices;
        public NativeArray<Color> Colors;
        public void Execute(Entity entity, int index, [ReadOnly]ref Translation position,[ReadOnly]ref HexCellData hexCellData)
        {
            var center = position.Value;
            Colors[index] = hexCellData.color;
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

        var vertices = new NativeArray<Vector3>(HexMetrics.HexCelllCount, Allocator.TempJob);
        var colors = new NativeArray<Color>(HexMetrics.HexCelllCount, Allocator.TempJob);
        var getDataJob = new GetHexCellDataForRenderMeshJob
        {
            Vertices = vertices,
            Colors=colors

        }.Schedule(hexCells, inputDeps);
        getDataJob.Complete();
        
        var Vertices = new NativeList<Vector3>(HexMetrics.HexCelllCount*18, Allocator.TempJob);
        var Triangles = new NativeList<int>(HexMetrics.HexCelllCount * 18, Allocator.TempJob);
        var Colors= new NativeList<Color>(HexMetrics.HexCelllCount*18, Allocator.TempJob);

        for (int i = 0; i < vertices.Length; i++)
        {//Todo:this is too slow,should do it in a Job with Burst
            Vector3 center = vertices[i];
            Color color = colors[i];

            for (int j = 0; j < 6; j++)
            {
                int verticesIndex = Vertices.Length;
                Vertices.Add(center);
                Vertices.Add(center + HexMetrics.corners[j]);
                Vertices.Add(center + HexMetrics.corners[j + 1]);
                Triangles.Add(verticesIndex);
                Triangles.Add(verticesIndex + 1);
                Triangles.Add(verticesIndex + 2);
                Colors.Add(color);
                Colors.Add(color);
                Colors.Add(color);
            }
        }

        var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(meshEntity);

        renderMesh.mesh.vertices = Vertices.ToArray();
        renderMesh.mesh.triangles = Triangles.ToArray();
        renderMesh.mesh.colors = Colors.ToArray();
        renderMesh.mesh.RecalculateNormals();
        //目前ECS还没有物理引擎支持，所以MeshCollider无效！Todo：添加物理特性
        //var meshColider = EntityManager.GetSharedComponentData<MeshColliderData>(meshEntity);
        //meshColider.HexMeshCollider.sharedMesh = renderMesh.mesh;
        vertices.Dispose();
        colors.Dispose();
        Vertices.Dispose();
        Triangles.Dispose();
        Colors.Dispose();
        hexMeshTag.bIfNewMap = false;
        bIfNewMap = false;

        return getDataJob;

    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();
        //verticesAsNativeArray.Dispose();
        //trianglesAsNativeArray.Dispose();
    }
}
