using System.Collections.Generic;

namespace Framework.Modules.Timer
{
    internal class TimerMinHeap
    {
        private readonly List<Timer> _items = new();

        public int Count => _items.Count;

        public void Push(Timer timer)
        {
            _items.Add(timer);
            HeapifyUp(_items.Count - 1);
        }

        public Timer Pop()
        {
            if (_items.Count == 0) return null;
            var result = _items[0];
            _items[0] = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);
            HeapifyDown(0);
            return result;
        }

        public Timer Peek() => _items.Count > 0 ? _items[0] : null;

        public void Clear() => _items.Clear();

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_items[index].CompareTo(_items[parent]) >= 0) break;
                Swap(index, parent);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            while (true)
            {
                int left = index * 2 + 1;
                int right = index * 2 + 2;
                int smallest = index;

                if (left < _items.Count && _items[left].CompareTo(_items[smallest]) < 0)
                    smallest = left;
                if (right < _items.Count && _items[right].CompareTo(_items[smallest]) < 0)
                    smallest = right;

                if (smallest == index) break;
                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            var temp = _items[a];
            _items[a] = _items[b];
            _items[b] = temp;
        }
    }
}
