using System.Collections.Generic;
using System;

namespace MRTK.Extensions
{
    public class HistoryRecorder<T>
    {
        private readonly Stack<T> _history = new Stack<T>();
        private readonly Stack<T> _redoStack = new Stack<T>();

        private readonly Action<T> _undoAction;
        private readonly Action<T> _redoAction;

        public bool CanUndo => _history.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public HistoryRecorder(Action<T> undoAction, Action<T> redoAction)
        {
            _undoAction = undoAction;
            _redoAction = redoAction;
        }

        // 添加新的記錄到歷史中
        public void AddToHistory(T item)
        {
            _history.Push(item);
            _redoStack.Clear(); // 每次新的操作後，清空 redo 的記錄
        }

        // 撤銷最近的操作
        public void Undo()
        {
            if (!CanUndo) return;
            
            T item = _history.Pop();
            _redoStack.Push(item);

            _undoAction?.Invoke(item);
        }

        // 重做最近撤銷的操作
        public void Redo()
        {
            if (!CanRedo) return;

            T item = _redoStack.Pop();
            _history.Push(item);

            _redoAction?.Invoke(item);
        }

        // 清除所有歷史記錄
        public void ClearHistory()
        {
            _history.Clear();
            _redoStack.Clear();
        }
    }
}  