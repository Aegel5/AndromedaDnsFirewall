using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.Utils; 
internal class ObservableDeque<T> : Deque<T> {

	//async void Updater() {
	//	while (true) {
	//		await Task.Delay(1000);
	//		if (needFullUpdate) {
	//			needFullUpdate = false;
	//			OnCollectionChanged(new NotifyCollectionChangedEventArgs(
	//				NotifyCollectionChangedAction.Reset));
	//		}
	//	}
	//}

	public ObservableDeque() {
		//Updater();
	}


	public event NotifyCollectionChangedEventHandler? CollectionChanged;

	bool needFullUpdate = false;

	public override void PushFront(T item) {
		base.PushFront(item);
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Add, item, 0));
	}

	public override T PopBack() {
		var item = base.PopBack();
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Remove,
			item,
			Count));
		return item;
	}

	public override void Clear() {
		base.Clear();
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Reset));
	}

	public void NotifyUpdated(int i) {
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Replace, this[i], this[i], i));
	}

	public void Move(int iFrom, int iTo) {
		var target = this[iFrom];
		var dir = iFrom > iTo ? 1 : -1;
		for (int i = iTo; i != iFrom; i+=dir) {
			this[i+dir] = this[i];
		}
		this[iTo] = target;
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(
			NotifyCollectionChangedAction.Move, target, iTo, iFrom));
	}

	protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
		CollectionChanged?.Invoke(this, e);

}
