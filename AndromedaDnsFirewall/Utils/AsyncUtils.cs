using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.Utils
{
    public class AWaitVariable2 {

        AsyncLock mtx = new();
        AsyncConditionVariable cv;
        public AWaitVariable2() {
            cv = new(mtx);
        }

        async public Task Wait(CancellationToken token) {
            using (await mtx.LockAsync()) {
                await cv.WaitAsync(token); // TODO wait ts
            }
            //return true;
        }

        async public Task Pulse() {
            using (await mtx.LockAsync()) {
                cv.NotifyAll();
            }
        }
    }
}
