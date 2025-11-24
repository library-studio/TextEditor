using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryStudio.Forms
{
    // 编辑动作的历史
    public class History
    {
        List<Action> _actions = new List<Action>();

        // 当前位置
        // 指向下次可新增的一个 index 位置
        int _currentIndex = 0;

        public void Clear()
        {
            _actions.Clear();
            _currentIndex = 0;
        }

        public void Memory(Action action)
        {
            if (_actions.Count > _currentIndex)
                _actions.RemoveRange(_currentIndex, _actions.Count - _currentIndex);
            _actions.Add(action);
            _currentIndex++;
        }

        public Action Back()
        {
            if (_currentIndex == 0)
                return null;
            _currentIndex--;
            return _actions[_currentIndex];
        }

        public Action Forward()
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
    }

    // 一个编辑动作
    public class Action
    {
        // replace select 之一
        public string Name { get; set; }

        public int Start { get; set; }
        public int End { get; set; }

        public string OldText { get; set; }

        public string NewText { get; set; }
    }
}
