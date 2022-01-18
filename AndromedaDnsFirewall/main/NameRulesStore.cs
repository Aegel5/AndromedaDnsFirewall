using AndromedaDnsFirewall.Utils;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.main
{
    class NameRulesStore
    {
        public class Rec {
            [PrimaryKey]
            public string name { get; set; }
            public bool block { get; set; }
        }

        public static NameRulesStore Inst = new();

        SQLiteConnection conn;

        public void Init() {
            var path = $"{ProgramUtils.BinFolder}/rules.db";
            conn = new SQLiteConnection(path);
            conn.CreateTable<Rec>();
        }
        public void Update(string name, bool block) {
            conn.InsertOrReplace(new Rec { name = name, block = block });
        }
        public void Delete(string name) {
            conn.Delete(new Rec { name = name });
        }
        public IEnumerable<Rec> Load() {
            return conn.Table<Rec>();
        }
    }
}
