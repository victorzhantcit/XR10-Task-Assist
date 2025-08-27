using Inspection.Dtos;
using Inspection.Utils;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XCharts.Runtime;

namespace Inspection.Core
{
    public enum OrderStatus
    {
        Default,
        Completed,
        Processing,
        Pending
    }

    public class InspectionListView : EnumStateVisualizer<OrderStatus>
    {
        public delegate void InspctionClicked(int index);
        public event InspctionClicked OnInspctionItemClicked;

        [SerializeField] private InspectionRootUI _inspectionRootUI;

        [Header("UI")]
        [SerializeField] private VirtualizedScrollRectList _inspectionList;
        [SerializeField] private RingChart _completionRingChart;
        [SerializeField] private PressableButton _updateDataButton;
        private List<InspectionDto> _inspectionDatas = new();
        private List<int> _fetchOrderIndexes = new();
        private OrderStatus _currentFetchStatus = OrderStatus.Default;
        private float savedScrollPosition = 0f;

        private new void Start()
        {
            _inspectionList.OnVisible += OnInspectionItemVisible;
            ShowAllOrders();
        }

        private void OnInspectionItemVisible(GameObject gameObject, int index)
        {
            if (index < 0 || index >= _inspectionDatas.Count) return;

            InspectionListItem item = gameObject.GetComponent<InspectionListItem>();
            int fetchIndex = _fetchOrderIndexes[index];

            item.SetContent(_inspectionDatas[fetchIndex]);
            item.SetColor(_inspectionRootUI.GetColorByStatus(_inspectionDatas[fetchIndex].status));
            item.SetEditAction(() => OnInspctionItemClicked?.Invoke(fetchIndex));
        }

        /// <param name="completionRate">�����v (0~100)</param>
        private void UpdateCompletionRate(float completionRate)
        {
            _completionRingChart.ClearData();

            var serie = _completionRingChart.GetSerie<Ring>();
            serie.AddData(completionRate, 100f);
        }

        /// <returns>���o�w�������A���ʤ����ƭ�</returns>
        private int GetCompletedRate()
        {
            if (_inspectionDatas == null || _inspectionDatas.Count == 0) return 0;

            int completedCount = _inspectionDatas.Count(workOrder =>
                _inspectionRootUI.GetColorByStatus(workOrder.status) == _inspectionRootUI.PositiveColor);
            float completionRate = ((float)completedCount / _inspectionDatas.Count) * 100;

            return Mathf.RoundToInt(completionRate);
        }

        public void SetUpdateButtonEnable(bool enable) => _updateDataButton.enabled = enable;
        public void ShowCompletedOrders() => ShowStatusOrder(OrderStatus.Completed);
        public void ShowProcessingOrders() => ShowStatusOrder(OrderStatus.Processing);
        public void ShowPendingOrders() => ShowStatusOrder(OrderStatus.Pending);
        public void ShowAllOrders() => ShowStatusOrder(OrderStatus.Default);

        private void ShowStatusOrder(OrderStatus status)
        {
            _fetchOrderIndexes = GetWorkOrderIndexesByStatus(status);
            _inspectionList.SetItemCount(0);
            _inspectionList.SetItemCount(_fetchOrderIndexes.Count);
            _inspectionList.ResetLayout();
            base.SetEnumValue(status);
        }

        private List<int> GetWorkOrderIndexesByStatus(OrderStatus status)
        {
            _currentFetchStatus = status;
            // �ھ� OrderStatus �ഫ�����������A�X
            string statusCode = status switch
            {
                OrderStatus.Completed => "c-processed",
                OrderStatus.Processing => "c-processing",
                OrderStatus.Pending => "c-pending",
                _ => null // ��󥼪����A�A������^��ӯ��ަC��
            };

            // �p�G statusCode �� null�A��^���㪺���ަC��
            if (string.IsNullOrEmpty(statusCode))
                return Enumerable.Range(0, _inspectionDatas.Count).ToList();

            // �z��ŦX���A�����ަC��
            return _inspectionDatas
                .Select((workOrder, index) => new { workOrder, index }) // �P�ɨ��o���޻P����
                .Where(item =>
                _inspectionRootUI.GetColorByStatus(item.workOrder.status) == _inspectionRootUI.GetColorByStatus(statusCode))
                .Select(item => item.index) // ������
                .ToList();
        }

        public void SetVisible(bool visible)
        {
            this.gameObject.SetActive(visible);
            if (visible) RebuildLayout();
            else savedScrollPosition = _inspectionList.Scroll;
        }

        private void RebuildLayout()
        {
            UpdateCompletionRate(GetCompletedRate());
            ShowStatusOrder(_currentFetchStatus);
            _inspectionList.Scroll = savedScrollPosition;
        }

        public void UpdateData(List<InspectionDto> inspectionDtos)
        {
            _inspectionDatas = inspectionDtos;
            ShowStatusOrder(_currentFetchStatus);
            UpdateCompletionRate(GetCompletedRate());
        }
    }
}
