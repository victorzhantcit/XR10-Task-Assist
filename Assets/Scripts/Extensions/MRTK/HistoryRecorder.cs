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

        // �K�[�s���O������v��
        public void AddToHistory(T item)
        {
            _history.Push(item);
            _redoStack.Clear(); // �C���s���ާ@��A�M�� redo ���O��
        }

        // �M�P�̪񪺾ާ@
        public void Undo()
        {
            if (!CanUndo) return;
            
            T item = _history.Pop();
            _redoStack.Push(item);

            _undoAction?.Invoke(item);
        }

        // �����̪�M�P���ާ@
        public void Redo()
        {
            if (!CanRedo) return;

            T item = _redoStack.Pop();
            _history.Push(item);

            _redoAction?.Invoke(item);
        }

        // �M���Ҧ����v�O��
        public void ClearHistory()
        {
            _history.Clear();
            _redoStack.Clear();
        }
    }
}  