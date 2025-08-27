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

        /// <param name="completionRate">完成率 (0~100)</param>
        private void UpdateCompletionRate(float completionRate)
        {
            _completionRingChart.ClearData();

            var serie = _completionRingChart.GetSerie<Ring>();
            serie.AddData(completionRate, 100f);
        }

        /// <returns>取得已完成狀態的百分比整數值</returns>
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
            // 根據 OrderStatus 轉換為對應的狀態碼
            string statusCode = status switch
            {
                OrderStatus.Completed => "c-processed",
                OrderStatus.Processing => "c-processing",
                OrderStatus.Pending => "c-pending",
                _ => null // 對於未知狀態，直接返回整個索引列表
            };

            // 如果 statusCode 為 null，返回完整的索引列表
            if (string.IsNullOrEmpty(statusCode))
                return Enumerable.Range(0, _inspectionDatas.Count).ToList();

            // 篩選符合狀態的索引列表
            return _inspectionDatas
                .Select((workOrder, index) => new { workOrder, index }) // 同時取得索引與項目
                .Where(item =>
                _inspectionRootUI.GetColorByStatus(item.workOrder.status) == _inspectionRootUI.GetColorByStatus(statusCode))
                .Select(item => item.index) // 取索引
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
