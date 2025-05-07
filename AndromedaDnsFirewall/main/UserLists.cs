using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;
internal enum RuleBlockType {
	Null = 0,
	Block,
	Allow
}
internal class UserLists {

	public static UserLists Inst = new();

	public static void Save() {

	}
	public static void Load() {
	}	

	public Dictionary<string, RuleBlockType> list = new();

}
