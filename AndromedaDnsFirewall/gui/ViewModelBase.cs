using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AndromedaDnsFirewall.gui;

internal class ViewModelBase : INotifyPropertyChanged {

	public event PropertyChangedEventHandler? PropertyChanged;

	// Универсальный метод для обновления значения
	protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) {
		if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
		storage = value;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		return true;
	}
}
