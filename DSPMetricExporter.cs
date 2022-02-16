using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using BepInEx.Configuration;
//using Prometheus;
using System;

namespace DSPMetricExporter
{
    // FIXME: This PluginInfo won't work and I have no idea why
    //[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInPlugin("com.46bit.dsp-metric-exporter-plugin", "DSP Metric Exporter Plugin", "0.0.0")]
    public class DSPMetricExporterPlugin : BaseUnityPlugin
    {
        private static DSPMetricExporterPlugin instance;

        //private static readonly Counter Updates = Metrics.CreateCounter("updates", "Counter of game ticks");

        //private MetricServer prometheusServer;
        private Prom prom;

        #region Unity Core Methods
        void Awake()
        {
            instance = this;
            //Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Logger.LogInfo("Metric Exporter Plugin: loaded");
            
            prom = new Prom();
            Logger.LogInfo("Metric Exporter Plugin: prometheus endpoint started");

            // FIXME: This PluginInfo won't work and I have no idea why
            //Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            Harmony harmony = new Harmony("com.46bit.dsp-metric-exporter-plugin");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        void Update()
        {
            prom.Update();
            // Updates.Inc();
        }
        #endregion

        // #region Harmony Patch Hooks in DSP
        // [HarmonyPatch(typeof(GameData), "GameTick")]
        // class GameData_GameTick
        // {
        //     public static void Postfix(long time, GameData __instance)
        //     {
        //         instance.Logger.LogInfo("Metric Exporter Plugin: GameTick.Postfix");
        //     }
        // }
        // #endregion
    }
}
