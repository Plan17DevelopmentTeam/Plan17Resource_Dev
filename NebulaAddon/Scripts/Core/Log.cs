using System;
using System.IO;
using BepInEx;
using Nebula;
using UnityEngine;

namespace Plana.Core;

public class PDebug
{
    static PDebug()
    {
        ClearLog();
    }
    public static void Log(object message)
    {
        string m = $"{DateTime.Now:[yyyy/MM/dd HH:mm:ss]} " + message.ToString() + Environment.NewLine;
        if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/LOG"))
        {
            File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/LOG/log.txt", m);
        }
        File.AppendAllText(new DirectoryInfo(Application.dataPath).Parent+ "/MoreRolesLog.txt", m);
    }
    private static void ClearLog()
    {
        string desktopLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LOG/log.txt");
        if (File.Exists(desktopLogPath))
        {
            File.WriteAllText(desktopLogPath, string.Empty);
        }
        string projectLogPath = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.FullName, "MoreRolesLog.txt");
        if (File.Exists(projectLogPath))
        {
            File.WriteAllText(projectLogPath, string.Empty);
        }
    }
}
