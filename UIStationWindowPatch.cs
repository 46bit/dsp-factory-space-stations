using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System;
using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace GigaStations
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

            if (itemProto.ID != GigaStationsPlugin.collector.ID || !__instance.active)
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

            int baseYSize = (stationComponent.isStellar || itemProto.ID == GigaStationsPlugin.collector.ID) ? 376 : 316;
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

            if (itemProto.ID != GigaStationsPlugin.collector.ID)
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
        [HarmonyPatch(typeof(UIStationWindow), "OnWarperIconClick")]
        public static bool OnWarperIconClickPrefix(UIStationWindow __instance, ref int obj)
        {
            if ((__instance.stationId == 0 || __instance.factory == null))
            {
                __instance._Close();
                return false;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            ItemProto gigaProto = LDB.items.Select(__instance.factory.entityPool[stationComponent.entityId].protoId);
            if (gigaProto.ID != GigaStationsPlugin.collector.ID)
            {
                return true; // not my ILS, return to original code
            }
            if (__instance.stationId == 0 || __instance.factory == null)
            {
                return false;
            }
            if (stationComponent.id != __instance.stationId)
            {
                return false;
            }
            if (__instance.player.inhandItemId > 0 && __instance.player.inhandItemCount == 0)
            {
                __instance.player.SetHandItems(0, 0);
            }
            else if (__instance.player.inhandItemId > 0 && __instance.player.inhandItemCount > 0)
            {
                int num = 1210;
                ItemProto itemProto = LDB.items.Select(num);
                if (__instance.player.inhandItemId != num)
                {
                    UIRealtimeTip.Popup("只能放入".Translate() + itemProto.name);
                    return false;
                }
                int num2 = GigaStationsPlugin.ilsMaxWarps;
                int warperCount = stationComponent.warperCount;
                int num3 = num2 - warperCount;
                if (num3 < 0)
                {
                    num3 = 0;
                }
                int num4 = (__instance.player.inhandItemCount >= num3) ? num3 : __instance.player.inhandItemCount;
                if (num4 <= 0)
                {
                    UIRealtimeTip.Popup("栏位已满".Translate());
                    return false;
                }
                stationComponent.warperCount += num4;
                __instance.player.AddHandItemCount_Unsafe(-num4);
                if (__instance.player.inhandItemCount <= 0)
                {
                    __instance.player.SetHandItemId_Unsafe(0);
                    __instance.player.SetHandItemCount_Unsafe(0);
                }
            }
            else if (__instance.player.inhandItemId == 0 && __instance.player.inhandItemCount == 0)
            {
                int warperCount2 = stationComponent.warperCount;
                int num5 = warperCount2;
                if (num5 <= 0)
                {
                    return false;
                }
                if (VFInput.shift || VFInput.control)
                {
                    num5 = __instance.player.package.AddItemStacked(1210, num5, 0, out int _);
                    if (warperCount2 != num5)
                    {
                        UIRealtimeTip.Popup("无法添加物品".Translate());
                    }
                    UIItemup.Up(1210, num5);
                }
                else
                {
                    __instance.player.SetHandItemId_Unsafe(1210);
                    __instance.player.SetHandItemCount_Unsafe(num5);
                }
                stationComponent.warperCount -= num5;
                if (stationComponent.warperCount < 0)
                {
                    Assert.CannotBeReached();
                    stationComponent.warperCount = 0;
                }
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnCreate")]
        public static bool _OnCreatePrefix(UIStationWindow __instance)
        {
            //part of 1% sliderstep fix
            __instance.minDeliverDroneSlider.maxValue = 100;
            __instance.minDeliverVesselSlider.maxValue = 100;

            GameObject prefab = GigaStationsPlugin.resource.bundle.LoadAsset<GameObject>("assets/gigastations/ui/station-scroll.prefab");

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
            if (itemProto.ID != GigaStationsPlugin.collector.ID) // not my stations
            {
                return;
            }

            StarSpaceStationsState spaceStationsState = CommonAPI.Systems.StarExtensionSystem.GetExtension<StarSpaceStationsState>(__instance.factory.planet.star, GigaStationsPlugin.spaceStationsStateRegistryId);
            spaceStationsState.spaceStations[stationComponent.id] = new SpaceStationState();
            var construction = new SpaceStationConstruction();
            construction.FromRecipe(recipe, 8 * 30);
            spaceStationsState.spaceStations[stationComponent.id].construction = construction;

            var maxStorage = GigaStationsPlugin.ilsMaxStorage + __instance.factory.gameData.history.remoteStationExtraStorage;

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