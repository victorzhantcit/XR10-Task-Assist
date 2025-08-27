using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WorkOrder.Dtos;
using WorkOrder.Utils;
using XCharts.Runtime;

namespace WorkOrder.Core
{
    public enum OrderStatus
    {
        Default,
        Completed,
        Processing,
        Pending
    }

    public class WorkOrderListView : EnumStateVisualizer<OrderStatus>
    {
        public delegate void WorkOrderClicked(int index);
        public event WorkOrderClicked OnOrderItemClicked;

        [SerializeField] private WorkOrderRootUI _workOrderRootUI;

        [Header("UI")]
        [SerializeField] private VirtualizedScrollRectList _workOrderList;
        [SerializeField] private RingChart _completionRingChart;
        [SerializeField] private PressableButton _updateDataButton;
        private List<WorkOrderDto> _workOrders = new();
        private List<int> _fetchOrderIndexes = new();
        private OrderStatus _currentFetchStatus = OrderStatus.Default;
        private float savedScrollPosition = 0f;

        private new void Start()
        {
            _workOrderList.OnVisible += OnWorkOrderItemVisible;
            ShowAllOrders();
        }

        private void OnWorkOrderItemVisible(GameObject gameObject, int index)
        {
            if (index < 0 || index >= _workOrders.Count) return;

            WorkOrderListItem item = gameObject.GetComponent<WorkOrderListItem>();
            int fetchIndex = _fetchOrderIndexes[index];

            item.SetContent(_workOrders[fetchIndex]);
            item.SetColor(_workOrderRootUI.GetColorByStatus(_workOrders[fetchIndex].status));
            item.SetEditAction(() => OnOrderItemClicked?.Invoke(fetchIndex));
        }

        /// <param name="completionRate">完成率 (0~100)</param>
        private void UpdateCompletionRate(float completionRate)
        {
            //Debug.Log(completionRate);
            _completionRingChart.ClearData();

            var serie = _completionRingChart.GetSerie<Ring>();
            serie.AddData(completionRate, 100f);
        }

        /// <returns>取得已完成狀態的百分比整數值</returns>
        private int GetCompletedRate()
        {
            //Debug.Log((_workOrders == null || _workOrders.Count == 0));
            if (_workOrders == null || _workOrders.Count == 0) return 0;

            int completedCount = _workOrders.Count(workOrder =>
                _workOrderRootUI.GetColorByStatus(workOrder.status) == _workOrderRootUI.GetColorByStatus("c-processed"));
            float completionRate = ((float)completedCount / _workOrders.Count) * 100;

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
            _workOrderList.SetItemCount(0);
            _workOrderList.SetItemCount(_fetchOrderIndexes.Count);
            _workOrderList.ResetLayout();
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
                return Enumerable.Range(0, _workOrders.Count).ToList();

            // 篩選符合狀態的索引列表
            return _workOrders
                .Select((workOrder, index) => new { workOrder, index }) // 同時取得索引與項目
                .Where(item =>
                    _workOrderRootUI.GetColorByStatus(item.workOrder.status) == _workOrderRootUI.GetColorByStatus(statusCode))
                .Select(item => item.index) // 取索引
                .ToList();
        }

        public void SetVisible(bool visible)
        {
            this.gameObject.SetActive(visible);
            if (visible) RebuildLayout();
            else savedScrollPosition = _workOrderList.Scroll;
        }

        private void RebuildLayout()
        {
            UpdateCompletionRate(GetCompletedRate());
            ShowStatusOrder(_currentFetchStatus);
            _workOrderList.Scroll = savedScrollPosition;
        }

        public void UpdateData(List<WorkOrderDto> workOrders)
        {
            _workOrders = workOrders;
            ShowStatusOrder(_currentFetchStatus);
            UpdateCompletionRate(GetCompletedRate());
        }
    }
}
