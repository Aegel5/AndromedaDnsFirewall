using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.Utils
{
    static class ProgramUtils {
        public static string TypeSession => IsDebug ? "DEBUG" : "RELEASE";

        static string binfolder = null;
        static string myexe = null;

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

        static WindowsPrincipal princ;

        static public bool IsElevated {
            get {
                if(princ == null) {
                    princ = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                }
                return princ.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        public static string MyExe {
            get {
                if (myexe != null)
                    return myexe;

                var exe = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("file:///", "");
                myexe = System.IO.Path.GetFileName(exe);
                myexe = myexe.Replace(".dll", ".exe");
                return myexe;

                //var t = System.Reflection.Assembly.GetExecutingAssembly().l
            }
        }

        public static string BinFolder {
            get {
                if (binfolder != null)
                    return binfolder;

                var exe = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("file:///", "");
                binfolder = System.IO.Path.GetDirectoryName(exe);
                return binfolder;
            }
        }

        public static string ExePath {
            get {
                return $"{BinFolder}\\{MyExe}";
            }
        }
    }
}
