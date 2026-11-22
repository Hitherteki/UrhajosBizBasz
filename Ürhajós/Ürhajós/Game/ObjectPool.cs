using System;
using System.Collections.Generic;

namespace SpaceShooter
{
    public sealed class ObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _available = new Stack<T>();
        private readonly List<T> _active = new List<T>();

        public IReadOnlyList<T> Active
        {
            get { return _active; }
        }

        public T Get()
        {
            T item;

            if (_available.Count > 0)
                item = _available.Pop();
            else
                item = new T();

            _active.Add(item);
            return item;
        }

        public void Release(int index)
        {
            if (index < 0 || index >= _active.Count)
                return;

            _available.Push(_active[index]);
            _active[index] = _active[_active.Count - 1];
            _active.RemoveAt(_active.Count - 1);
        }

        public void Clear()
        {
            for (int i = 0; i < _active.Count; i++)
                _available.Push(_active[i]);

            _active.Clear();
        }
    }
}