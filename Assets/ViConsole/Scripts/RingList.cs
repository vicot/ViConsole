using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ViConsole
{
    public class RingList<T> : IList<T>, IList
    {
        public int MaxLength { get; set; } = 5;

        ConcurrentQueue<T> _queue = new();

        public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(T item)
        {
            _queue.Enqueue(item);
            while (_queue.Count > MaxLength) _queue.TryDequeue(out _);
        }

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        void IList.Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public bool IsFixedSize { get; } = false;

        bool IList.IsReadOnly => false;

        object IList.this[int index]
        {
            get => this[index];
            set => throw new NotImplementedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        int ICollection.Count => _queue.Count;

        public bool IsSynchronized { get; }
        public object SyncRoot { get; }

        int ICollection<T>.Count => _queue.Count;

        bool ICollection<T>.IsReadOnly => false;

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public T this[int index]
        {
            get => _queue.ElementAt(index);
            set => throw new NotImplementedException();
        }
    }
}