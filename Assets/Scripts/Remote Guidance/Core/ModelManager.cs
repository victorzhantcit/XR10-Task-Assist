using Guidance.Dtos;
using Guidance.Utils;
using MRTK.Extensions;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Guidance.Core.XR
{
    public class ModelManager : MonoBehaviour
    {

        [SerializeField] private ARMeshManager _arMeshManager;
        [SerializeField] private DrawingHandler _drawerPrefab;
        [SerializeField] private Vector3 _markerShiftPosition = Vector3.zero;

        private HistoryRecorder<Shape3D> _shapeHistory;
        public bool CanRedoMarker => _shapeHistory.CanRedo;
        public bool CanUndoMarker => _shapeHistory.CanUndo;

        public float MarkerOffsetX
        {
            get => _markerShiftPosition.x;
            set => _markerShiftPosition.x = value;
        }

        public float MarkerOffsetY
        {
            get => _markerShiftPosition.y;
            set => _markerShiftPosition.y = value;
        }

        public float MarkerDefaultDepth
        {
            get => _markerShiftPosition.z;
            set => _markerShiftPosition.z = value;
        }

        public string MarkerOffsetInfo => $"OffsetX: {MarkerOffsetX}\nOffsetY: {MarkerOffsetY}\nDepth: {MarkerDefaultDepth}";

        private ObjectPool<DrawingHandler> _visualizerPool;
        private GameObject _arMeshGroup = null;

        public void Start()
        {
            _visualizerPool = new ObjectPool<DrawingHandler>(_drawerPrefab, this.transform);
            _shapeHistory = new HistoryRecorder<Shape3D>(UndoShapeHandler, RedoShapeHandler);
        }

        /// <summary>
        /// ArMeshManager 應該置於 MRTK XR Rig/Camera Offset目錄下，啟用時會自動抓取動態生成的網格群組物件 
        /// </summary>
        public void ActivateArMesh(bool isActive)
        {
            // 取得 AR Mesh Manager 產生網格的集合
            if (_arMeshGroup == null)
            {
                Transform cameraOffset = _arMeshManager.transform.parent;
                _arMeshGroup = cameraOffset.GetChild(cameraOffset.childCount - 1).gameObject;
            }

            // 啟用/關閉 AR Mesh 偵測與顯示
            _arMeshManager.enabled = isActive;
            _arMeshGroup.SetActive(isActive);
        }

        public void EraseAllMarker()
        {
            for (int i = this.transform.childCount - 1; i >= 0; i--)
            {
                DrawingHandler visualizer = this.transform.GetChild(i).GetComponent<DrawingHandler>();

                visualizer.ResetDrawer();
                _visualizerPool.Release(visualizer);
            }
            _shapeHistory.ClearHistory();
        }

        private void RedoShapeHandler(Shape3D shape)
        {
            ShapeReborn(shape);
            Debug.Log("RedoShapeHandler");
        }

        private void UndoShapeHandler(Shape3D shape)
        {
            for (int i = this.transform.childCount - 1; i >= 0; i--)
            {
                DrawingHandler visualizer = this.transform.GetChild(i).GetComponent<DrawingHandler>();

                if (visualizer.Snapshot == shape.Snapshot)
                {
                    visualizer.ResetDrawer();
                    _visualizerPool.Release(visualizer);
                    break;
                }
            }
            Debug.Log("UndoShapeHandler");
        }

        public void RedoShape()
        {
            Debug.Log("RedoShape");
            _shapeHistory.Redo();
        }

        public void UndoShape()
        {
            Debug.Log("UndoShape");
            _shapeHistory.Undo();
        }

        public void AddToShapeHistory(Shape3D shapeData)
        {
            Debug.Log("AddToShapeHistory");
            _shapeHistory.AddToHistory(shapeData);
        }

        public void DrawShape(ShapeData shape)
        {
            Debug.Log("DrawShape");
            if (shape.ShapeType == ShapeType.None || shape.PointData.Count == 0) return;

            DrawingHandler visualizer = _visualizerPool.Get();
            visualizer.ResetDrawer();

            // 繪製起始點
            Vector3 startPosition = GetMarkerPosition(shape.FirstPoint);
            Pose textPose = Pose.identity;
            visualizer.DrawPoint(0, startPosition);

            if (shape.ShapeType == ShapeType.Point)
            {
                Shape3D point = new Shape3D(ShapeType.Point, new Vector3[] { startPosition });
                textPose = GetMarkerPositionText(startPosition);

                if (!string.IsNullOrEmpty(shape.Description)) visualizer.DrawText(shape.Description, textPose);

                visualizer.Snapshot = point.Snapshot;
                _shapeHistory.AddToHistory(point);
                return;
            }

            Vector3 secondPosition = GetMarkerPosition(shape.SecondPoint);
            textPose = GetMarkerPositionText(startPosition, secondPosition);
            visualizer.DrawPoint(1, secondPosition);

            if (shape.ShapeType == ShapeType.Line)
            {
                Vector3[] lineVectors = new Vector3[]
                {
                startPosition,
                secondPosition
                };
                Shape3D line = new Shape3D(ShapeType.Line, lineVectors);

                if (!string.IsNullOrEmpty(shape.Description)) visualizer.DrawText(shape.Description, textPose);

                visualizer.DrawLine(lineVectors);
                visualizer.Snapshot = line.Snapshot;
                _shapeHistory.AddToHistory(line);
            }

            if (shape.ShapeType == ShapeType.Rect)
            {
                Vector3[] rectVectors = new Vector3[]
                {
                startPosition,
                GetMarkerPosition(shape.MidPoint1),
                secondPosition,
                GetMarkerPosition(shape.MidPoint2),
                startPosition
                };
                Shape3D rect = new Shape3D(ShapeType.Rect, rectVectors, secondPosition);

                if (!string.IsNullOrEmpty(shape.Description)) visualizer.DrawText(shape.Description, textPose);

                visualizer.DrawLine(rectVectors);
                visualizer.Snapshot = rect.Snapshot;
                _shapeHistory.AddToHistory(rect);
            }
        }

        public Vector3 GetMarkerPosition(PointData point)
        {
            // 將螢幕座標轉換為世界座標
            Vector3 viewPortPoint = new Vector3(point.X, point.Y, Camera.main.nearClipPlane);
            Vector3 worldPoint = Camera.main.ViewportToWorldPoint(viewPortPoint);

            // 根據相機方向應用偏移
            worldPoint += Camera.main.transform.up * _markerShiftPosition.y;
            worldPoint += Camera.main.transform.right * _markerShiftPosition.x;

            // 計算從相機到 worldPoint 的方向向量
            Vector3 direction = (worldPoint - Camera.main.transform.position).normalized;
            float extendDistance = _markerShiftPosition.z; // 延伸距離，可調整
            Vector3 extendedPoint = Camera.main.transform.position + direction * extendDistance;

            // 射線檢測
            Ray ray = new Ray(Camera.main.transform.position, direction);
            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
            {
                // 如果有碰撞點，往相機方向偏移
                Vector3 hitPoint = hit.point;

                // 使用 -direction 表示往相機方向移動
                float offsetDistance = 0.01f; // 偏移距離
                hitPoint += -direction * offsetDistance;

                return hitPoint;
            }

            // 如果無碰撞，返回延伸點
            return extendedPoint;
        }



        public Pose GetMarkerPositionText(Vector3 startPoint, Vector3? secondPoint = null)
        {
            Vector3 direction;
            if (secondPoint.HasValue) // 計算 startPoint 到 secondPoint 的方向向量，並將其反向
                direction = -(secondPoint.Value - startPoint).normalized;
            else // 若未提供 secondPoint，設置預設方向 (例如，向前方向)
                direction = Vector3.left;

            // 設置文本初始位置
            float distanceFromStart = 0.05f;  // 控制距離
            Vector3 initialTextPosition = startPoint + direction * distanceFromStart;

            // 往相機方向移動 3 公分
            Vector3 cameraPosition = Camera.main.transform.position;
            Vector3 toCameraDirection = (cameraPosition - initialTextPosition).normalized;

            float moveDistance = 0.1f;  // 3 公分 = 0.03 公尺
            Vector3 finalTextPosition = initialTextPosition + toCameraDirection * moveDistance;

            // 計算朝向相機的旋轉
            Quaternion rotation = Quaternion.LookRotation(-toCameraDirection);

            return new Pose(finalTextPosition, rotation);
        }

        private void ShapeReborn(Shape3D shape)
        {
            if (shape.ShapeType == ShapeType.None || shape.PointData.Length == 0) return;

            DrawingHandler visualizer = _visualizerPool.Get();
            visualizer.ResetDrawer();
            visualizer.Snapshot = shape.Snapshot;

            // 繪製起始點
            visualizer.DrawPoint(0, shape.FirstPoint);

            if (shape.ShapeType == ShapeType.Point) return;

            visualizer.DrawPoint(1, shape.SecondPoint);
            visualizer.DrawLine(shape.PointData);
        }
    }
}
