using System.Collections.Generic;
using Sample1;
using UnityEngine;

namespace Sample
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class MeshGenerator : MonoBehaviour
    {
        [SerializeField] private int gridSize = 16;
        [SerializeField] private float cellSize = 1f; // 頂点間の距離
        [SerializeField] private float surfaceLevel = 0.1f;

        private float[,,] voxelGrid;
        private MeshFilter meshFilter;
        private Mesh mesh;
        private MeshCollider meshCollider;

        private readonly Vector3[] cubeCorners =
        {
            new(0, 0, 0), new(0, 0, 1), new(1, 0, 1), new(1, 0, 0),
            new(0, 1, 0), new(0, 1, 1), new(1, 1, 1), new(1, 1, 0)
        };
        private readonly Vector3[] normalizedVertexOffset =
        {
            new(0, 0, 0.5f), new(0.5f, 0, 1), new(1, 0, 0.5f), new(0.5f, 0, 0),
            new(0, 1, 0.5f), new(0.5f, 1, 1), new(1, 1, 0.5f), new(0.5f, 1, 0),
            new(0, 0.5f, 0), new(0, 0.5f, 1), new(1, 0.5f, 1), new(1, 0.5f, 0)
        };

        public float CellSize => cellSize;

        private void Start()
        {
            meshFilter = gameObject.GetComponent<MeshFilter>();
            mesh = new Mesh();
            meshFilter.mesh = mesh;
            meshCollider = gameObject.GetComponent<MeshCollider>();

            GenerateVoxelGrid();
            CreateMesh();
        }

        public void DigVoxel(Vector3Int voxelIndex, float digStrength)
        {
            if (voxelIndex.x < 0 || voxelIndex.x > gridSize ||
                voxelIndex.y < 0 || voxelIndex.y > gridSize ||
                voxelIndex.z < 0 || voxelIndex.z > gridSize)
            {
                Debug.LogWarning("Voxel index out of bounds");
                return;
            }

            var modifiedValue = voxelGrid[voxelIndex.x, voxelIndex.y, voxelIndex.z] - digStrength;
            voxelGrid[voxelIndex.x, voxelIndex.y, voxelIndex.z] = Mathf.Max(0f, modifiedValue);
        }

        public void UpdateMesh()
        {
            CreateMesh();
        }

        private void GenerateVoxelGrid()
        {
            voxelGrid = new float[gridSize + 1, gridSize + 1, gridSize + 1];

            for (var x = 0; x <= gridSize; x++)
            {
                for (var y = 0; y <= gridSize; y++)
                {
                    for (var z = 0; z <= gridSize; z++)
                    {
                        voxelGrid[x, y, z] = y == gridSize ? 0f : 1f;

                        // パーリンノイズで三次元の地面を生成
                        // var noiseValue = Mathf.PerlinNoise(x * 0.1f, z * 0.1f) - (y * 0.1f);
                        // voxelGrid[x, y, z] = noiseValue;
                    }
                }
            }
        }

        private void CreateMesh()
        {
            var indexByVertexTable = new Dictionary<Vector3, int>();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            for (var x = 0; x < gridSize; x++)
            {
                for (var y = 0; y < gridSize; y++)
                {
                    for (var z = 0; z < gridSize; z++)
                    {
                        var (cubeVertices, cubeTriangles) = BuildCubeMeshInformation(new Vector3Int(x, y, z), indexByVertexTable);

                        vertices.AddRange(cubeVertices);
                        triangles.AddRange(cubeTriangles);
                    }
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();

            meshCollider.sharedMesh = mesh;
        }

        private (List<Vector3> vertices, List<int> triangles) BuildCubeMeshInformation(Vector3Int normalizedVertexOriginPosition, Dictionary<Vector3, int> indexByVertexTable)
        {
            var cubeIndex = GetCubeIndex(normalizedVertexOriginPosition);

            var edgeIndexList = ConvertToEdgeIndexList(cubeIndex);
            var edgeIndexByCubeVertexTable = GetEdgeIndexByCubeVertexTable(edgeIndexList, normalizedVertexOriginPosition);

            var meshVertexIndexByEdgeIndexTable = new Dictionary<int, int>();
            var cubeVertices = new List<Vector3>();
            foreach (var cubeVertexWithEdgeIndex in edgeIndexByCubeVertexTable)
            {
                // すでに登録されている頂点があればそのインデックスを使う
                if (indexByVertexTable.TryGetValue(cubeVertexWithEdgeIndex.Key, out var listIndex))
                {
                    meshVertexIndexByEdgeIndexTable[cubeVertexWithEdgeIndex.Value] = listIndex;
                    continue;
                }

                // まだ登録されていない頂点を登録
                var newIndex = indexByVertexTable.Count;
                indexByVertexTable.Add(cubeVertexWithEdgeIndex.Key, newIndex);
                meshVertexIndexByEdgeIndexTable[cubeVertexWithEdgeIndex.Value] = newIndex;
                cubeVertices.Add(cubeVertexWithEdgeIndex.Key);
            }

            var cubeTriangles = GetCubeTriangles(cubeIndex, meshVertexIndexByEdgeIndexTable);

            return (cubeVertices, cubeTriangles);
        }

        private int GetCubeIndex(Vector3Int normalizedVertexOriginPosition)
        {
            var cubeIndex = 0;

            for (var i = 0; i < 8; i++)
            {
                var corner = normalizedVertexOriginPosition + cubeCorners[i];
                var cubeValue = voxelGrid[(int)corner.x, (int)corner.y, (int)corner.z];
                if (cubeValue < surfaceLevel)
                {
                    cubeIndex |= 1 << i;
                }
            }

            return cubeIndex;
        }

        private List<int> ConvertToEdgeIndexList(int cubeIndex)
        {
            var edgeIndexes = MarchingCubesTable.EdgeTable[cubeIndex];

            // edgeIndexesをビットとして扱い、立っているビットを一番右が辺の0として配列にする
            // 1001_0000_0101 -> 0, 2, 8, 11
            var edgeIndexList = new List<int>();
            const int edgeCount = 12;
            for (var edgeIndex = 0; edgeIndex < edgeCount; edgeIndex++)
            {
                if ((edgeIndexes & (1 << edgeIndex)) != 0)
                {
                    edgeIndexList.Add(edgeIndex);
                }
            }

            return edgeIndexList;
        }

        private Dictionary<Vector3, int> GetEdgeIndexByCubeVertexTable(IReadOnlyList<int> edgeIndexList, Vector3Int normalizedVertexOriginPosition)
        {
            var cubeVerticesWithIndex = new Dictionary<Vector3, int>(edgeIndexList.Count);
            foreach (var edgeIndex in edgeIndexList)
            {
                var normalizedVertexPosition = normalizedVertexOriginPosition + normalizedVertexOffset[edgeIndex];
                var vertexPosition = cellSize * normalizedVertexPosition;

                cubeVerticesWithIndex.Add(vertexPosition, edgeIndex);
            }

            return cubeVerticesWithIndex;
        }

        private List<int> GetCubeTriangles(int cubeIndex, Dictionary<int, int> meshVertexIndexByEdgeIndexTable)
        {
            var cubeTriangles = new List<int>();
            for (var i = 0; MarchingCubesTable.TriangleTable[cubeIndex, i] != -1; i++)
            {
                var edgeIndex = MarchingCubesTable.TriangleTable[cubeIndex, i];
                var listIndex = meshVertexIndexByEdgeIndexTable[edgeIndex];
                cubeTriangles.Add(listIndex);
            }

            return cubeTriangles;
        }

        private void OnDestroy()
        {
            Destroy(mesh);
            mesh = null;
        }
    }
}