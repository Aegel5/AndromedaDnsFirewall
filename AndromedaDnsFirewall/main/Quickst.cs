using AndromedaDnsFirewall.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.main
{
    internal class Quickst
    {

        public static Quickst Inst { get; private set; }
        static readonly string path = $"{ProgramUtils.BinFolder}/qstate.json";
        static public void Load()
        {
            if (!File.Exists(path))
            {
                Inst = new();
            }
            else
            {
                var cont = File.ReadAllText(path); // blocks this thread!
                Inst = JsonSerializer.Deserialize<Quickst>(cont);
            }
        }
        static public void Save()
        {
            var cont = JsonSerializer.Serialize(Inst);
            File.WriteAllText(path, cont);// blocks this thread!
        }

        public bool UseCache { get; set; } = false;

        public bool LogEnable { get; set; } = true;

        public WorkMode mode { get; set; } = WorkMode.AllExceptBlackList;

        static public WorkMode Mode => Inst.mode;

    }
}
