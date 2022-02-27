﻿using System;
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
                "dsp_factory_space_stations_tower_v2",
                null,
                new[] { 18, 11, 32, 1 },
                608
            );

            StarSpaceStationsState.register();

            ProtoRegistry.onLoadingFinished += OnLoadingFinished;

            Harmony harmony = new Harmony(MODGUID);
            harmony.PatchAll(typeof(StationComponentPatch));
            harmony.PatchAll(typeof(UIRecipePickerPatch));
            harmony.PatchAll(typeof(UIStationWindowPatch));
            harmony.PatchAll(typeof(SaveFixPatch)); // FIXME: Taken from GigaStations, figure out if relevant

            UtilSystem.AddLoadMessageHandler(SaveFixPatch.GetFixMessage);

            Log.Info("waiting");
        }

        void OnLoadingFinished()
        {
            if (!factorySpaceStationModel.prefabDesc.hasObject)
            {
                throw new Exception("could not load GameObject from asset for factory space station");
            }

            // FIXME: Move most of these onto the prefab asset
            factorySpaceStationModel.prefabDesc.isPowerConsumer = false;
            factorySpaceStationModel.prefabDesc.workEnergyPerTick = 3333334; // FIXME: Configure

            var interstellarLogisticsTower = LDB.items.Select(2104);
            factorySpaceStationModel.prefabDesc.materials = interstellarLogisticsTower.prefabDesc.materials;
            factorySpaceStationModel.prefabDesc.lodMaterials = interstellarLogisticsTower.prefabDesc.lodMaterials;
            //Material newMat = Instantiate(collectorModel.prefabDesc.lodMaterials[0][0]);
            //newMat.color = new Color(0.3726f, 0.8f, 1f, 1f);
            //collectorModel.prefabDesc.lodMaterials[0][0] = newMat;

            Log.Info("loaded");
        }
    }
}