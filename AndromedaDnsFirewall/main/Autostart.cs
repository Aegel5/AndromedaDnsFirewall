using AndromedaDnsFirewall.Utils;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
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
            return td.Definition.Actions[0].ToString();
        }

        public static void RemoveAutostart() {
            using TaskService ts = new TaskService();
            ts.RootFolder.DeleteTask(taskName, false);
        }

        static string taskName = "AndromedaDnsFirewall_3fjka38a";

        public static void SetCurAutostart() {
            using TaskService ts = new TaskService();
            var td = ts.NewTask();
            td.RegistrationInfo.Description = "AndromedaDnsFirewall autostart";
            td.Triggers.Add(new LogonTrigger() { });
            td.Actions.Add(new ExecAction(ProgramUtils.ExePath));
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
