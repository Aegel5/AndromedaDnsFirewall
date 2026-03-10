using System;
using System.Linq;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;

public class AsyncUtils {
	static public async Task<bool> WaitWithTimeout(Task task, TimeSpan timeout) {
		var task_res = await Task.WhenAny(task, Task.Delay(timeout));
		if (task_res == task) {
			return true;
		}
		return false;
	}

	static public async Task<Task> GetFirstSuccess(params Task[] tasks) {
		var taskList = tasks.ToList();

		while (taskList.Count > 0) {
			// Ждем завершения любой из оставшихся задач
			Task completedTask = await Task.WhenAny(taskList);

			// Если задача завершилась успешно — возвращаем её
			if (completedTask.Status == TaskStatus.RanToCompletion) {
				return completedTask;
			}

			// Если задача упала или была отменена, убираем её из списка и продолжаем поиск
			taskList.Remove(completedTask);
		}

		await tasks[0]; // получаем exception
		throw new Exception("All failed");
	}
}

// самая простая реализация на TaskCompletionSource
public class AOneTimePulse {
	TaskCompletionSource<bool> _tcs = new();
	public Task Wait() => _tcs.Task;
	async public Task<bool> Wait(TimeSpan timeout) {
		return await AsyncUtils.WaitWithTimeout(_tcs.Task, timeout);
	}
	public void Pulse() {
		_tcs.TrySetResult(true);
	}
	public bool IsPulsed => _tcs.Task.IsCompleted;
}

public class ACounter {
	public class ACounter_Holder : IDisposable {
		ACounter data;
		bool disposed = false;

		public ACounter_Holder(ACounter data) {
			this.data = data;
			data.Count++;
		}

		public void Dispose() { // Поддерживаем множественный Dispose
			if (disposed) return;
			disposed = true;
			data.Count--;
		}
	}
	public IDisposable TakeOrExcept() {
		if (IsTaked) {
			throw new Exception("Already taked");
		}
		return new ACounter_Holder(this);
	}
	public IDisposable Take() {
		return new ACounter_Holder(this);
	}
	public bool IsTaked => Count != 0;
	public int Count { get; private set; }

}










