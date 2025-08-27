using UnityEngine;
using UnityEngine.UI;

namespace Guidance.Utils
{
    public class OptimizedGridLayout : MonoBehaviour
    {
        public GridLayoutGroup gridLayout;   // 參考的 GridLayoutGroup
        public RectTransform container;      // 父容器（用於計算尺寸）
        public float aspectRatio = 16f / 9f; // 固定的寬高比 (16:9)
        public float spacing = 10f;          // Image 之間的間距

        private Vector2 previousContainerSize;
        private int previousChildCount;

        void Start()
        {
            previousChildCount = -1;
            previousContainerSize = new Vector2(0, 0);
            AdjustGridLayout();
        }

        private void LateUpdate()
        {
            if (ContainerSizeChanged())
                AdjustGridLayout();
        }

        private bool ContainerSizeChanged()
        {
            Vector2 currentContainerSize = container.rect.size;
            if (currentContainerSize != previousContainerSize)
            {
                previousContainerSize = currentContainerSize;
                return true; // 尺寸變更了
            }

            // 檢查子物件數量是否變更
            if (gridLayout.transform.childCount != previousChildCount)
            {
                previousChildCount = gridLayout.transform.childCount;
                return true;
            }

            return false; // 尺寸沒有變更
        }

        public void AdjustGridLayout()
        {
            int childCount = gridLayout.transform.childCount;
            if (childCount == 0)
            {
                return; // 如果沒有子物體，不需要做任何調整
            }

            // 取得容器的寬高
            float containerWidth = container.rect.width;
            float containerHeight = container.rect.height;

            // 獲取 GridLayoutGroup 的 padding 設定
            RectOffset padding = gridLayout.padding;

            // 計算可用的寬度和高度，減去 padding
            float availableWidth = containerWidth - padding.left - padding.right;
            float availableHeight = containerHeight - padding.top - padding.bottom;

            // 計算容器的寬高比
            float containerAspectRatio = availableWidth / availableHeight;

            // 計算最佳列數，根據可用寬度和子物件數量來動態決定列數
            int columns = Mathf.CeilToInt(Mathf.Sqrt(childCount * (containerAspectRatio / aspectRatio)));
            int rows = Mathf.CeilToInt((float)childCount / columns);

            // 計算總間距
            float totalSpacingWidth = (columns - 1) * spacing;
            float totalSpacingHeight = (rows - 1) * spacing;

            // 重新計算可用寬度和高度，扣除 spacing
            availableWidth -= totalSpacingWidth;
            availableHeight -= totalSpacingHeight;

            // 計算每個單元格的寬度和高度，考慮 16:9 比例
            float cellWidth = availableWidth / columns;
            float cellHeight = cellWidth / aspectRatio;

            // 如果高度不足以顯示完整的 16:9 比例，則以高度為基準重新計算
            if (cellHeight * rows > availableHeight)
            {
                cellHeight = availableHeight / rows;
                cellWidth = cellHeight * aspectRatio;
            }

            // 設置 GridLayoutGroup 的 cellSize
            gridLayout.cellSize = new Vector2(cellWidth, cellHeight);

            // 設置 GridLayoutGroup 的 spacing
            gridLayout.spacing = new Vector2(spacing, spacing);
        }
    }
}
