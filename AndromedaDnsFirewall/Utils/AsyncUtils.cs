using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.Utils
{
    public class AsyncCounter
    {
        public class ACounter_Holder : IDisposable
        {
            AsyncCounter data;

            public ACounter_Holder(AsyncCounter data)
            {
                this.data = data;
                data.Add();
            }

            public void Dispose()
            {
                data.Sub();
            }
        }
        public IDisposable TakeChecked()
        {
            if (IsTaked)
            {
                throw new Exception("Already taked");
            }
            return new ACounter_Holder(this);
        }
        public IDisposable Take()
        {
            return new ACounter_Holder(this);
        }
        public bool IsTaked => cnt != 0;
        public int cnt { get; private set; }

        void Add()
        {
            cnt++;
        }
        void Sub()
        {
            cnt--;
        }
    }
}
