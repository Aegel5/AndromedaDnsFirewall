//## Deque
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class Deque<T> : IEnumerable<T>, IList<T>, IList {
		
    private int _offset, _mask;
    private T[] _buffer;
    public Deque() : this(16) { }


    public Deque(int capacity) {
        Debug.Assert(capacity > 0);
        capacity = (int)BitOperations.RoundUpToPowerOf2((uint)capacity);
        //Debug.Assert((capacity & (capacity - 1)) == 0);
        _mask = capacity - 1;
        _offset = 0;
        _buffer = new T[capacity];
        Count = 0;
    }

    public Deque(ICollection<T> lst) : this(lst.Count) {
        foreach (T t in lst) {
            PushBack(t);
        }
    }

    public Deque(IEnumerable<T> lst) : this() {
        foreach (T t in lst) {
            PushBack(t);
        }
    }

    [MethodImpl(256)] ref T get(int i) => ref _buffer[(_offset + i) & _mask];
    public void PushBack(T item) { if (Count == _buffer.Length) Extend();  get(Count++) = item; }
    public void Add(T item) { PushBack(item); }
    public T PopBack() { Debug.Assert(Count > 0); return get(--Count); }

	public void PushFront(T item) {
        if (Count == _buffer.Length) Extend();
        Count++;
        _buffer[(--_offset) & _mask] = item;
    }

    public T PopFront() {
        Debug.Assert(Count > 0);
        Count--;
        return _buffer[(_offset++) & _mask];
    }

    private void Extend() {
        int capacity = _buffer.Length;
        Debug.Assert(Count == capacity);
        T[] tmpBuf = new T[2 * capacity];
        for (int i = 0; i < capacity; i++) {
            tmpBuf[i] = get(i);
        }
        _mask = 2 * capacity - 1;
        _buffer = tmpBuf;
        _offset = 0;
    }

    public int Count { get; private set; }

    public T Front {
        get {
            Debug.Assert(Count > 0);
            return _buffer[_offset & _mask];
        }
    }
    public T Back {
        get {
            Debug.Assert(Count > 0);
            return get(Count - 1);
        }
    }

    public bool IsEmpty => Count == 0;

	public bool IsReadOnly => false;

	public bool IsFixedSize => false;

	public bool IsSynchronized => false;

	public object SyncRoot => this;

	object? IList.this[int index] { get => this[index]; set => get(index) = this[index] = (T)value!; }
	T IList<T>.this[int index] { get => get(index); set => get(index)=value; }

	public ref T this[int i] => ref get(i);
    public void Clear() { Count = 0; }

    public void Insert(int index, T item) {
        Debug.Assert(0 <= index && index <= Count);
        if (Count == _buffer.Length) Extend();
        if (Count - index - 1 <= index) {
            for (int i = Count - 1; i >= index; i--) {
                get(i + 1) = get(i);
            }
        } else {
            for (int i = 0; i < index; i++) {
                get(i - 1) = get(i);
            }
            _offset--;
        }
        get(index) = item;
        Count++;
    }

    public void RemoveAt(int index) {
        Debug.Assert(0 <= index && index < Count);

        if (Count - index - 1 <= index) {
            for (int i = index + 1; i < Count; i++) {
                get(i - 1) = get(i);
            }
        } else {
            for (int i = index - 1; i >= 0; i--) {
                get(i + 1) = get(i);
            }
            _offset++;
        }
        Count--;
    }

    public IEnumerator<T> GetEnumerator() {
        for (int i = 0; i < Count; i++) {
            yield return get(i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

	public int IndexOf(T item) {
		for (int i = 0; i < Count; i++) {
			if (EqualityComparer<T>.Default.Equals(this[i], item)) return i;
		}
		return -1;
	}

	public bool Contains(T item) {
		if (Count == 0) return false;
		return IndexOf(item) != -1;
	}

	public void CopyTo(T[] array, int arrayIndex) {
		for (int i = 0; i < Count; i++) {
			array[arrayIndex + i] = this[i];
		}
	}

	public bool Remove(T item) {
		int index = IndexOf(item);
		if (index >= 0) {
			RemoveAt(index);
			return true;
		}
		return false;
	}

	public int Add(object? value) {
		Add((T)value!);
		return Count - 1;
	}

	public bool Contains(object? value) {
		return Contains((T)value!);
	}

	public int IndexOf(object? value) {
		return IndexOf((T)value!);
	}

	public void Insert(int index, object? value) {
		Insert(index, (T)value!);
	}

	public void Remove(object? value) {
		Remove((T)value!);
	}

	public void CopyTo(Array array, int index) {
		for (int i = 0; i < Count; i++) {
			// Записываем через SetValue (работает медленнее, но универсально)
			array.SetValue(this[i], index + i);
		}
	}
}




