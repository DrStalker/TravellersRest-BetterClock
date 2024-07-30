using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace BetterClock
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static Harmony _harmony;
        internal static ManualLogSource Log;
        
        private static ConfigEntry<bool> _debugLogging;

        public static GameDate reflectedCurrentTime;

        public Plugin()
        { 
            _debugLogging = Config.Bind("Debug", "Debug Logging", false, "Logs additional information to console"); 


        }
        
        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            //How to find the "Worldtime" object?
            //Unity Scene DontDestroyOnLoad contains object "Management" which contain component "WorldTime" but I can't figure out how to access that via C#
            //reflectedCurrentTime = Traverse.Create(WorldTime).Field("currentGameDate").GetValue<GameDate>();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        
        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
        
        public static void DebugLog(string message)
        {
            //if (_debugLogging.Value)
            if (_debugLogging.Value)
            {
                Log.LogInfo(String.Format("BetterClock: {0}", message));
                
            }
        }

        [HarmonyPatch(typeof(WorldTime), "Update")]
        [HarmonyPostfix]
        static void WorldTimeUpdatePostfix(WorldTime __instance)
        {
            //DebugLog("WorldTimeUpdatePostfix Postfix");
            reflectedCurrentTime = Traverse.Create(__instance).Field("currentGameDate").GetValue<GameDate>();


        }

        [HarmonyPatch(typeof(TimeUI), "Update")]
        [HarmonyPostfix]
        static void TimeUIUpdatePostfix(TimeUI __instance)
        {
            
            
            TextMeshProUGUI reflectedText = Traverse.Create(__instance).Field("showingTextMesh").GetValue<TextMeshProUGUI>();

            // How to acccess the game time without relying on randomly names objects?
            // WorldTime.Update -> WorldTime.RANDOM -> TimeUI.set_CurrentTime(GameDate)) -> sets TimeUI.Random1 -> TimeUI.Update() sets TimeUI.Random1= WorldTime.ClockToNearest5min(this.Random1);
            // So attach to WorldTime.Update and set a value to current time
            // Or! reflection to find <GameDate>WorldTime.currentGameDate as Plugin.reflectedTime



            GameDate timeNow = reflectedCurrentTime;
            int h = timeNow.hour;
            int m = timeNow.min - timeNow.min % 5;
            int w = timeNow.week;
            string weekday = timeNow.day.ToString();
            int dayofmonth = (int)(timeNow.week * GameDate.DAY_IN_WEEK + timeNow.day + 1);  // Copied existing code, which accounts for weeks with a nuimber of days other than 7

            string hx = h < 10 ? "0" + h.ToString() : "" + h.ToString();
            string mx = m < 10 ? "0" + m.ToString() : "" + m.ToString();



            if (reflectedText != null)
            {
                reflectedText.text = String.Format("{0}:{1} {2} {3}", hx, mx, weekday, dayofmonth);

                reflectedText.maxVisibleCharacters = reflectedText.text.Length;
            }

        }



    }
}
