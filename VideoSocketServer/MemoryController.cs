using System;
using System.Collections.Generic;

namespace VideoSocketServer
{
    public class MemoryController<T>
        where T : class
    {
        internal T[] _items;
        internal static uint _next;
        internal static uint _latest;
        internal static uint _oldest;
        internal static object _lockable;

        public event EventHandler<uint> OnNewItem;

        public MemoryController(uint capacity = 1000)
        {
            _next = 0;
            _latest = 0;
            _oldest = 0;
            _items = new T[capacity];
            _lockable = new object();
        }
        
        //shared video list
        public uint AddItem(T item)
        {
            uint result = 0;
            lock (_lockable)
            {
                _items[_next] = item;
                result = _next;
                _oldest = _items[_items.Length - 1] == null ? 0 : (result + 1 > _items.Length - 1 ? 0 : result + 1);
                _latest = _next;
                _next = (_next + 1 < _items.Length) ? _next + 1 : 0;
                RaiseNewItemEvent(result);
            }
            return result;
        }
        
        public T GetItem(uint? position = null)
        {
            uint finalPos = _oldest;
            if (position != null)
                finalPos = (uint)position;

            if (_items[finalPos] == null)
                throw new Exception("Requested position is invalid");

            return _items[finalPos];
        }

        //event raising
        private void RaiseNewItemEvent(uint position)
        {
            if (OnNewItem != null)
                OnNewItem(this, position);
        }
    }
}