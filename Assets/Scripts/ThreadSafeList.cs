﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable()]
internal class ThreadSafeList<T> : IList<T>
{
    private List<T> _list;
    private object _root;

    internal ThreadSafeList(List<T> list)
    {
        _list = list;
        _root = ((System.Collections.ICollection)list).SyncRoot;
    }

    public int Count {
        get {
            lock (_root)
            {
                return _list.Count;
            }
        }
    }

    public bool IsReadOnly {
        get {
            return ((ICollection<T>)_list).IsReadOnly;
        }
    }

    public void Add(T item)
    {
        lock (_root)
        {
            _list.Add(item);
        }
    }

    public void Clear()
    {
        lock (_root)
        {
            _list.Clear();
        }
    }

    public bool Contains(T item)
    {
        lock (_root)
        {
            return _list.Contains(item);
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (_root)
        {
            _list.CopyTo(array, arrayIndex);
        }
    }

    public bool Remove(T item)
    {
        lock (_root)
        {
            return _list.Remove(item);
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        lock (_root)
        {
            return _list.GetEnumerator();
        }
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        lock (_root)
        {
            return ((IEnumerable<T>)_list).GetEnumerator();
        }
    }

    public T this[int index] {
        get {
            lock (_root)
            {
                return _list[index];
            }
        }
        set {
            lock (_root)
            {
                _list[index] = value;
            }
        }
    }

    public int IndexOf(T item)
    {
        lock (_root)
        {
            return _list.IndexOf(item);
        }
    }

    public void Insert(int index, T item)
    {
        lock (_root)
        {
            _list.Insert(index, item);
        }
    }

    public void RemoveAt(int index)
    {
        lock (_root)
        {
            _list.RemoveAt(index);
        }
    }

    public List<T> Clone()
    {
        List<T> newList = new List<T>();

        lock (_root)
        {
            _list.ForEach(x => newList.Add(x));
        }

        return newList;
    }
}