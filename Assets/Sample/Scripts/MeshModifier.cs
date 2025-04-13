using UnityEngine;

namespace Sample
{
    [RequireComponent(typeof(MeshGenerator))]
    public class MeshModifier : MonoBehaviour
    {
        [SerializeField] private float digStrength = 0.8f;

        private MeshGenerator meshGenerator;
        private Camera mainCamera;

        private void Start()
        {
            meshGenerator = GetComponent<MeshGenerator>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitInfo))
            {
                var hitPoint = hitInfo.point;
                ModifyNearestVoxelVertex(hitPoint);
            }
        }

        private void ModifyNearestVoxelVertex(Vector3 hitPoint)
        {
            // 衝突点をボクセル座標系に変換し、最も近い格子点のインデックスを計算
            var normalizedPoint = (hitPoint - transform.position) / meshGenerator.CellSize;
            var nearestVertexIndex = new Vector3Int(
                Mathf.FloorToInt(normalizedPoint.x),
                Mathf.FloorToInt(normalizedPoint.y),
                Mathf.FloorToInt(normalizedPoint.z)
            );

            // クリックした付近の8頂点を掘る
            meshGenerator.DigVoxel(nearestVertexIndex, digStrength);
            meshGenerator.DigVoxel(nearestVertexIndex + Vector3Int.right, digStrength);
            meshGenerator.DigVoxel(nearestVertexIndex + Vector3Int.up, digStrength);
            meshGenerator.DigVoxel(nearestVertexIndex + Vector3Int.forward, digStrength);
            meshGenerator.DigVoxel(nearestVertexIndex + Vector3Int.right + Vector3Int.up, digStrength);
            meshGenerator.DigVoxel(nearestVertexIndex + Vector3Int.right + Vector3Int.forward, digStrength);
            meshGenerator.DigVoxel(nearestVertexIndex + Vector3Int.up + Vector3Int.forward, digStrength);
            meshGenerator.DigVoxel(nearestVertexIndex + Vector3Int.right + Vector3Int.up + Vector3Int.forward, digStrength);
            
            meshGenerator.UpdateMesh();
        }
    }
}