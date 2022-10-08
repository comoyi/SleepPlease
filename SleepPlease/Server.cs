using System.Collections.Generic;
using System.Timers;
using BepInEx.Logging;

namespace SleepPlease;

public class Server
{
    private static ManualLogSource Log;

    private static List<string> inBedPlayerInfos = new();
    private static List<string> notInBedPlayerInfos = new();

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

        Log.LogDebug("[Server] 1");

        bool isShowStatusPanel = false;

        bool isTimeCanSleep = EnvMan.instance.CanSleep();

        if (isTimeCanSleep)
        {
            Log.LogDebug("[Server] 2 can sleep now");

            int inBedCount = 0;
            List<Player> players = Player.GetAllPlayers();

            inBedPlayerInfos.Clear();
            notInBedPlayerInfos.Clear();
            foreach (Player p in players)
            {
                if (p.InBed())
                {
                    inBedCount++;
                    inBedPlayerInfos.Add(p.GetPlayerName());
                }
                else
                {
                    notInBedPlayerInfos.Add(p.GetPlayerName());
                }
            }

            if (inBedCount > 0)
            {
                isShowStatusPanel = true;
            }

            // for test
            // isShowStatusPanel = true;
            // inBedPlayerInfos.Add("fake-in-X");
            // inBedPlayerInfos.Add("fake-in-Y");
            // notInBedPlayerInfos.Add("fake-notin-1");
            // notInBedPlayerInfos.Add("fake-notin-2");

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC.RPCNameXSleepPlease, inBedPlayerInfos, notInBedPlayerInfos, isShowStatusPanel);
        }
    }
}