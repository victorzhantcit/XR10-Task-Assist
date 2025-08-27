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
        /// ArMeshManager ���Ӹm�� MRTK XR Rig/Camera Offset�ؿ��U�A�ҥήɷ|�۰ʧ���ʺA�ͦ�������s�ժ��� 
        /// </summary>
        public void ActivateArMesh(bool isActive)
        {
            // ���o AR Mesh Manager ���ͺ��檺���X
            if (_arMeshGroup == null)
            {
                Transform cameraOffset = _arMeshManager.transform.parent;
                _arMeshGroup = cameraOffset.GetChild(cameraOffset.childCount - 1).gameObject;
            }

            // �ҥ�/���� AR Mesh �����P���
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

            // ø�s�_�l�I
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
            // �N�ù��y���ഫ���@�ɮy��
            Vector3 viewPortPoint = new Vector3(point.X, point.Y, Camera.main.nearClipPlane);
            Vector3 worldPoint = Camera.main.ViewportToWorldPoint(viewPortPoint);

            // �ھڬ۾���V���ΰ���
            worldPoint += Camera.main.transform.up * _markerShiftPosition.y;
            worldPoint += Camera.main.transform.right * _markerShiftPosition.x;

            // �p��q�۾��� worldPoint ����V�V�q
            Vector3 direction = (worldPoint - Camera.main.transform.position).normalized;
            float extendDistance = _markerShiftPosition.z; // �����Z���A�i�վ�
            Vector3 extendedPoint = Camera.main.transform.position + direction * extendDistance;

            // �g�u�˴�
            Ray ray = new Ray(Camera.main.transform.position, direction);
            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
            {
                // �p�G���I���I�A���۾���V����
                Vector3 hitPoint = hit.point;

                // �ϥ� -direction ��ܩ��۾���V����
                float offsetDistance = 0.01f; // �����Z��
                hitPoint += -direction * offsetDistance;

                return hitPoint;
            }

            // �p�G�L�I���A��^�����I
            return extendedPoint;
        }



        public Pose GetMarkerPositionText(Vector3 startPoint, Vector3? secondPoint = null)
        {
            Vector3 direction;
            if (secondPoint.HasValue) // �p�� startPoint �� secondPoint ����V�V�q�A�ñN��ϦV
                direction = -(secondPoint.Value - startPoint).normalized;
            else // �Y������ secondPoint�A�]�m�w�]��V (�Ҧp�A�V�e��V)
                direction = Vector3.left;

            // �]�m�奻��l��m
            float distanceFromStart = 0.05f;  // ����Z��
            Vector3 initialTextPosition = startPoint + direction * distanceFromStart;

            // ���۾���V���� 3 ����
            Vector3 cameraPosition = Camera.main.transform.position;
            Vector3 toCameraDirection = (cameraPosition - initialTextPosition).normalized;

            float moveDistance = 0.1f;  // 3 ���� = 0.03 ����
            Vector3 finalTextPosition = initialTextPosition + toCameraDirection * moveDistance;

            // �p��¦V�۾�������
            Quaternion rotation = Quaternion.LookRotation(-toCameraDirection);

            return new Pose(finalTextPosition, rotation);
        }

        private void ShapeReborn(Shape3D shape)
        {
            if (shape.ShapeType == ShapeType.None || shape.PointData.Length == 0) return;

            DrawingHandler visualizer = _visualizerPool.Get();
            visualizer.ResetDrawer();
            visualizer.Snapshot = shape.Snapshot;

            // ø�s�_�l�I
            visualizer.DrawPoint(0, shape.FirstPoint);

            if (shape.ShapeType == ShapeType.Point) return;

            visualizer.DrawPoint(1, shape.SecondPoint);
            visualizer.DrawLine(shape.PointData);
        }
    }
}
