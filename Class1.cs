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
        public static ResourceData resource2;

        void Awake()
        {
            logger = Logger;

            string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            resource = new ResourceData(MODNAME, "gigastations", pluginfolder);
            resource.LoadAssetBundle("gigastations");
            ProtoRegistry.AddResource(resource);
            resource2 = new ResourceData(MODNAME, "dsp_factory_space_stations", pluginfolder);
            resource2.LoadAssetBundle("dsp_factory_space_stations");
            if (!resource2.HasAssetBundle())
            {
                throw new Exception("asset bundle not loaded");
            }
            ProtoRegistry.AddResource(resource2);

            //General
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
            var constructionUnit = ProtoRegistry.RegisterItem(2111, "ConstructionUnit_Name", "ConstructionUnit_Desc", "dsp_factory_space_stations/icon_collector", 2702, 64);
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
            collector = ProtoRegistry.RegisterItem(2112, "FactorySpaceStation_Name", "FactorySpaceStation_Desc", "dsp_factory_space_stations_icon_collector", 2703);
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
            collectorModel = ProtoRegistry.RegisterModel(302, collector, "dsp_factory_space_stations_tower", null, new[] { 18, 11, 32, 1 }, 607);

            //GameObject gameObject = Resources.Load<GameObject>("factory-space-station-tower");
            /*if (gameObject == null)
            {
                *//*var loadAsset = AssetBundle.LoadFromFile("C:\\Users\\miki\\AppData\\Roaming\\Thunderstore Mod Manager\\DataFolder\\DysonSphereProgram\\profiles\\Default\\BepInEx\\plugins\\Unknown-DSPFactorySpaceStations\\dsp_factory_space_stations2");
                if (loadAsset == null)
                {
                    throw new Exception("invalid model path: unable to load asset bundle");
                }*//*
                if (!resource2.bundle.Contains("factory-space-station-tower.prefab"))
                {
                    throw new Exception("asset does not exist");
                }
                GameObject obj = resource2.bundle.LoadAsset<GameObject>("factory-space-station-tower.prefab");
                if (obj == null)
                {
                    throw new Exception("asset exists but not gameobject");
                }
                throw new Exception("invalid bundle? unable to load asset as gameobject");
            }
*/
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
            if (!collectorModel.prefabDesc.hasObject)
            {
                throw new Exception("could not load GameObject from asset for factory space station");
            }

            collectorModel.prefabDesc.isStation = true;
            collectorModel.prefabDesc.isStellarStation = true;
            collectorModel.prefabDesc.isCollectStation = false;
            collectorModel.prefabDesc.isPowerConsumer = false;

            collectorModel.prefabDesc.workEnergyPerTick = 3333334;
            collectorModel.prefabDesc.stationMaxItemCount = ilsMaxStorage;
            collectorModel.prefabDesc.stationMaxItemKinds = ilsMaxSlots;
            collectorModel.prefabDesc.stationMaxDroneCount = ilsMaxDrones;
            collectorModel.prefabDesc.stationMaxShipCount = ilsMaxVessels;
            collectorModel.prefabDesc.stationMaxEnergyAcc = Convert.ToInt64(ilsMaxAcuGJ * 1000000000);

            // FIXME: copy material etc over as well
            collectorModel.prefabDesc.lodMaterials = LDB.items.Select(2104).prefabDesc.lodMaterials;
            Material newMat = Instantiate(collectorModel.prefabDesc.lodMaterials[0][0]);
            newMat.color = stationColor;
            collectorModel.prefabDesc.lodMaterials[0][0] = newMat;
            // Set MaxWarpers in station init!!!!!

            for (int i = 0; i < collectorModel.prefabDesc.landPoints.Length; i++)
            {
                collectorModel.prefabDesc.landPoints[i].x *= 3;
                collectorModel.prefabDesc.landPoints[i].z *= 3;
            }
        }
    }

    public struct SpaceStationConstruction : ISerializeState
    {
        public Dictionary<int, int> remainingConstructionItems;

        public void FromRecipe(RecipeProto recipe, int productionRate)
        {
            remainingConstructionItems = new Dictionary<int, int>();

            // Allow no output item's rate to exceed productionRate
            var maxProductionsPerSecond = productionRate;
            for (int i = 0; i < recipe.ResultCounts.Length; i++)
            {
                maxProductionsPerSecond = Math.Min(maxProductionsPerSecond, productionRate / recipe.ResultCounts[i]);
            }
            var secondsPerProduction = recipe.TimeSpend / 60.0;

            double neededSorterMk3, neededBeltMk3, neededFrames, neededTurbines;
            switch (recipe.Type)
            {
                case ERecipeType.Assemble:
                    var assemblerSpeedDivider = LDB.items.Select(2305).prefabDesc.assemblerSpeed / 10000.0;
                    var neededAssemblerMk3 = maxProductionsPerSecond * secondsPerProduction / assemblerSpeedDivider;
                    remainingConstructionItems.Add(2305, roundForLogistics(neededAssemblerMk3));

                    neededSorterMk3 = neededAssemblerMk3 * (recipe.Items.Length + recipe.Results.Length);
                    neededBeltMk3 = neededSorterMk3 * 6;
                    neededFrames = neededAssemblerMk3 * 4;
                    neededTurbines = neededAssemblerMk3 / 2;
                    break;
                case ERecipeType.Smelt:
                    var planeSmelterSpeedDivider = LDB.items.Select(2315).prefabDesc.assemblerSpeed / 10000.0;
                    var neededPlaneSmelter = maxProductionsPerSecond * secondsPerProduction / planeSmelterSpeedDivider;
                    remainingConstructionItems.Add(2315, roundForLogistics(neededPlaneSmelter));

                    neededSorterMk3 = neededPlaneSmelter * (recipe.Items.Length + recipe.Results.Length);
                    neededBeltMk3 = neededSorterMk3 * 6;
                    neededFrames = neededPlaneSmelter * 4;
                    neededTurbines = neededPlaneSmelter / 2;
                    break;
                case ERecipeType.Chemical:
                    var chemicalPlantSpeedDivider = LDB.items.Select(2309).prefabDesc.assemblerSpeed / 10000.0;
                    var neededChemicalPlants = maxProductionsPerSecond * secondsPerProduction / chemicalPlantSpeedDivider;
                    remainingConstructionItems.Add(2309, roundForLogistics(neededChemicalPlants));

                    neededSorterMk3 = neededChemicalPlants * (recipe.Items.Length + recipe.Results.Length);
                    neededBeltMk3 = neededSorterMk3 * 10;
                    neededFrames = neededChemicalPlants * 8;
                    neededTurbines = neededChemicalPlants;
                    break;
                case ERecipeType.Refine:
                    var refinerySpeedDivider = LDB.items.Select(2308).prefabDesc.assemblerSpeed / 10000.0;
                    var neededRefineries = maxProductionsPerSecond * secondsPerProduction / refinerySpeedDivider;
                    remainingConstructionItems.Add(2308, roundForLogistics(neededRefineries));

                    neededSorterMk3 = neededRefineries * (recipe.Items.Length + recipe.Results.Length);
                    neededBeltMk3 = neededSorterMk3 * 8;
                    neededFrames = neededRefineries * 8;
                    neededTurbines = neededRefineries;
                    break;
                case ERecipeType.Particle:
                    var particleColliderSpeedDivider = LDB.items.Select(2310).prefabDesc.assemblerSpeed / 10000.0;
                    var neededParticleColliders = maxProductionsPerSecond * secondsPerProduction / particleColliderSpeedDivider;
                    remainingConstructionItems.Add(2310, roundForLogistics(neededParticleColliders));

                    neededSorterMk3 = neededParticleColliders * (recipe.Items.Length + recipe.Results.Length);
                    neededBeltMk3 = neededSorterMk3 * 10;
                    neededFrames = neededParticleColliders * 12;
                    neededTurbines = neededParticleColliders * 1.5;
                    break;
                case ERecipeType.Research:
                    var labSpeedDivider = LDB.items.Select(2901).prefabDesc.assemblerSpeed / 10000.0;
                    var neededLabs = maxProductionsPerSecond * secondsPerProduction / labSpeedDivider;
                    remainingConstructionItems.Add(2901, roundForLogistics(neededLabs));

                    neededSorterMk3 = neededLabs * (recipe.Items.Length + recipe.Results.Length);
                    neededBeltMk3 = neededSorterMk3 * 2;
                    neededFrames = neededLabs * 6;
                    neededTurbines = neededLabs / 2;
                    break;
                case ERecipeType.Fractionate: // FIXME: Implement nice error rather than failing silently
                    var fractionatorSpeedDivider = LDB.items.Select(2314).prefabDesc.assemblerSpeed / 10000.0;
                    var neededFractionators = 0; // maxProductionsPerSecond * secondsPerProduction / fractionatorSpeedDivider;
                    remainingConstructionItems.Add(2314, roundForLogistics(neededFractionators));

                    neededSorterMk3 = 0;
                    neededBeltMk3 = neededFractionators * 4;
                    neededFrames = neededFractionators * 4;
                    neededTurbines = neededFractionators / 2;
                    break;
                default: // Not handling ERecipeType.Exchange but that doesn't seem to be available from UIRecipePicker
                    throw new Exception("recipe type unsupported");
            }
            remainingConstructionItems.Add(2013, roundForLogistics(neededSorterMk3));
            remainingConstructionItems.Add(2003, roundForLogistics(neededBeltMk3));
            remainingConstructionItems.Add(1125, roundForLogistics(neededFrames));
            remainingConstructionItems.Add(1204, roundForLogistics(neededTurbines));
            GigaStationsPlugin.logger.LogInfo("Requesting construction items: " + string.Join(", ", remainingConstructionItems));
        }

        // Requesting less than 100 items as the `max` from a logistics station seems to
        // get rounded down to 0. So round-up. Very reasonable given we were already
        // discarding leftover constructions items.
        // FIXME: Move this logic to logistics-specific code
        private int roundForLogistics(double itemCount)
        {
            return (int) (Math.Ceiling(itemCount / 100.0) * 100.0);
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
