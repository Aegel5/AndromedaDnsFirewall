using System;
using System.Threading;
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










