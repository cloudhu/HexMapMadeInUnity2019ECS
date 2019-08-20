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

    //private NativeArray<Vector3> verticesAsNativeArray;
    //private NativeArray<int> trianglesAsNativeArray;
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
        //verticesAsNativeArray = new NativeArray<Vector3>(HexMetrics.totalVertices, Allocator.TempJob);
        //trianglesAsNativeArray = new NativeArray<int>(HexMetrics.totalVertices, Allocator.TempJob);
    }

    [BurstCompile]
    private struct CopySimPointsToVerticesJob : IJobForEachWithEntity<Translation> {
        public NativeArray<Vector3> Vertices;
        public NativeArray<int> Triangles;
        public void Execute(Entity entity, int index, [ReadOnly]ref Translation position)
        {
            var currentPosition = position.Value;

            Vector3 center = new Vector3
            {
                x = currentPosition.x,
                y = currentPosition.y,
                z = currentPosition.z
            };
            Vertices[index] = center;
            for (int i = 0; i < 6; i++)
            {
                //Triangles[Vertices.Length] = Vertices.Length;
                //Vertices[Vertices.Length] = center;
                //Triangles[Vertices.Length] = Vertices.Length;
                //Vertices[Vertices.Length] =center + HexMetrics.corners[i];
                //Triangles[Vertices.Length] = Vertices.Length;
                //Vertices[Vertices.Length] =center + HexMetrics.corners[i + 1];
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Debug.Log(hexMeshTag.bIfNewMap);
        if (hexMeshTag.bIfNewMap)
        {
            var verticesAsNativeArray = new NativeArray<Vector3>(HexMetrics.totalVertices, Allocator.TempJob);
            var trianglesAsNativeArray = new NativeArray<int>(HexMetrics.totalVertices, Allocator.TempJob);
            var copyToSimPointsJob = new CopySimPointsToVerticesJob
            {
                Vertices = verticesAsNativeArray,
                Triangles = trianglesAsNativeArray,
            }.Schedule(hexCells, inputDeps);
            copyToSimPointsJob.Complete();

            Debug.Log(hexMeshTag.bIfNewMap);
            //Debug.Log(meshEntity);//Entity(1:1)
            var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(meshEntity);
            //Debug.Log(renderMesh);//Unity.Rendering.RenderMesh
            var newVertexArray = new Vector3[verticesAsNativeArray.Length];
            verticesAsNativeArray.CopyTo(newVertexArray);
            //Debug.Log(newVertexArray.Length);
            for (int i = 0; i < 36; i++)
            {
                Debug.Log(i);
                Debug.Log(verticesAsNativeArray[i]);
            }
            renderMesh.mesh.vertices = newVertexArray;
            var newTris= new int[trianglesAsNativeArray.Length];
            trianglesAsNativeArray.CopyTo(newTris);
            //var newMesh = new Mesh();
            //newMesh.name = "New Mesh";
            //newMesh.vertices = newVertexArray;
            //newMesh.normals = renderMesh.mesh.normals;
            //newMesh.tangents = renderMesh.mesh.tangents;
            //newMesh.triangles = newTris;
            //newMesh.RecalculateNormals();
            //newMesh.MarkDynamic();
            //renderMesh.mesh = newMesh;
            //Debug.Log(renderMesh.mesh.vertexCount);
            renderMesh.mesh.triangles = newTris;
            renderMesh.mesh.RecalculateNormals();
            //var meshColider = EntityManager.GetSharedComponentData<MeshColliderData>(meshEntity);
            //meshColider.HexMeshCollider.sharedMesh = renderMesh.mesh;
            verticesAsNativeArray.Dispose();
            trianglesAsNativeArray.Dispose();
            hexMeshTag.bIfNewMap = false;
            Debug.Log(hexMeshTag.bIfNewMap);
            return copyToSimPointsJob;
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
