using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall; 
public static class ProgramUtils {
    public static string TypeSession => IsDebug ? "DEBUG" : "RELEASE";

    static public bool IsRelease => !IsDebug;

    static public bool IsDebug {
        get {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    public static string BinFolder => Path.GetDirectoryName(ExePath);

    public static string ExePath => Environment.ProcessPath;

    static public string FindOurPath(string name) {
        var path = BinFolder;
        while (true) {
            var cur = Path.Combine(path, name);

            if (File.Exists(cur) || Directory.Exists(cur)) {
                return cur;
            }

            path = Path.GetDirectoryName(path);
            if (path == null) {
                throw new Exception("can't find " + name);
            }
        }
    }

    static bool? is_autostart;
    static public bool IsAutoStart {
        get {
            if (is_autostart.HasValue) return is_autostart.Value;
            is_autostart = HasArg("/autostart");
            return is_autostart.Value;
        }
    }
    static public bool HasArg(string cmd_arg) => Environment.GetCommandLineArgs().FirstOrDefault(x => x.Contains(cmd_arg)) != null;

    public static DateTime StartTime { get; private set; } = DateTime.UtcNow;


}
