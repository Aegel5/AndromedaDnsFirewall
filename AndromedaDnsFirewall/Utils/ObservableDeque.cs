using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.Utils; 
internal class ObservableDeque<T> : IList<T>, IEnumerable<T>, INotifyCollectionChanged, IList {
	Deque<T> _items = new();

	async void Updater() {
		//while (true) {
		//	await Task.Delay(1000);
		//	if (needFullUpdate) {
		//		needFullUpdate = false;
		//		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
		//			NotifyCollectionChangedAction.Reset));
		//	}
		//}
	}

	public ObservableDeque() {
		Updater();
	}

	public int Count => _items.Count;


	public event NotifyCollectionChangedEventHandler? CollectionChanged;

	bool needFullUpdate = false;

	public void PushFront(T item) {
		_items.PushFront(item);
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Add, item, 0));
	}

	public void PopBack() {
		var item = _items.PopBack();
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Remove,
			item,
			_items.Count));
	}

	public void Clear() {
		_items.Clear();
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Reset));
	}

	public T Front => _items.Front;

	// ----- Interfaces

	public bool IsReadOnly => ((ICollection<T>)_items).IsReadOnly;

	public bool IsFixedSize => false;

	public bool IsSynchronized => false;

	public object SyncRoot => this;

	object? IList.this[int index] { get => this[index]; set => this[index] = (T)value!; }
	public T this[int index] { get => ((IList<T>)_items)[index]; set => ((IList<T>)_items)[index] = value; }

	public void FrontUpdated() {
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Replace, _items.Front, _items.Front, 0));
	}

	protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
		CollectionChanged?.Invoke(this, e);

	public int IndexOf(T item) {
		return ((IList<T>)_items).IndexOf(item);
	}

	public void Insert(int index, T item) {
		((IList<T>)_items).Insert(index, item);
	}

	public void RemoveAt(int index) {
		((IList<T>)_items).RemoveAt(index);
	}

	public void Add(T item) {
		((ICollection<T>)_items).Add(item);
	}

	public bool Contains(T item) {
		return ((ICollection<T>)_items).Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex) {
		((ICollection<T>)_items).CopyTo(array, arrayIndex);
	}

	public bool Remove(T item) {
		return ((ICollection<T>)_items).Remove(item);
	}

	public IEnumerator<T> GetEnumerator() {
		return ((IEnumerable<T>)_items).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return ((IEnumerable)_items).GetEnumerator();
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
