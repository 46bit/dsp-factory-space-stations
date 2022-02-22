using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Collections.Generic;
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
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(UtilSystem), nameof(StarExtensionSystem))]
    public class GigaStationsPlugin : BaseUnityPlugin
    {

        public const string MODGUID = "org.kremnev8.plugin.GigaStationsUpdated";
        public const string MODNAME = "GigaStationsUpdated";
        public const string VERSION = "2.2.7";

        public const string LDB_TOOL_GUID = "me.xiaoye97.plugin.Dyson.LDBTool";
        public const string WARPERS_MOD_GUID = "ShadowAngel.DSP.DistributeSpaceWarper";

        public static int spaceStationsStateRegistryId;

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

            ProtoRegistry.RegisterString("ModificationWarn", "  - [GigaStationsUpdated] Replaced {0} buildings");
            ProtoRegistry.RegisterString("CantDowngradeWarn", "Downgrading logistic station is not possible!");

            ProtoRegistry.RegisterString("ConstructionUnit_Name", "Space Construction Unit");
            ProtoRegistry.RegisterString("ConstructionUnit_Desc", "Component for constructing space structures");
            var constructionUnit = ProtoRegistry.RegisterItem(2111, "ConstructionUnit_Name", "ConstructionUnit_Desc", "assets/gigastations/icon_ils", 2702, 64);
            ProtoRegistry.RegisterRecipe(
                411,
                ERecipeType.Assemble,
                1800,
                new[] { 1502, 5001, 1305, 1127, 2209 },
                new[] { 50, 20, 15, 10, 2 },
                new[] { constructionUnit.ID },
                new[] { 1 },
                "ConstructionUnit_Desc",
                1606
            );

            ProtoRegistry.RegisterString("FactorySpaceStation_Name", "Factory Space Station");
            ProtoRegistry.RegisterString("FactorySpaceStation_Desc", "Space station that can act as a factory");
            collector = ProtoRegistry.RegisterItem(2112, "FactorySpaceStation_Name", "FactorySpaceStation_Desc", "assets/gigastations/icon_collector", 2703);
            collector.BuildInGas = true;
            ProtoRegistry.RegisterRecipe(
                412,
                ERecipeType.Assemble,
                4000,
                new[] { constructionUnit.ID, 2105, 1502, 2210, 1204, 1406 },
                new[] { 8, 3, 500, 16, 60, 60 },
                new[] { collector.ID },
                new[] { 1 },
                "FactorySpaceStation_Desc",
                1606
            );
            collectorModel = ProtoRegistry.RegisterModel(302, collector, "Entities/Prefabs/interstellar-logistic-station", null, new[] { 18, 11, 32, 1 }, 607);

            ProtoRegistry.onLoadingFinished += AddGigaCollector;

            spaceStationsStateRegistryId = CommonAPI.Systems.StarExtensionSystem.registry.Register("space_stations_state", typeof(StarSpaceStationsState));

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

            //UtilSystem.AddLoadMessageHandler(Save?FixPatch.GetFixMessage);

            logger.LogInfo("GigaStations is initialized!");
        }

        void AddGigaCollector()
        {
            collectorModel.prefabDesc.isStation = true;
            collectorModel.prefabDesc.isStellarStation = true;
            collectorModel.prefabDesc.isCollectStation = false;
            collectorModel.prefabDesc.isPowerConsumer = false;

            collectorModel.prefabDesc.workEnergyPerTick = 3333334;
            //workEnergyPerTick / ((double)buildPreview.desc.stationCollectSpeed > 0
            collectorModel.prefabDesc.stationMaxItemCount = ilsMaxStorage;
            collectorModel.prefabDesc.stationMaxItemKinds = ilsMaxSlots;
            collectorModel.prefabDesc.stationMaxDroneCount = ilsMaxDrones;
            collectorModel.prefabDesc.stationMaxShipCount = ilsMaxVessels;
            collectorModel.prefabDesc.stationMaxEnergyAcc = Convert.ToInt64(ilsMaxAcuGJ * 1000000000);
            // FIXME: Add other types of factories (different machines and recipes)
            collectorModel.prefabDesc.assemblerRecipeType = ERecipeType.Assemble;

            //Make Giga stations blue
            Material newMat = Instantiate(collectorModel.prefabDesc.lodMaterials[0][0]);
            newMat.color = stationColor;
            collectorModel.prefabDesc.lodMaterials[0][0] = newMat;
            // Set MaxWarpers in station init!!!!!
        }
    }

    public struct SpaceStationConstruction : ISerializeState
    {
        public Dictionary<int, int> remainingConstructionItems;

        public void FromRecipe(RecipeProto recipe, int productionRate)
        {
            // FIXME: Add support for other recipe types
            Assert.Equals(recipe.Type, ERecipeType.Assemble);

            remainingConstructionItems = new Dictionary<int, int>();

            // Allow no output item's rate to exceed productionRate
            var maxProductionsPerSecond = productionRate;
            for (int i = 0; i < recipe.ResultCounts.Length; i++)
            {
                maxProductionsPerSecond = Math.Min(maxProductionsPerSecond, productionRate / recipe.ResultCounts[i]);
            }

            var secondsPerProduction = recipe.TimeSpend / 60.0;
            var assemblerSpeedDivider = LDB.items.Select(2305).prefabDesc.assemblerSpeed / 10000.0;
            var neededAssemblerMk3 = maxProductionsPerSecond * secondsPerProduction / assemblerSpeedDivider;
            remainingConstructionItems.Add(2305, (int)Math.Ceiling(neededAssemblerMk3));

            var neededSorterMk3 = neededAssemblerMk3 * (recipe.Items.Length + recipe.Results.Length);
            remainingConstructionItems.Add(2013, (int)Math.Ceiling(neededSorterMk3));

            var neededBeltMk3 = neededSorterMk3 * 6;
            remainingConstructionItems.Add(2003, (int)Math.Ceiling(neededBeltMk3));

            var neededFrames = neededAssemblerMk3 * 4;
            remainingConstructionItems.Add(1125, (int)Math.Ceiling(neededFrames));

            var neededTurbines = neededAssemblerMk3 / 2;
            remainingConstructionItems.Add(1204, (int)Math.Ceiling(neededTurbines));
        }

        public int Provide(int itemId, int count)
        {
            if (remainingConstructionItems.ContainsKey(itemId))
            {
                var result = remainingConstructionItems[itemId] - count;
                if (result < 0)
                {
                    remainingConstructionItems[itemId] = 0;
                    return Math.Abs(result);
                }
                else
                {
                    remainingConstructionItems[itemId] = result;
                    return 0;
                }
            }
            else
            {
                return count;
            }
        }

        public bool Complete()
        {
            foreach (var item in remainingConstructionItems)
            {
                if (item.Value > 0)
                {
                    return false;
                }
            }
            return true;
        }

        public void Free()
        {
            remainingConstructionItems = null;
        }

        public void Import(BinaryReader r)
        {
            remainingConstructionItems = new Dictionary<int, int>();

            var remainingConstructionItemsCursor = r.ReadInt32();
            for (int i = 1; i < remainingConstructionItemsCursor; i++)
            {
                var itemId = r.ReadInt32();
                var count = r.ReadInt32();
                remainingConstructionItems[itemId] = count;
            }
        }

        public void Export(BinaryWriter w)
        {
            w.Write(remainingConstructionItems.Count);
            foreach (var item in remainingConstructionItems)
            {
                w.Write(item.Key);
                w.Write(item.Value);
            }
        }
    }

    public class SpaceStationState : ISerializeState
    {
        public int stationComponentId;
        public SpaceStationConstruction? construction;

        public void Init(int stationComponentId)
        {
            this.stationComponentId = stationComponentId;
        }

        public void Free()
        {
            stationComponentId = 0;
            construction = null;
        }

        public void Import(BinaryReader r)
        {
            stationComponentId = r.ReadInt32();
            if (r.ReadByte() == 1)
            {
                construction = new SpaceStationConstruction();
                construction?.Import(r);
            }
        }

        public void Export(BinaryWriter w)
        {
            w.Write(stationComponentId);
            if (construction == null)
            {
                w.Write((byte)0);
            }
            else
            {
                w.Write((byte)1);
                construction?.Export(w);
            }
        }
    }

    public class StarSpaceStationsState : IStarExtension
    {
        public int id;

        // FIXME: Switch to a CommonAPI Pool once no longer reusing StationComponent and its IDs
        public Dictionary<int, SpaceStationState> spaceStations;

        public void Init(StarData star)
        {
            id = star.id;
            spaceStations = new Dictionary<int, SpaceStationState>();
        }

        public void Free()
        {
            id = 0;
            foreach (var pair in spaceStations)
            {
                pair.Value.Free();
            }
            spaceStations = null;
        }

        public void Import(BinaryReader r)
        {
            id = r.ReadInt32();

            spaceStations = new Dictionary<int, SpaceStationState>();
            var spaceStationsCursor = r.ReadInt32();
            for (int i = 1; i < spaceStationsCursor; i++)
            {
                var spaceStationId = r.ReadInt32();
                if (r.ReadByte() != 1) continue;
                var spaceStationState = new SpaceStationState();
                spaceStationState.Import(r);
                spaceStations[spaceStationId] = spaceStationState;
            }
        }

        public void Export(BinaryWriter w)
        {
            w.Write(id);
            w.Write(spaceStations.Count);
            foreach (var item in spaceStations)
            {
                w.Write(item.Key);
                if (item.Value == null)
                {
                    w.Write((byte)0);
                }
                else
                {
                    w.Write((byte)1);
                    item.Value.Export(w);
                }
            }
        }
    }
}
