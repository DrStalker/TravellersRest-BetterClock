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
        private readonly ConfigEntry<KeyCode> _pauseHotKey;
        private static ConfigEntry<float> _speedMultSlow;
        private static ConfigEntry<float> _speedMultFast;
        private static ConfigEntry<bool> _ThreeCharDays;
        public static GameDate betterClockTime;
        public static int tick = 0;
        public static int tickGUI = 0;
        public static bool paused = false;
        public static string pausedText = "PAUSED";

        public static SpeedState gameSpeed = SpeedState.normal;
        private static string gameSpeedText = "";

        private static TextMeshProUGUI reflectedClockText = null;

        WorldTime myWorldTime;
        public Plugin()
        {
            _debugLogging = Config.Bind("Debug", "Debug Logging", false, "Logs additional information to console");
            _ticksToUpdate = Config.Bind("General", "Ticks Between Updates", 10, "only update the time data every X frames");

            _speedHotKey = Config.Bind("Speed Control", "hotkey", KeyCode.F9, "Press to toggle between normal/fast/slow speeds");
            _pauseHotKey = Config.Bind("Speed Control", "pause key", KeyCode.None, "Press to toggle between paused/unpaused");
            _speedMultSlow = Config.Bind("Speed Control", "Slow Speed", 0.2f, "Clock speed multiplier in Slow mode");
            _speedMultFast = Config.Bind("Speed Control", "Fast Speed", 5.0f, "Clock speed multiplier in Fast mode");
            _ThreeCharDays = Config.Bind("General", "Three Character Days", false, "Shows 'Wed' instead of 'We' etc.");
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


        private void SetWorldSpeed()
        {
            float newSpeed = 1.0f;
            if (paused)
            {
                newSpeed = 0.0f;
            }
            //This feels ugly instad of having a nice data structure to pull each setting from, but it works.
            else if (gameSpeed == SpeedState.normal)
            {
                newSpeed = 1.0f;
                gameSpeedText = "";
            }
            else if (gameSpeed == SpeedState.fast)
            { 
                newSpeed = _speedMultFast.Value;
                gameSpeedText = "+";
            }
            else if (gameSpeed == SpeedState.slow)
            {
                newSpeed = _speedMultSlow.Value;
                gameSpeedText = "-";
            }
            else
            {
                Log.LogError("SetWorldSpeed(): If you see this message, something is very broken.");
                return;
            }
            WorldTime.multiplierDevConsole = newSpeed;
        }


        private void Update()
        {
            if (Input.GetKeyDown(_speedHotKey.Value))
            {

                if (gameSpeed == SpeedState.normal) gameSpeed = SpeedState.fast;
                else if (gameSpeed == SpeedState.fast) gameSpeed = SpeedState.slow;
                else if (gameSpeed == SpeedState.slow) gameSpeed = SpeedState.normal;
                else
                {
                    Log.LogError("Update(): If you see this message, something is very broken.");
                    return;
                }
                SetWorldSpeed();
            }
            if (Input.GetKeyDown(_pauseHotKey.Value))
            {
                paused = !paused;
                SetWorldSpeed();
            }
        }


        [HarmonyPatch(typeof(WorldTime), "Update")]
        [HarmonyPostfix]
        static void WorldTimeUpdatePostfix(WorldTime __instance)
        {
            if (++tick > _ticksToUpdate.Value)
            {
                tick = 0;
                betterClockTime = Traverse.Create(__instance).Field("currentGameDate").GetValue<GameDate>();

            }

        }

        [HarmonyPatch(typeof(TimeUI), "Update")]
        [HarmonyPostfix]
        static void TimeUIUpdatePostfix(TimeUI __instance)
        {
            if (reflectedClockText is null)
            {
                DebugLog($"Hunting for Clock text object...");
                reflectedClockText = Traverse.Create(__instance).Field("showingTextMesh").GetValue<TextMeshProUGUI>();
                if (reflectedClockText is null)
                {
                    DebugLog($"TimeUIUpdatePostfix: ERROR: unable to find clock text object");
                    return;
                }
                else
                {
                    DebugLog($"TimeUIUpdatePostfix(): Found clock text object");
                }
            }
            

            string clocktext = "";

            if (!paused)
            {
                int h = betterClockTime.hour;
                int m = betterClockTime.min - betterClockTime.min % 5;
                int w = betterClockTime.week;
                int dayLength = _ThreeCharDays.Value ? 3 : 2;
                string weekday = betterClockTime.day.ToString().Substring(0, dayLength); // This skips localization, but getting it to use the localization functions looks difficult
                int dayofmonth = (int)(betterClockTime.week * GameDate.DAY_IN_WEEK + betterClockTime.day + 1);  // Copied existing code, which accounts for weeks with a nuimber of days other than 7

                string hx = h < 10 ? "0" + h.ToString() : "" + h.ToString();
                string mx = m < 10 ? "0" + m.ToString() : "" + m.ToString();
                clocktext = $"{hx}:{mx} {weekday} {dayofmonth}{gameSpeedText}";
            }
            else
            {
                clocktext = $"{pausedText}";
            }


            //After all that, set the clock text
            reflectedClockText.text = clocktext;
            reflectedClockText.maxVisibleCharacters = reflectedClockText.text.Length;


        }



    }
}

