using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryStudio.Forms
{
    // 编辑动作的历史
    public class History
    {
        List<EditAction> _actions = new List<EditAction>();
        int _maxItems = 10 * 1024;

        // 当前位置
        // 指向下次可新增的一个 index 位置
        int _currentIndex = 0;

        public History(int maxItems)
        {
            _maxItems = maxItems;
        }

        public void Clear()
        {
            _actions.Clear();
            _currentIndex = 0;
        }

        public void Memory(EditAction action)
        {
            if (_actions.Count > _currentIndex)
                _actions.RemoveRange(_currentIndex, _actions.Count - _currentIndex);

            // 防止元素数超过限额
            if (_actions.Count > _maxItems)
            {
                _actions.RemoveRange(0, _actions.Count - _maxItems);
                Debug.Assert(_actions.Count <= _maxItems);
            }
            _actions.Add(action);
            _currentIndex++;
        }

        public EditAction Back()
        {
            if (_currentIndex == 0)
                return null;
            _currentIndex--;
            return _actions[_currentIndex];
        }

        public EditAction Forward()
        {
            if (_currentIndex >= _actions.Count)
                return null;
            var action = _actions[_currentIndex];
            _currentIndex++;
            return action;
        }

        public bool CanUndo()
        {
            if (_actions.Count > 0 && _currentIndex > 0)
                return true;
            return false;
        }

        public bool CanRedo()
        {
            if (_actions.Count > 0 && _currentIndex < _actions.Count)
                return true;
            return false;
        }

        public override string ToString()
        {
            var text = new StringBuilder();
            foreach (var action in _actions)
            {
                text.AppendLine(action.ToString());
            }

            return text.ToString();
        }
    }

    // 一个编辑动作
    public class EditAction
    {
        // replace select 之一
        public string Name { get; set; }

        public int Start { get; set; }
        public int End { get; set; }

        public string OldText { get; set; }

        public string NewText { get; set; }
        
        public override string ToString()
        {
            return $"Name={Name}, Start={Start}, End={End}, OldText='{OldText}', NewText='{NewText}'";
        }
    }
}
