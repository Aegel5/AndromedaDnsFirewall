using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.Utils;

internal class ObservableDeque<T> : Deque<T>, INotifyCollectionChanged {

	List<int> indexToUpdate = new();

	async void Updater() {
		while (true) {
			await Task.Delay(1000);
			NotifyUpdate();
		}
	}

	void NotifyUpdate() {
		if (indexToUpdate.Count == 0)
			return;
		foreach (var item in indexToUpdate) {
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(
				NotifyCollectionChangedAction.Replace, this[item], this[item], item));
		}
		indexToUpdate.Clear();
	}

	public ObservableDeque() {
		Updater();
	}


	public event NotifyCollectionChangedEventHandler? CollectionChanged;

	public void PushFrontNotify(T item) {
		NotifyUpdate();
		base.PushFront(item);
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Add, item, 0));
	}

	public T PopBackNofity() {
		NotifyUpdate();
		var item = base.PopBack();
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Remove,
			item,
			Count));
		return item;
	}

	public void ClearNotify() {
		indexToUpdate.Clear();
		base.Clear();
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Reset));
	}

	public void NotifyUpdated(int i) {
		if (indexToUpdate.Contains(i))
			return;
		indexToUpdate.Add(i);
	}

	//public void Move(int iFrom, int iTo) {
	//	var target = this[iFrom];
	//	var dir = iFrom > iTo ? 1 : -1;
	//	for (int i = iFrom; i != iTo; i-=dir) {
	//		this[i] = this[i-dir];
	//	}
	//	this[iTo] = target;
	//	OnCollectionChanged(new NotifyCollectionChangedEventArgs(
	//		NotifyCollectionChangedAction.Move, target, iTo, iFrom));
	//}

	protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
		CollectionChanged?.Invoke(this, e);

}
