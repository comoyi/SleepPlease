using System.Collections.Generic;
using System.Timers;
using BepInEx.Logging;

namespace SleepPlease;

public class Server
{
    private static ManualLogSource Log;

    private static List<string> inBedPlayerInfos = new();
    private static List<string> notInBedPlayerInfos = new();

    private static readonly List<string> emptyStringList = new();

    public static void SetLog(ManualLogSource Log)
    {
        Server.Log = Log;
    }

    public static void ServerCheckSleep(object source, ElapsedEventArgs args)
    {
        if (!ZNet.instance.IsServer())
        {
            Log.LogWarning("Not Server but in ServerCheckSleep()");
            return;
        }

        bool isShowStatusPanel = false;

        bool isTimeCanSleep = EnvMan.instance.CanSleep();

        if (isTimeCanSleep)
        {
            int inBedCount = 0;

            inBedPlayerInfos.Clear();
            notInBedPlayerInfos.Clear();
            
            // // for test
            // inBedPlayerInfos.Add("玩家");
            // inBedPlayerInfos.Add("Player");
            // inBedPlayerInfos.Add("プレイヤー");
            // notInBedPlayerInfos.Add("玩家");
            // notInBedPlayerInfos.Add("Player");
            // notInBedPlayerInfos.Add("プレイヤー");

            List<ZDO> allCharacterZdos = ZNet.instance.GetAllCharacterZDOS();
            if (allCharacterZdos.Count > 0)
            {
                foreach (ZDO zdo in allCharacterZdos)
                {
                    if (zdo.GetBool("inBed"))
                    {
                        inBedCount++;
                        inBedPlayerInfos.Add(zdo.GetString("playerName", "-"));
                    }
                    else
                    {
                        notInBedPlayerInfos.Add(zdo.GetString("playerName", "-"));
                    }
                }
            }

            if (inBedCount > 0)
            {
                isShowStatusPanel = true;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC.RPCNameXSleepPlease, inBedPlayerInfos, notInBedPlayerInfos, isShowStatusPanel);
        }
        else
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC.RPCNameXSleepPlease, emptyStringList, emptyStringList, false);
        }
    }
}