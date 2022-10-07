using System;
using System.Collections.Generic;
using System.Timers;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SleepPlease;

[BepInPlugin(pluginGUID, pluginName, pluginVersion)]
public class SleepPlease : BaseUnityPlugin
{
    private const string pluginGUID = "com.comoyi.valheim.SleepPlease";
    private const string pluginName = "SleepPlease";
    private const string pluginVersion = "1.0.3";

    private readonly Harmony harmony = new Harmony(pluginGUID);

    private static ManualLogSource Log;

    private ConfigEntry<int> configPositionX;
    private ConfigEntry<int> configPositionY;

    private ConfigEntry<int> configMaxDailyTipTimes;
    private ConfigEntry<string> configSleepTipText;
    private ConfigEntry<int> configCanSleepMaxDailyTipTimes;
    private ConfigEntry<string> configCanSleepTipText;

    private static int dailyTippedTimes = 0;

    private static int dailyMaxTipTimes = 3;

    private static int positionX = 0;
    private static int positionY = 0;

    private static string sleepTipText = "天黑了，睡觉啦！";
    private static int canSleepDailyTippedTimes = 0;
    private static int canSleepDailyMaxTipTimes = 1;
    private static string canSleepTipText = "天黑了！";

    private GUIStyle guiStyle;
    private static bool isShowInBed = false;

    private static Texture2D sleepIcon;
    private static List<Player> inBedPlayers = new List<Player>();
    private static List<Player> notInBedPlayers = new List<Player>();

    private void Awake()
    {
        Log = Logger;

        configPositionX = Config.Bind("UI", "X", 50, "Position X");
        positionX = configPositionX.Value;
        configPositionY = Config.Bind("UI", "Y", 500, "Position Y");
        positionY = configPositionY.Value;

        configMaxDailyTipTimes = Config.Bind("Tip", "MaxDailyTipTimes", 3, "Show sleep tip max times per day");
        dailyMaxTipTimes = configMaxDailyTipTimes.Value;
        configSleepTipText = Config.Bind("Tip", "SleepTipText", "天黑了，睡觉啦！", "Sleep tip text");
        sleepTipText = configSleepTipText.Value;

        configCanSleepMaxDailyTipTimes =
            Config.Bind("Tip", "CanSleepMaxDailyTipTimes", 1, "Show sleep tip max times per day");
        canSleepDailyMaxTipTimes = configCanSleepMaxDailyTipTimes.Value;
        configCanSleepTipText = Config.Bind("Tip", "CanSleepTipText", "可以睡觉了！", "Can sleep tip text");
        canSleepTipText = configCanSleepTipText.Value;

        guiStyle = new GUIStyle();
        guiStyle.normal.textColor = Color.yellow;
        guiStyle.fontSize = 20;
        Font f = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (f != null)
        {
            guiStyle.font = f;
        }

        sleepIcon = GetSleepIcon();

        harmony.PatchAll();
        Log.LogDebug("loaded");
    }


    private void OnGUI()
    {
        if (isShowInBed)
        {
            // int offsetX = 0;
            int offsetY = 0;

            try
            {
                foreach (Player p in inBedPlayers)
                {
                    GUI.Label(new Rect(positionX - 35, positionY + offsetY, 32, 32), sleepIcon);
                    GUI.Label(new Rect(positionX, positionY + offsetY, 200, 400), $"{p.GetPlayerName()}", guiStyle);
                    offsetY += 20;
                }

                foreach (Player p in notInBedPlayers)
                {
                    GUI.Label(new Rect(positionX, positionY + offsetY, 200, 400), $"{p.GetPlayerName()}", guiStyle);
                    offsetY += 20;
                }
            }
            catch (Exception)
            {
                return;
            }
        }
    }

    [HarmonyPatch(typeof(Game), "Awake")]
    private class CheckSleepPatch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Enabled = true;
            timer.Interval = 3000;
            timer.Start();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(CheckSleep);
        }
    }


    [HarmonyPatch(typeof(Game), "SleepStop")]
    private class ResetDailyTippedTimesPatch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            Log.LogDebug("Reset dailyTippedTimes");
            dailyTippedTimes = 0;
            canSleepDailyTippedTimes = 0;
        }
    }

    [HarmonyPatch(typeof(Player), "SetSleeping")]
    private class ResetDailyTippedTimes
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            Log.LogDebug("Reset dailyTippedTimes");
            dailyTippedTimes = 0;
            canSleepDailyTippedTimes = 0;
        }
    }

    private static void CheckSleep(object source, ElapsedEventArgs args)
    {
        isShowInBed = false;

        // Log.LogDebug("1");
        Player player = Player.m_localPlayer;
        if (!(bool)(UnityEngine.Object)player)
        {
            return;
        }

        // Log.LogDebug("2");
        if (player.IsSleeping())
        {
            return;
        }

        // Log.LogDebug("3");

        bool isTimeCanSleep = EnvMan.instance.CanSleep();

        if (isTimeCanSleep)
        {
            // for test
            // isShowInBed = true;

            canSleepDailyTippedTimes++;
            if (canSleepDailyTippedTimes <= canSleepDailyMaxTipTimes)
            {
                // Log.LogDebug("4 can sleep now");
                MessageHud.instance.ShowBiomeFoundMsg(canSleepTipText, false);
            }

            // Log.LogDebug("5");

            int inBed = 0;
            int total = 0;
            List<Player> players = Player.GetAllPlayers();
            inBedPlayers.Clear();
            notInBedPlayers.Clear();

            foreach (Player p in players)
            {
                total++;
                if (p.InBed())
                {
                    inBed++;
                    inBedPlayers.Add(p);
                }
                else
                {
                    notInBedPlayers.Add(p);
                }
            }


            if (inBed > 0)
            {
                isShowInBed = true;
            }

            if (inBed > 0 && inBed != total)
            {
                // Log.LogDebug("6");
                dailyTippedTimes++;
                if (dailyTippedTimes <= dailyMaxTipTimes)
                {
                    Log.LogDebug("show message center");
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{sleepTipText} {inBed}/{total} 已躺好", 0);
                }
                else
                {
                    // Log.LogDebug("7");
                }
            }
        }
    }

    private Texture2D GetSleepIcon()
    {
        string relativeFilePath = "BepInEx\\plugins\\SleepPlease\\sleep.png";
        Texture2D tex = LoadTexture(relativeFilePath);
        if (tex == null)
        {
            Log.LogInfo("Loaded default sleep icon");
            tex = GetDefaultSleepIcon();
        }
        else
        {
            Log.LogInfo($"Loaded sleep icon from {relativeFilePath}");
        }

        return tex;
    }

    private Texture2D GetDefaultSleepIcon()
    {
        Texture2D tex = new Texture2D(10, 10);
        byte[] pngBytes = new byte[]
        {
            137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 64, 0, 0, 0, 38, 8, 6, 0, 0, 0, 116,
            196, 157, 153, 0, 0, 0, 1, 115, 82, 71, 66, 0, 174, 206, 28, 233, 0, 0, 2, 82, 73, 68, 65, 84, 104, 67, 237,
            88, 61, 79, 194, 80, 20, 61, 132, 133, 144, 70, 221, 234, 162, 12, 202, 10, 63, 64, 13, 12, 70, 7, 255, 1,
            38, 178, 234, 15, 96, 96, 34, 76, 36, 42, 171, 186, 98, 162, 163, 19, 154, 104, 194, 4, 186, 235, 138, 11,
            113, 145, 205, 161, 33, 44, 4, 243, 42, 66, 63, 160, 237, 123, 189, 216, 86, 218, 164, 73, 147, 222, 123,
            223, 59, 167, 231, 126, 188, 70, 176, 224, 87, 132, 26, 127, 169, 84, 26, 82, 199, 44, 151, 203, 228, 251,
            252, 221, 35, 105, 224, 98, 177, 56, 172, 84, 42, 212, 248, 81, 171, 213, 144, 207, 231, 73, 247, 26, 18,
            48, 98, 128, 148, 213, 80, 1, 218, 20, 72, 239, 0, 213, 123, 241, 116, 216, 93, 26, 251, 6, 51, 5, 66, 2,
            66, 5, 132, 41, 176, 216, 53, 192, 174, 252, 109, 164, 128, 106, 29, 144, 86, 204, 150, 79, 183, 192, 233,
            177, 93, 4, 238, 247, 178, 44, 163, 219, 237, 234, 58, 159, 55, 109, 208, 3, 240, 140, 45, 127, 16, 224, 17,
            120, 110, 2, 68, 102, 250, 70, 163, 129, 86, 171, 53, 91, 154, 162, 224, 247, 15, 1, 121, 157, 91, 242, 170,
            195, 107, 11, 120, 109, 170, 143, 188, 10, 160, 61, 212, 136, 130, 103, 59, 175, 62, 0, 233, 109, 49, 2,
            174, 43, 0, 187, 61, 37, 192, 13, 120, 95, 16, 176, 154, 0, 228, 53, 251, 47, 208, 253, 0, 62, 59, 122, 59,
            183, 224, 125, 65, 192, 81, 17, 96, 183, 221, 165, 145, 155, 106, 74, 1, 62, 176, 4, 48, 213, 92, 53, 167,
            247, 121, 229, 11, 184, 187, 180, 166, 115, 148, 183, 170, 145, 93, 17, 100, 68, 111, 29, 76, 143, 87, 202,
            1, 207, 117, 151, 53, 192, 169, 2, 52, 139, 129, 240, 48, 100, 201, 148, 149, 202, 206, 78, 128, 199, 155,
            177, 123, 34, 145, 64, 167, 211, 113, 60, 8, 77, 186, 128, 90, 3, 70, 109, 72, 90, 6, 10, 23, 230, 47, 107,
            88, 236, 79, 8, 224, 0, 47, 73, 18, 20, 69, 49, 13, 126, 86, 147, 160, 185, 13, 178, 177, 245, 188, 14, 108,
            166, 244, 31, 197, 8, 158, 189, 157, 183, 2, 172, 192, 27, 106, 81, 60, 30, 71, 175, 215, 155, 138, 213, 57,
            1, 60, 224, 231, 77, 0, 71, 113, 141, 197, 98, 232, 247, 251, 51, 113, 58, 35, 96, 22, 248, 247, 55, 224,
            69, 243, 215, 71, 51, 117, 129, 165, 205, 94, 206, 174, 111, 204, 126, 175, 45, 130, 90, 43, 14, 240, 204,
            45, 155, 205, 34, 147, 201, 168, 17, 88, 26, 20, 10, 5, 129, 26, 224, 180, 8, 26, 219, 160, 56, 252, 233,
            158, 156, 224, 141, 65, 196, 71, 97, 63, 16, 224, 18, 188, 187, 81, 216, 107, 2, 8, 192, 187, 35, 64, 64,
            202, 201, 100, 18, 237, 118, 91, 248, 127, 67, 52, 26, 29, 14, 6, 131, 159, 149, 131, 120, 24, 10, 9, 248,
            7, 10, 16, 16, 62, 169, 203, 100, 16, 243, 40, 5, 72, 209, 240, 6, 243, 67, 13, 224, 221, 51, 169, 189, 142,
            0, 162, 200, 188, 115, 0, 209, 178, 98, 97, 66, 2, 180, 109, 80, 140, 67, 147, 87, 160, 20, 64, 132, 217,
            54, 140, 240, 160, 98, 27, 57, 32, 6, 11, 79, 192, 55, 15, 13, 134, 54, 37, 29, 11, 217, 0, 0, 0, 0, 73, 69,
            78, 68, 174, 66, 96, 130,
        };
        tex.LoadImage(pngBytes);
        return tex;
    }

    private static Texture2D LoadTexture(string relativeFilePath)
    {
        // Log.LogDebug($"rel:{relativeFilePath}");
        string filePath = System.IO.Path.GetFullPath(relativeFilePath);
        // Log.LogDebug($"abs:{filePath}");
        Texture2D tex2D;
        byte[] fileData;

        if (System.IO.File.Exists(filePath))
        {
            try
            {
                fileData = System.IO.File.ReadAllBytes(filePath);
            }
            catch (Exception e)
            {
                Log.LogInfo("" + e.Message);
                return null;
            }

            tex2D = new Texture2D(10, 10);
            if (tex2D.LoadImage(fileData))
            {
                return tex2D;
            }
        }

        return null;
    }
}
