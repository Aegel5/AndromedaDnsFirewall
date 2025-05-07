using Nito.AsyncEx;
using System;
using System.Diagnostics.CodeAnalysis;
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
    static public async Task WaitOrThrowCancelable(Task task, CancellationTokenSource token, TimeSpan timeout) {
        if (!await WaitWithTimeout(task, timeout)) {
            token.Cancel(); // запросим cancel
            try {
                await task; // здесь по идее случится exception cancel request
            } catch (TaskCanceledException) {
            }
            throw new Exception($"timeout wait task {timeout.TotalSeconds} sec"); // на всякий случай сами кинем exception 
        }
    }

}

public class AWaitCondVariable {

    AsyncLock mtx = new();
    AsyncConditionVariable cv;
    public AWaitCondVariable() {
        cv = new(mtx);
    }

    async public Task<bool> Wait(TimeSpan ts) {
        using (await mtx.LockAsync()) {
            await cv.WaitAsync(); // TODO wait ts
        }
        return true;
    }

    async public Task Pulse() {
        using (await mtx.LockAsync()) {
            cv.NotifyAll();
        }
    }
}

public class AOneTimePulse {

    AsyncManualResetEvent evt = new();
    async public Task<bool> Wait(TimeSpan timeout) {
        if (evt.IsSet) return true;
        return await AsyncUtils.WaitWithTimeout(evt.WaitAsync(), timeout);
    }
    public Task Wait() => evt.WaitAsync();
    public void Pulse() {
        evt.Set();
    }
    public bool IsPulsed => evt.IsSet;
}

// Сомнительная реализация на cancelation token
public class AOneTimePulse2 {
    CancellationTokenSource token = new();
    // Логика как и у condition variable, сначала проверка, потом wait, потом снова проверка.
    async public Task<bool> Wait(TimeSpan ts) {
        if (token.IsCancellationRequested) {
            // Так делать нельзя, потому что ПЕРВОЕ прерывание будет не на await Task.Delay, а на await Wait
            // Соответсвенно тут токен может уже быть отмененным
            //throw new Exception("wrong logic. already Pulse"); 
            return true;
        }
        try {
            await Task.Delay(ts, token.Token);
        } catch (TaskCanceledException) {
            return true;
        }
        return false;
    }
    async public Task<bool> Wait(int ms) {
        return await Wait(TimeSpan.FromMilliseconds(ms));
    }
    public void Pulse() {
        //if (token != null)
        token.Cancel();
    }
}


public class AsyncMutex {
    AsyncLock _mutex = new AsyncLock();
    public AwaitableDisposable<IDisposable> Take() {
        return _mutex.LockAsync();
    }
    //public bool IsTaked() {
    //    _mutex.
    //}
}


/*
 * Не позволяет ждать освобождения, только проверку на то взято или нет.
 */
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

public class GlobalMutex : IDisposable {
    Mutex? mtx = null;
    public string Name { get; private set; }

    [MemberNotNullWhen(true, nameof(mtx))]
    public bool IsTaked {get; private set; }
    public GlobalMutex(string uniqeName) {
        Name = uniqeName;
    }
    public bool TryTake() {
        if (IsTaked)
            return true;
        if (mtx == null) {
            mtx = new Mutex(false, Name);
        }
        try {
            IsTaked = mtx.WaitOne(TimeSpan.Zero, true);
        } catch (AbandonedMutexException) {
            IsTaked = true;
        }
        return IsTaked;
    }
    public void Release() {
        if (!IsTaked)
            return;
        mtx.ReleaseMutex();
        IsTaked = false;
    }

    public void Dispose() {
        Release();
    }

    // For using(){} Create NEW independent mutex
    //public GlobalMutex TakeCloneOrExcept() { // Делается для испльзования в using
    //    GlobalMutex clone = new(Name);
    //    if (!clone.TryTake()) {
    //        throw new Exception($"can not take mutex {Name}");
    //    }
    //    return clone;
    //}
}
