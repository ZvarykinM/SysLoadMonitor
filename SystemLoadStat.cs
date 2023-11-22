using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CPUandMemoTest;

class ProcessGetter
{
    public static List<string> GetProcessInfo()
    {
            var AllProcessesInfo = new List<string>();
            foreach(var p in Process.GetProcesses())
                AllProcessesInfo.Add($"{p.BasePriority};{p.ProcessName};{p.Id};{p.PeakWorkingSet64}");
            AllProcessesInfo.Sort((x, y) => (Convert.ToInt64(x.Split(';').Last()) >= Convert.ToInt64(y.Split(';').Last()))?1:0);
            return AllProcessesInfo;
    }
}
class UnixSystemMonitoringVirtualDevice
{
    private string[] CPUInfoFieldNames = ["user", "nice", "system", "idle"];

    private string[] MemInfoFieldNames = ["Total", "Free", "Available"];

    public string CPUInfoString{get; private set;}

    public string MemInfoString{get; private set;}

    public double[] Renew => [PrintCPUUsageInfo(), PrintMemoryUsageInfo()];

    private double PrintCPUUsageInfo()
    {
        var AllStringsFromProcStat = File.ReadAllLines("/proc/stat");
        var CPUStatInfo = new Dictionary<string, int>();
        var FirstStringFields = AllStringsFromProcStat[0][5..].Split('\u0020');
        for(var i = 0; i < 4; i++)
            CPUStatInfo.Add(CPUInfoFieldNames[i], Convert.ToInt32(FirstStringFields[i]));

        var InfoString = "";
        foreach(var item in CPUStatInfo)
            InfoString += $"\t{item.Key}-time: \t{item.Value}\n";
        CPUInfoString = InfoString;

        return CalculateCPUPercentage(CPUStatInfo);
    }

    private double PrintMemoryUsageInfo()
    {
        var AllStringsFromMemoStat = File.ReadAllLines("/proc/meminfo");
        var MemoryStatInfo = new Dictionary<string, int>();
        for(var i = 0; i < 3; i++)
            MemoryStatInfo.Add(MemInfoFieldNames[i], GetOnlyDigitsFromString(AllStringsFromMemoStat[i]));

        var InfoString = "";
        foreach(var item in MemoryStatInfo)
            InfoString += $"\t{item.Key}-time: \t{item.Value}\n";

        return Convert.ToDouble(MemoryStatInfo["Free"]) / Convert.ToDouble(MemoryStatInfo["Total"]);
    }

    static int GetOnlyDigitsFromString(string str) => Convert.ToInt32(Regex.Replace(str, @"[^\d]", ""));

    private static double CalculateCPUPercentage(Dictionary<string, int> CPUStatInfo)
    {
        var values = CPUStatInfo.Values;
        var total = values.Sum();
        var Enumerator = total - values.ElementAt(3);
        return Convert.ToDouble(Enumerator) / Convert.ToDouble(total);
    }
}