using System.Collections.Generic;
using BepInEx.Logging;

namespace SleepPlease;

public class RPC
{
    private static ManualLogSource Log;

    public const string RPCNameXSleepPlease = "RPC_XSleepPleaseS";

    public static void SetLog(ManualLogSource Log)
    {
        RPC.Log = Log;
    }

    public static void RPC_XSleepPleaseS(long sender, List<string> inBedPlayers, List<string> notInBedPlayers, bool isShowStatusPanel)
    {
        SleepPlease.InBedPlayerInfos = inBedPlayers;
        SleepPlease.NotInBedPlayerInfos = notInBedPlayers;
        SleepPlease.IsShowStatusPanel = isShowStatusPanel;

        // string msg = "";
        // msg += "in: ";
        // foreach (var player in inBedPlayers)
        // {
        //     msg += $"{player},";
        // }
        //
        // msg += ";notin: ";
        // foreach (var player in notInBedPlayers)
        // {
        //     msg += $"{player},";
        // }
        //
        // Log.LogDebug($"{RPCNameXSleepPlease}, isShowStatusPanel: {isShowStatusPanel}, msg: {msg}");
    }
}