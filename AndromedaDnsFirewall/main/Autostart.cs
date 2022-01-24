using AndromedaDnsFirewall.Utils;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall.main
{
    public class AutostartCheck {

        public static string GetAutostartExe() {
            using TaskService ts = new TaskService();
            var td = ts.RootFolder.Tasks.Where(x => x.Name == taskName).FirstOrDefault();
            if (td == null)
                return null;
            //var act = td.Definition.Actions[0];
            return td.Definition.Actions[0].ToString();
        }



        public static void Swap() {
            if(GetAutostartExe() == null) {
                SetCurAutostart();
            } else {
                RemoveAutostart();
            }
        }

        public async static System.Threading.Tasks.Task SwapCheck() {

            if (ProgramUtils.IsElevated) {
                Swap();
                return;
            }

            await System.Threading.Tasks.Task.Run(() => {
                var proc = new Process();
                proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
                proc.StartInfo.FileName = ProgramUtils.ExePath;
                proc.StartInfo.Arguments = "/swap_autostart";
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.Verb = "runas";

                if (!proc.Start()) {
                    Log.Info("process not started");
                    return;
                }

                proc.WaitForExit();
            });

            return;
        }

        static void RemoveAutostart() {
            using TaskService ts = new TaskService();
            ts.RootFolder.DeleteTask(taskName, false);
        }

        static string taskName = "AndromedaDnsFirewall_3fjka38a";

        static void SetCurAutostart() {
            using TaskService ts = new TaskService();
            var td = ts.NewTask();
            td.RegistrationInfo.Description = "AndromedaDnsFirewall autostart";
            td.Triggers.Add(new LogonTrigger() { });
            td.Actions.Add(new ExecAction(ProgramUtils.ExePath) { Arguments="/autostart"});
            td.Settings.ExecutionTimeLimit = TimeSpan.Zero;
            td.Settings.AllowDemandStart = true;
            //td.Principal.RunLevel = TaskRunLevel.Highest;
            td.Settings.DeleteExpiredTaskAfter = TimeSpan.Zero;
            td.Settings.StopIfGoingOnBatteries = false;
            td.Settings.DisallowStartIfOnBatteries = false;
            td.Settings.StartWhenAvailable = true;
            ts.RootFolder.DeleteTask(taskName, false);
            ts.RootFolder.RegisterTaskDefinition(taskName, td);
        }
    }
}
