using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System;
using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace DSPFactorySpaceStations
{
    [HarmonyPatch]
    public static class UIStationWindowPatch
    {
        public static RectTransform contentTrs;
        public static RectTransform scrollTrs;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), "OnStationIdChange")]
        [HarmonyPriority(Priority.Last)]
        public static void OnStationIdChangePre(UIStationWindow __instance, ref string __state)
        {
            if (__instance.stationId == 0 || __instance.factory == null || __instance.transport?.stationPool == null)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            ItemProto itemProto = LDB.items.Select(__instance.factory.entityPool[stationComponent.entityId].protoId);

            if (itemProto.ID != DSPFactorySpaceStationsPlugin.collector.ID || !__instance.active)
            {
                return;
            }

            __state = stationComponent.name;
            if (string.IsNullOrEmpty(__state))
            {
                __state = "Factory Space Station #" + stationComponent.gid;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "OnStationIdChange")]
        public static void OnStationIdChangePost(UIStationWindow __instance, string __state)
        {
            if (__instance.stationId == 0 || __instance.factory == null || __instance.transport?.stationPool == null)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            ItemProto itemProto = LDB.items.Select(__instance.factory.entityPool[stationComponent.entityId].protoId);

            int storageCount = ((stationComponent.isCollector || stationComponent.isVeinCollector) ? stationComponent.collectionIds.Length : stationComponent.storage.Length);

            int baseYSize = (stationComponent.isStellar || itemProto.ID == DSPFactorySpaceStationsPlugin.collector.ID) ? 376 : 316;
            if (stationComponent.isCollector)
            {
                baseYSize = 136;
            }
            else if (stationComponent.isVeinCollector)
            {
                baseYSize = 289;
            }

            ((RectTransform)__instance.storageUIs[0].transform).anchoredPosition = new Vector2(0, 0);

            int yPos = stationComponent.isVeinCollector ? -190 : -90;
            scrollTrs.anchoredPosition = new Vector2(scrollTrs.anchoredPosition.x, yPos);

            if (itemProto.ID != DSPFactorySpaceStationsPlugin.collector.ID)
            {
                foreach (UIStationStorage slot in __instance.storageUIs)
                {
                    slot.popupBoxRect.anchoredPosition = new Vector2(5, 0);
                }

                scrollTrs.sizeDelta = new Vector2(scrollTrs.sizeDelta.x, 76 * storageCount);
                contentTrs.sizeDelta = new Vector2(contentTrs.sizeDelta.x, 76 * storageCount);
                int newYSize = baseYSize + 76 * storageCount;

                __instance.windowTrans.sizeDelta = new Vector2(600, newYSize);
                return;
            }

            __instance.nameInput.text = __state;

            if (__instance.active)
            {
                int newXSize = 600;

                foreach (UIStationStorage slot in __instance.storageUIs)
                {
                    slot.popupBoxRect.anchoredPosition = new Vector2(5, 0);
                }

                int visibleCount = storageCount;
                for (int i = storageCount - 1; i >= 0; i--) {
                    if (stationComponent.storage[i].itemId != 0)
                    {
                        break;
                    }
                    visibleCount--;
                }

                int newYSize = baseYSize + 76 * visibleCount;

                __instance.windowTrans.sizeDelta = new Vector2(newXSize, newYSize);

                scrollTrs.sizeDelta = new Vector2(scrollTrs.sizeDelta.x, 76 * visibleCount);
                contentTrs.sizeDelta = new Vector2(contentTrs.sizeDelta.x, 76 * visibleCount);

                for (int i = 0; i < __instance.storageUIs.Length; i++)
                {
                    if (i < storageCount)
                    {
                        __instance.storageUIs[i].station = stationComponent;
                        __instance.storageUIs[i].index = i;
                        __instance.storageUIs[i]._Open();
                    }
                    else
                    {
                        __instance.storageUIs[i].station = null;
                        __instance.storageUIs[i].index = 0;
                        __instance.storageUIs[i]._Close();
                    }
                    __instance.storageUIs[i].ClosePopMenu();
                }

                if (stationComponent.minerId == 0)
                {
                    UIRecipePicker.Popup(__instance.windowTrans.anchoredPosition + new Vector2(-300f, -135f), new Action<RecipeProto>(recipe => UIStationWindowPatch.OnRecipePickerReturn(__instance, recipe)));
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnUpdate")]
        public static void OnStationUpdate(UIStationWindow __instance)
        {
            if (__instance.stationId == 0 || __instance.factory == null)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];

            float size = __instance.powerGroupRect.sizeDelta.x - 140;
            float percent = stationComponent.energy / (float)stationComponent.energyMax;

            float diff = percent > 0.7 ? -30 : 30;

            __instance.energyText.rectTransform.anchoredPosition = new Vector2(Mathf.Round(size * percent + diff), 0.0f);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnCreate")]
        public static bool _OnCreatePrefix(UIStationWindow __instance)
        {
            //part of 1% sliderstep fix
            __instance.minDeliverDroneSlider.maxValue = 100;
            __instance.minDeliverVesselSlider.maxValue = 100;

            GameObject prefab = DSPFactorySpaceStationsPlugin.resource1.bundle.LoadAsset<GameObject>("assets/gigastations/ui/station-scroll.prefab");

            GameObject scrollPane = UnityEngine.Object.Instantiate(prefab, __instance.transform, false);
            scrollTrs = (RectTransform)scrollPane.transform;

            scrollTrs.anchorMin = Vector2.up;
            scrollTrs.anchorMax = Vector2.one;
            scrollTrs.pivot = new Vector2(0.5f, 1);
            scrollTrs.offsetMin = new Vector2(40, 400);
            scrollTrs.offsetMax = new Vector2(-40, -90);

            GameObject contentPane = scrollPane.transform.Find("Viewport/pane").gameObject;
            contentTrs = (RectTransform)contentPane.transform;

            __instance.storageUIs = new UIStationStorage[12];
            for (int i = 0; i < __instance.storageUIs.Length; i++)
            {
                __instance.storageUIs[i] = UnityEngine.Object.Instantiate(__instance.storageUIPrefab, contentTrs);
                __instance.storageUIs[i].stationWindow = __instance;
                __instance.storageUIs[i]._Create();
            }
            __instance.veinCollectorPanel._Create();

            return false;
        }

        private static void OnRecipePickerReturn(UIStationWindow __instance, RecipeProto recipe)
        {
            if (recipe == null)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            ItemProto itemProto = LDB.items.Select(__instance.factory.entityPool[stationComponent.entityId].protoId);
            if (itemProto.ID != DSPFactorySpaceStationsPlugin.collector.ID) // not my stations
            {
                return;
            }

            StarSpaceStationsState spaceStationsState = CommonAPI.Systems.StarExtensionSystem.GetExtension<StarSpaceStationsState>(__instance.factory.planet.star, DSPFactorySpaceStationsPlugin.spaceStationsStateRegistryId);
            spaceStationsState.spaceStations[stationComponent.id] = new SpaceStationState();
            var construction = new SpaceStationConstruction();
            construction.FromRecipe(recipe, 8 * 30);
            spaceStationsState.spaceStations[stationComponent.id].construction = construction;

            var maxStorage = itemProto.prefabDesc.stationMaxItemCount + __instance.factory.gameData.history.remoteStationExtraStorage;

            var store = stationComponent.storage;
            lock (store)
            {
                // FIXME: Handle warpers or antimatter rods being part of the recipe
                int i = 2;
                foreach (var item in construction.remainingConstructionItems)
                {
                    store[i].itemId = item.Key;
                    store[i].localLogic = ELogisticStorage.Demand;
                    store[i].remoteLogic = ELogisticStorage.Demand;
                    store[i].max = (int)Math.Min(maxStorage, item.Value);
                    i++;
                }

                // FIXME: Stop abusing a field to store the recipe ID
                stationComponent.minerId = recipe.ID;
            }

            __instance.factory.transport.RefreshTraffic(stationComponent.id);
            __instance.factory.gameData.galacticTransport.RefreshTraffic(stationComponent.gid);
            __instance.OnStationIdChange();
        }
    }
}