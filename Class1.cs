using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
using UnityEngine;

[module: UnverifiableCode]
#pragma warning disable 618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore 618
namespace GigaStations
{
    [BepInDependency(LDB_TOOL_GUID)]
    [BepInDependency(WARPERS_MOD_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInPlugin(MODGUID, MODNAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]

    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(UtilSystem))]
    public class GigaStationsPlugin : BaseUnityPlugin
    {

        public const string MODGUID = "org.kremnev8.plugin.GigaStationsUpdated";
        public const string MODNAME = "GigaStationsUpdated";
        public const string VERSION = "2.2.7";

        public const string LDB_TOOL_GUID = "me.xiaoye97.plugin.Dyson.LDBTool";
        public const string WARPERS_MOD_GUID = "ShadowAngel.DSP.DistributeSpaceWarper";



        public static ManualLogSource logger;
        public static int gridXCount { get; set; } = 1;
        public static int gridYCount { get; set; } = 12;

        public static Color stationColor { get; set; } = new Color(0.3726f, 0.8f, 1f, 1f);

        //ILS
        public static int ilsMaxStorage { get; set; } = 30000; //Vanilla 10000
        public static int ilsMaxWarps { get; set; } = 100; //Vanilla 50
        public static int ilsMaxVessels { get; set; } = 30; //Vanilla 10 (limit from 10-30)
        public static int ilsMaxDrones { get; set; } = 150; //Vanilla 100
        public static long ilsMaxAcuGJ { get; set; } = 50; //Vanilla 12 GJ = * 1 000 000 000
        public static int ilsMaxSlots { get; set; } = 12; //Vanilla 5 (limited to from 5-12)


        public static ItemProto collector;

        public static ModelProto collectorModel;

        public static ResourceData resource;

        void Awake()
        {
            logger = Logger;

            string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            resource = new ResourceData(MODNAME, "gigastations", pluginfolder);
            resource.LoadAssetBundle("gigastations");

            ProtoRegistry.AddResource(resource);


            //General
            gridXCount = Config.Bind("-|0|- General", "-| 1 Grid X Max. Count", 1, new ConfigDescription("Amount of slots visible horizontally.\nIf this value is bigger than 1, layout will form a grid", new AcceptableValueRange<int>(1, 3))).Value;
            gridYCount = Config.Bind("-|0|- General", "-| 2 Grid Y Max. Count", 5, new ConfigDescription("Amount of slots visible vertically", new AcceptableValueRange<int>(3, 12))).Value;
            stationColor = Config.Bind("-|0|- General", "-| 3 Station Color", new Color(0.3726f, 0.8f, 1f, 1f), "Color tint of giga stations").Value;

            //ILS
            ilsMaxSlots = Config.Bind("-|1|- ILS", "-| 1 Max. Item Slots", 12, new ConfigDescription("The maximum Item Slots the Station can have.\nVanilla: 5", new AcceptableValueRange<int>(5, 12))).Value;
            ilsMaxStorage = Config.Bind("-|1|- ILS", "-| 2 Max. Storage", 30000, "The maximum Storage capacity per Item-Slot.\nVanilla: 10000").Value;
            ilsMaxVessels = Config.Bind("-|1|- ILS", "-| 3 Max. Vessels", 30, new ConfigDescription("The maximum Logistic Vessels amount.\nVanilla: 10", new AcceptableValueRange<int>(10, 30))).Value;
            ilsMaxDrones = Config.Bind("-|1|- ILS", "-| 4 Max. Drones", 150, new ConfigDescription("The maximum Logistic Drones amount.\nVanilla: 50", new AcceptableValueRange<int>(50, 150))).Value;
            ilsMaxAcuGJ = Config.Bind("-|1|- ILS", "-| 5 Max. Accu Capacity (GJ)", 50, "The Stations maximum Accumulator Capacity in GJ.\nVanilla: 12 GJ").Value;
            ilsMaxWarps = Config.Bind("-|1|- ILS", "-| 6 Max. Warps", 150, "The maximum Warp Cells amount.\nVanilla: 50").Value;


            ProtoRegistry.RegisterString("FactorySpaceStationPlaneFilter_Name", "Plane Filters");
            ProtoRegistry.RegisterString("FactorySpaceStationPlaneFilter_Desc", "Space station near a gas giant producing Plane Filters.");

            ProtoRegistry.RegisterString("ModificationWarn", "  - [GigaStationsUpdated] Replaced {0} buildings");

            ProtoRegistry.RegisterString("CantDowngradeWarn", "Downgrading logistic station is not possible!");


            collector = ProtoRegistry.RegisterItem(2112, "FactorySpaceStationPlaneFilter_Name", "FactorySpaceStationPlaneFilter_Desc", "assets/gigastations/icon_collector", 2703);
            collector.BuildInGas = true;

            //ProtoRegistry.RegisterRecipe(412, ERecipeType.Assemble, 3600, new[] { 2103, 1205, 1406, 2207 }, new[] { 1, 50, 20, 20 }, new[] { collector.ID },
            //    new[] { 1 }, "FactorySpaceStationPlaneFilter_Desc", 1606);
            // IDs from https://dsp-wiki.com/Modding:Recipe_IDs
            var planeFilterRecipe = LDB.recipes.Select(38);
            var frameRecipe = LDB.recipes.Select(80);
            var reinforcedThrusterRecipe = LDB.recipes.Select(21);
            var assemblingMachineMk3Recipe = LDB.recipes.Select(47);

            var rateOfProduction = 30 * 8;
            // FIXME: Handle recipes better, stop assuming integer fields are the right ones
            var requiredAssemblingMachineMk3s = rateOfProduction * planeFilterRecipe.TimeSpend / planeFilterRecipe.ResultCounts[0];
            var framesToBuild = requiredAssemblingMachineMk3s;

            ProtoRegistry.RegisterRecipe(
                412,
                ERecipeType.Assemble,
                4000,
                new[] { 2305 },//, 1125, 1406 }, //assemblingMachineMk3Recipe.Results[0], frameRecipe.Results[0], reinforcedThrusterRecipe.Results[0] },
                new[] { 1 },//1, 1 }, // requiredAssemblingMachineMk3s, framesToBuild, 25 },
                new[] { collector.ID },
                new[] { 1 },
                "FactorySpaceStationPlaneFilter_Desc",
                1606
            );

            collectorModel = ProtoRegistry.RegisterModel(302, collector, "Entities/Prefabs/interstellar-logistic-station", null, new[] { 18, 11, 32, 1 }, 607);//, 2, new[] { 2105, 0 });

            ProtoRegistry.onLoadingFinished += AddGigaCollector;

            Harmony harmony = new Harmony("com.46bit.dsp-factory-space-stations-plugin");
            harmony.PatchAll(typeof(StationEditPatch));
            harmony.PatchAll(typeof(SaveFixPatch));
            harmony.PatchAll(typeof(UIStationWindowPatch));
            foreach (var pluginInfo in BepInEx.Bootstrap.Chainloader.PluginInfos)
            {
                if (pluginInfo.Value.Metadata.GUID != WARPERS_MOD_GUID) continue;

                ((ConfigEntry<bool>)pluginInfo.Value.Instance.Config["General", "ShowWarperSlot"]).Value = true;
                logger.LogInfo("Overriding Distribute Space Warpers config: ShowWarperSlot = true");
                break;
            }

/*            UtilSystem.AddLoadMessageHandler(Save?FixPatch.GetFixMessage);
*/
            logger.LogInfo("GigaStations is initialized!");

        }

        void AddGigaCollector()
        {
            collectorModel.prefabDesc.isCollectStation = false;
            collectorModel.prefabDesc.workEnergyPerTick = 3333334;
            //workEnergyPerTick / ((double)buildPreview.desc.stationCollectSpeed > 0
            collectorModel.prefabDesc.stationMaxItemCount = ilsMaxStorage;
            collectorModel.prefabDesc.stationMaxItemKinds = ilsMaxSlots;
            collectorModel.prefabDesc.stationMaxDroneCount = ilsMaxDrones;
            collectorModel.prefabDesc.stationMaxShipCount = ilsMaxVessels;
            collectorModel.prefabDesc.stationMaxEnergyAcc = Convert.ToInt64(ilsMaxAcuGJ * 1000000000);

            //Make Giga stations blue
            Material newMat = Instantiate(collectorModel.prefabDesc.lodMaterials[0][0]);
            newMat.color = stationColor;
            collectorModel.prefabDesc.lodMaterials[0][0] = newMat;
            // Set MaxWarpers in station init!!!!!


        }

    }
}
