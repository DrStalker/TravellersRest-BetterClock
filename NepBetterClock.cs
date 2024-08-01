using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;


namespace BetterClock
{    public enum SpeedState { normal, fast, slow }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static Harmony _harmony;
        internal static ManualLogSource Log;

        private static ConfigEntry<bool> _debugLogging;
        private static ConfigEntry<int> _ticksToUpdate; //To avoid calling reflection every frame... doesn't really matter if the clock lags slightly a few frames behind the real time
        private readonly ConfigEntry<KeyCode> _speedHotKey;
        private static ConfigEntry<float> _speedMultSlow;
        private static ConfigEntry<float> _speedMultFast;
        public static GameDate betterClockTime;
        public static int tick = 0;

        
        public static SpeedState gameSpeed = SpeedState.normal;
        private static string gameSpeedText = "";

        WorldTime myWorldTime;
        public Plugin()
        {
            _debugLogging = Config.Bind("Debug", "Debug Logging", false, "Logs additional information to console");
            _ticksToUpdate = Config.Bind("General", "Ticks Between Updates", 10, "only update the time data every X frames");
            _speedHotKey = Config.Bind("Speed Control", "hotkey", KeyCode.F9, "Press to toggle between normal/fast/slow speeds");
            _speedMultSlow = Config.Bind("Speed Control", "Slow Speed", 0.2f, "Clock speed multiplier in Slow mode");
            _speedMultFast = Config.Bind("Speed Control", "Fast Speed", 5.0f, "Clock speed multiplier in Fast mode");
            myWorldTime = FindObjectOfType<WorldTime>();
            
        }

        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            gameSpeed = SpeedState.normal;
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
                Log.LogInfo(string.Format("BetterClock: {0}", message));

            }
        }
        private void Update()
        {
            if (Input.GetKeyDown(_speedHotKey.Value))
            {
                //This feels ugly instad of having a nice data structure to pull each setting from, but it works.
                if (gameSpeed == SpeedState.normal)
                {
                    gameSpeed = SpeedState.fast;
                    gameSpeedText = "+";
                    WorldTime.multiplierDevConsole =_speedMultFast.Value;
                    //MainUI.ShowErrorText(1, "Speed: Fast");
                }
                else if (gameSpeed == SpeedState.fast)
                {
                    gameSpeed = SpeedState.slow;
                    gameSpeedText = "-";
                    WorldTime.multiplierDevConsole = _speedMultSlow.Value;
                    //MainUI.ShowErrorText(1, "Speed: Slow");
                }
                else if (gameSpeed == SpeedState.slow)
                {
                    gameSpeed = SpeedState.normal;
                    gameSpeedText = "";
                    WorldTime.multiplierDevConsole = 1.0f;
                    //MainUI.ShowErrorText(1, "Speed: Normal");
                }
                else
                {
                    Log.LogError("If you see this message, something is very broken. BC-Update()-speedHotKey");
                }


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
            string weekday = betterClockTime.day.ToString().Substring(0,2); // This skips localization, but getting it to use the localaization functions looks difficult
            int dayofmonth = (int)(betterClockTime.week * GameDate.DAY_IN_WEEK + betterClockTime.day + 1);  // Copied existing code, which accounts for weeks with a nuimber of days other than 7

            string hx = h < 10 ? "0" + h.ToString() : "" + h.ToString();
            string mx = m < 10 ? "0" + m.ToString() : "" + m.ToString();



            if (reflectedText != null)
            {
                reflectedText.text = string.Format("{0}:{1} {2} {3}{4}", hx, mx, weekday, dayofmonth, gameSpeedText);

                reflectedText.maxVisibleCharacters = reflectedText.text.Length;
            }

        }



    }
}

