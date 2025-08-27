using TMPro;
using UnityEngine;

namespace Guidance.Core
{
    public class DrawingHandler : MonoBehaviour
    {
        [SerializeField] private Transform _remoteVisualPointer;
        [SerializeField] private Transform _secondVisualPointer;
        [SerializeField] private LineRenderer _shapelineRenderer;
        [SerializeField] private LineRenderer _textLineRenderer;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Gradient _lineColorGradient;
        [SerializeField] private Gradient _rectColorGradient;

        public float LineWidth = 0.005f;
        public string Snapshot { get; set; }

        public void DrawLine(Vector3[] positions)
        {
            if (positions.Length == 2)
                DrawLine(positions, _lineColorGradient);
            else if (positions.Length == 5)
                DrawLine(positions, _rectColorGradient);
        }

        public void DrawPoint(int pointIndex, Vector3 position)
        {
            if (pointIndex == 0)
            {
                _remoteVisualPointer.position = position;
                _remoteVisualPointer.gameObject.SetActive(true);
            }
            else
            {
                _secondVisualPointer.position = position;
                _secondVisualPointer.gameObject.SetActive(true);
            }
        }

        public void DrawText(string description, Pose textPose)
        {
            _label.text = description;
            _label.transform.SetPositionAndRotation(textPose.position, textPose.rotation);
            _label.gameObject.SetActive(true);
            _textLineRenderer.positionCount = 2;
            _textLineRenderer.SetPositions(new Vector3[] { _remoteVisualPointer.position, textPose.position });
        }

        public void ResetDrawer()
        {
            _remoteVisualPointer.gameObject.SetActive(false);
            _secondVisualPointer.gameObject.SetActive(false);
            _label.gameObject.SetActive(false);
            _shapelineRenderer.positionCount = 0;
            _textLineRenderer.positionCount = 0;
            Snapshot = string.Empty;
        }

        private void DrawLine(Vector3[] positions, Gradient gradient)
        {
            _shapelineRenderer.positionCount = positions.Length;
            _shapelineRenderer.colorGradient = gradient;
            _shapelineRenderer.startWidth = LineWidth;
            _shapelineRenderer.endWidth = LineWidth;
            _shapelineRenderer.SetPositions(positions);
        }

        private void SetText(string text, Vector3 position)
        {
            _label.text = text;
        }
    }
}

