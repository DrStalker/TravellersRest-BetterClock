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
        private static ConfigEntry<int> _ticksToUpdate; //To avoid calling reflection every frame... doesn't really matter if the clock lags slightly a few frames behind the real time

        public static GameDate betterClockTime;
        public static int tick = 0;


        public Plugin()
        { 
            _debugLogging  = Config.Bind("Debug", "Debug Logging", false, "Logs additional information to console");
            _ticksToUpdate = Config.Bind("General", "Ticks Between Updates", 10, "only update the time data every X frames");

        }
        
        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
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
            if (++tick > _ticksToUpdate.Value)
            {
                tick -= _ticksToUpdate.Value;
                betterClockTime = Traverse.Create(__instance).Field("currentGameDate").GetValue<GameDate>();

            }

            


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




            int h = betterClockTime.hour;
            int m = betterClockTime.min - betterClockTime.min % 5;
            int w = betterClockTime.week;
            string weekday = betterClockTime.day.ToString(); // This skips localization, but getting it to use the localaization functions looks difficult
            int dayofmonth = (int)(betterClockTime.week * GameDate.DAY_IN_WEEK + betterClockTime.day + 1);  // Copied existing code, which accounts for weeks with a nuimber of days other than 7

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
