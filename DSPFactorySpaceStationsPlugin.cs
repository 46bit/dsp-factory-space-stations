using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using System;
using System.IO;
using CommonAPI;
using CommonAPI.Systems;
using UnityEngine;
/*
namespace DSPFactorySpaceStations
{
    // FIXME: This PluginInfo won't work and I have no idea why
    //[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInPlugin("com.46bit.dsp-factory-space-stations-plugin", "DSP Factory Space Stations Plugin", "0.0.0")]
    [BepInDependency("me.xiaoye97.plugin.Dyson.LDBTool")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry))]
    public class DSPFactorySpaceStationsPlugin : BaseUnityPlugin
    {
        public static DSPFactorySpaceStationsPlugin instance;

        public static ManualLogSource logger;

        //public static ItemProto spaceStationsLogisticsSystem;
        //public static ModelProto spaceStationsLogisticsSystemModel;
        public static ResourceData resource;

        public static ItemProto fssPlaneFilter;
        public static ModelProto collectorModel;

        void Awake()
        {
            instance = this;
            logger = Logger;

            logger.LogInfo("DSPFactorySpaceStationsPlugin: awake");

            // FIXME: Make my own icons
            string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            resource = new ResourceData("com.46bit.dsp-factory-space-stations-plugin", "gigastations", pluginfolder);
            resource.LoadAssetBundle("gigastations");
            ProtoRegistry.AddResource(resource);

            ProtoRegistry.RegisterString("FSSPlaneFilter_Name", "Factory for Plane Filters");
            ProtoRegistry.RegisterString("FSSPlaneFilter_Desc", "Space station near a gas giant producing Plane Filters");

            fssPlaneFilter = ProtoRegistry.RegisterItem(2112, "FSSPlaneFilter_Name", "FSSPlaneFilter_Desc", "assets/gigastations/texture2d/icon_collector", 2703);
            fssPlaneFilter.BuildInGas = true;
            ProtoRegistry.RegisterRecipe(412, ERecipeType.Assemble, 3600, new[] { 1125, 1406 }, new[] { 50, 20 }, new[] { fssPlaneFilter.ID },
                new[] { 1 }, "FSSPlaneFilter_Desc", 1606);
            //collectorModel = ProtoRegistry.RegisterModel(302, fssPlaneFilter, "Entities/Prefabs/orbital-collector", null, new[] { 18, 11, 32, 1 }, 607);

            //ProtoRegistry.onLoadingFinished += AddGigaCollector;

            *//*
            fssPlaneFilter = ProtoRegistry.RegisterItem(4601, "FSSPlaneFilter_Name", "FSSPlaneFilter_Desc", "assets/gigastations/texture2d/icon_collector", 2703);
            //pls = ProtoRegistry.RegisterItem(2110, "PLS_Name", "PLS_Desc", "assets/gigastations/texture2d/icon_pls", 2701);
            fssPlaneFilter.BuildInGas = true;

            *//* foreach (var recipe in LDB.recipes.dataArray)
             {
                 recipe.Name
                 recipe.Type == ERecipeType.Assemble
                 recipe.Items
                 recipe.Results
             }*//*

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
                4602, 
                ERecipeType.Assemble,
                4000,
                new[] { 2305, 1125, 1406 }, //assemblingMachineMk3Recipe.Results[0], frameRecipe.Results[0], reinforcedThrusterRecipe.Results[0] },
                new[] { 1, 1, 1 }, // requiredAssemblingMachineMk3s, framesToBuild, 25 },
                new[] { fssPlaneFilter.ID },
                new[] { 1 },
                "FSSPlaneFilter_Desc",
                1606
            );


            *//*ProtoRegistry.RegisterRecipe(
                462,
                ERecipeType.Assemble,
                400,
                new[] { 1001 },
                new[] { 1 },
                new[] { 1002 },
                new[] { 1 },
                "FSSPlaneFilter_Desc"
            );*//*

            //collectorModel = ProtoRegistry.RegisterModel(463, fssPlaneFilter, "Entities/Prefabs/orbital-collector", null, new[] { 18, 11, 32, 1 }, 607);
            collectorModel = ProtoRegistry.RegisterModel(4603, fssPlaneFilter, "Entities/Prefabs/orbital-collector", null, new[] { 18, 11, 32, 1 }, 607, 2, new[] { 2105, 0 });*/

            /*
                        plsModel = ProtoRegistry.RegisterModel(300, pls, "Entities/Prefabs/logistic-station", null, new[] { 24, 38, 12, 10, 1 }, 605, 2, new[] { 2103, 0 });
                        ilsModel = ProtoRegistry.RegisterModel(301, ils, "Entities/Prefabs/interstellar-logistic-station", null, new[] { 24, 38, 12, 10, 1 }, 606, 2, new[] { 2104, 0 });
                        collectorModel = ProtoRegistry.RegisterModel(302, collector, "Entities/Prefabs/orbital-collector", null, new[] { 18, 11, 32, 1 }, 607, 2, new[] { 2105, 0 });

                        ProtoRegistry.onLoadingFinished += AddGigaPLS;
                        ProtoRegistry.onLoadingFinished += AddGigaILS;
            */



            /*spaceStationsLogisticsSystemModel = ProtoRegistry.RegisterModel(301, spaceStationsLogisticsSystem, "Entities/Prefabs/interstellar-logistic-station", null, new[] { 24, 38, 12, 10, 1 }, 606, 2, new[] { 2104, 0 });
            ProtoRegistry.onLoadingFinished += AddSpaceStationLogisticsSystem;*//*

            // FIXME: This PluginInfo won't work and I have no idea why
            //Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            //Harmony harmony = new Harmony("com.46bit.dsp-factory-space-stations-plugin");
            //harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        void AddGigaCollector()
        {
            var oriItem = LDB.items.Select(2105);

            collectorModel.prefabDesc.stationMaxItemCount = 20000;
            collectorModel.prefabDesc.stationCollectSpeed = oriItem.prefabDesc.stationCollectSpeed * 1;

            collectorModel.prefabDesc.workEnergyPerTick /= oriItem.prefabDesc.workEnergyPerTick / 1 >= 0 ? 1 : oriItem.prefabDesc.workEnergyPerTick; //??yes or no??

            //Make Giga stations blue
            Material newMat = Instantiate(collectorModel.prefabDesc.lodMaterials[0][0]);
            newMat.color = new Color(0.3726f, 0.8f, 1f, 1f);
            collectorModel.prefabDesc.lodMaterials[0][0] = newMat;

            // Set MaxWarpers in station init!!!!!
        }
        *//*
                void AddSpaceStationLogisticsSystem()
                {
                    spaceStationsLogisticsSystemModel.prefabDesc.stationShipPos.z += 20;

                    Material newMat = Instantiate(spaceStationsLogisticsSystemModel.prefabDesc.lodMaterials[0][0]);
                    newMat.color = new Color(0.3726f, 0.8f, 1f, 1f);
                    spaceStationsLogisticsSystemModel.prefabDesc.lodMaterials[0][0] = newMat;
                }
        */
        /* [HarmonyPatch(typeof(StationComponent), "Init")]
         class StationComponent_Init_Patch : HarmonyPatch
         {
             public static void Prefix(PrefabDesc _desc, int _entityId, EntityData[] _entityPool)
             {
                 if (_desc.isStellarStation)
                 {
                     //_desc.stationShipPos.z += 150;
                     _entityPool[_entityId].pos.z += 100;
                     //_entityPool[_entityId].rot.x += (float)Math.PI / 2;
                     _entityPool[_entityId].rot *= Quaternion.Euler(90, 0, 0);
                 }
             }

             *//*public static void Postfix(StationComponent __instance)
             {
                 if (__instance.isStellar)
                 {
                     __instance.shipDockRot.x += (float)Math.PI / 2;

                     int num = __instance.workShipDatas.Length;
                     for (int i = 0; i < num; i++)
                     {
                         __instance.shipDiskRot[i] = Quaternion.Euler(0f, 360f / (float)num * (float)i, 0f);
                         __instance.shipDiskPos[i] = __instance.shipDiskRot[i] * new Vector3(0f, 0f, 11.5f);
                     }
                     for (int j = 0; j < num; j++)
                     {
                         __instance.shipDiskRot[j] = __instance.shipDockRot * __instance.shipDiskRot[j];
                         __instance.shipDiskPos[j] = __instance.shipDockPos + __instance.shipDockRot * __instance.shipDiskPos[j];
                     }
                 }
             }*//*
         }

         [HarmonyPatch(typeof(StationComponent), "Import")]
         class StationComponent_Import_Patch : HarmonyPatch
         {
             public static void Prefix(PrefabDesc _desc)
             {
                 if (_desc.isStellarStation)
                 {

                     _desc.stationShipPos.z += 150;
                 }
             }

             public static void Postfix(StationComponent __instance)
             {
                 if (__instance.isStellar)
                 {
                     //__instance.shipDockPos = _entityPool[_entityId].pos + _entityPool[_entityId].rot * _desc.stationShipPos;
                     __instance.shipDockRot.x += (float) Math.PI / 2;// = _entityPool[_entityId].rot;

                     int num = __instance.workShipDatas.Length;
                     for (int i = 0; i < num; i++)
                     {
                         __instance.shipDiskRot[i] = Quaternion.Euler(0f, 360f / (float)num * (float)i, 0f);
                         __instance.shipDiskPos[i] = __instance.shipDiskRot[i] * new Vector3(0f, 0f, 11.5f);
                     }
                     for (int j = 0; j < num; j++)
                     {
                         __instance.shipDiskRot[j] = __instance.shipDockRot * __instance.shipDiskRot[j];
                         __instance.shipDiskPos[j] = __instance.shipDockPos + __instance.shipDockRot * __instance.shipDiskPos[j];
                     }
                 }
             }
         }*//*
    }
}
*/