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
namespace DSPFactorySpaceStations
{
    [BepInDependency(CommonAPIPlugin.GUID)]
    [BepInDependency("me.xiaoye97.plugin.Dyson.LDBTool")]
    [BepInDependency(GIGASTATIONS_GUID)]
    [BepInPlugin(MODGUID, MODNAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(UtilSystem), nameof(StarExtensionSystem))]
    public class DSPFactorySpaceStationsPlugin : BaseUnityPlugin
    {
        public const string MODGUID = "46bit.plugin.DSPFactorySpaceStationsPlugin";
        public const string MODNAME = "FactorySpaceStations";
        public const string VERSION = "0.0.1";

        public const string GIGASTATIONS_GUID = "org.kremnev8.plugin.GigaStationsUpdated";

        public static int spaceStationsStateRegistryId;

        public static ResourceData resourceData;
        public static ItemProto factorySpaceStationItem;
        public static ModelProto factorySpaceStationModel;

        void Awake()
        {
            Log.logger = Logger;
            Log.Info("starting");

            resourceData = new ResourceData(MODNAME, "dsp_factory_space_stations");
            resourceData.LoadAssetBundle("Assets\\dsp_factory_space_stations");
            Assert.True(resourceData.HasAssetBundle());
            ProtoRegistry.AddResource(resourceData);

            ProtoRegistry.RegisterString("ConstructionUnitName", "Space Construction Unit");
            ProtoRegistry.RegisterString("ConstructionUnitDesc", "Component for constructing space structures");
            ProtoRegistry.RegisterString("ConstructionUnitRecipeDesc", "Component for constructing space structures");
            var constructionUnit = ProtoRegistry.RegisterItem(
                4601,
                "ConstructionUnitName",
                "ConstructionUnitDesc",
                "dsp_factory_space_stations_icon_collector",
                1607,
                64
            );
            ProtoRegistry.RegisterRecipe(
                4601,
                ERecipeType.Assemble,
                1800,
                new[] { 5001, 1203, 1305, 1205, 2209 },
                new[] { 5, 50, 25, 10, 1 },
                new[] { constructionUnit.ID },
                new[] { 1 },
                "ConstructionUnitRecipeDesc"
            );

            ProtoRegistry.RegisterString("FactorySpaceStationName", "Factory Space Station");
            ProtoRegistry.RegisterString("FactorySpaceStationDesc", "Space station that can act as a factory");
            ProtoRegistry.RegisterString("FactorySpaceStationRecipeDesc", "Space station that can act as a factory");
            factorySpaceStationItem = ProtoRegistry.RegisterItem(
                4602,
                "FactorySpaceStationName",
                "FactorySpaceStationDesc", 
                "dsp_factory_space_stations_icon_collector",
                1608,
                10
            );
            factorySpaceStationItem.BuildInGas = true;
            ProtoRegistry.RegisterRecipe(
                4602,
                ERecipeType.Assemble,
                4000,
                new[] { 2105, constructionUnit.ID, 1125, 2210, 1406 },
                new[] { 1, 8, 100, 16, 60 },
                new[] { factorySpaceStationItem.ID },
                new[] { 1 },
                "FactorySpaceStationRecipeDesc",
                1606
            );
            factorySpaceStationModel = ProtoRegistry.RegisterModel(
                303, 
                factorySpaceStationItem, 
                "dsp_factory_space_stations_tower", 
                null, 
                new[] { 18, 11, 32, 1 },
                608
            );

            spaceStationsStateRegistryId = CommonAPI.Systems.StarExtensionSystem.registry.Register("space_stations_state", typeof(StarSpaceStationsState));

            ProtoRegistry.onLoadingFinished += OnLoadingFinished;

            Harmony harmony = new Harmony(MODGUID);
            harmony.PatchAll(typeof(StationEditPatch));
            harmony.PatchAll(typeof(SaveFixPatch));
            harmony.PatchAll(typeof(UIRecipePickerPatch));
            harmony.PatchAll(typeof(UIStationWindowPatch));

            UtilSystem.AddLoadMessageHandler(SaveFixPatch.GetFixMessage);

            Log.Info("waiting");
        }

        void OnLoadingFinished()
        {
            if (!factorySpaceStationModel.prefabDesc.hasObject)
            {
                throw new Exception("could not load GameObject from asset for factory space station");
            }

            factorySpaceStationModel.prefabDesc.isStation = true;
            factorySpaceStationModel.prefabDesc.isStellarStation = true;
            factorySpaceStationModel.prefabDesc.isCollectStation = false;

            factorySpaceStationModel.prefabDesc.isPowerConsumer = false;
            factorySpaceStationModel.prefabDesc.workEnergyPerTick = 3333334;

            factorySpaceStationModel.prefabDesc.stationMaxItemCount = 10000;
            factorySpaceStationModel.prefabDesc.stationMaxItemKinds = 12;
            factorySpaceStationModel.prefabDesc.stationMaxDroneCount = 100;
            factorySpaceStationModel.prefabDesc.stationMaxShipCount = 20;
            factorySpaceStationModel.prefabDesc.stationMaxEnergyAcc = 50 * 1_000_000_000l;

            var interstellarLogisticsTower = LDB.items.Select(2104);
            factorySpaceStationModel.prefabDesc.materials = interstellarLogisticsTower.prefabDesc.materials;
            factorySpaceStationModel.prefabDesc.lodMaterials = interstellarLogisticsTower.prefabDesc.lodMaterials;
            //Material newMat = Instantiate(collectorModel.prefabDesc.lodMaterials[0][0]);
            //newMat.color = new Color(0.3726f, 0.8f, 1f, 1f);
            //collectorModel.prefabDesc.lodMaterials[0][0] = newMat;

            Log.Info("loaded");
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
            Log.Info("Requesting construction items: " + string.Join(", ", remainingConstructionItems));
        }

        // Requesting less than 100 items as the `max` from a logistics station seems to
        // get rounded down to 0. So round-up. Very reasonable given we were already
        // discarding leftover constructions items.
        // FIXME: Move this logic to logistics-specific code
        private int roundForLogistics(double itemCount)
        {
            return (int) (Math.Ceiling(itemCount / 100.0) * 100.0);
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
