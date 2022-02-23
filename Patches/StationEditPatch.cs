using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace DSPFactorySpaceStations
{
    [HarmonyPatch]
    public static class StationEditPatch
    {
        /*
                public static CodeMatcher MoveToLabel(this CodeMatcher matcher, Label label)
                {
                    while (!matcher.Labels.Contains(label))
                    {
                        matcher.Advance(1);
                    }

                    return matcher;
                }

                private static readonly Dictionary<OpCode, OpCode> storeToLoad = new Dictionary<OpCode, OpCode>
                {
                    {OpCodes.Stloc_0, OpCodes.Ldloc_0},
                    {OpCodes.Stloc_1, OpCodes.Ldloc_1},
                    {OpCodes.Stloc_2, OpCodes.Ldloc_2},
                    {OpCodes.Stloc_3, OpCodes.Ldloc_3},
                    {OpCodes.Stloc, OpCodes.Ldloc},
                    {OpCodes.Stloc_S, OpCodes.Ldloc_S}
                };

                public static OpCode ToLoad(this OpCode opCode)
                {
                    if (storeToLoad.Keys.Contains(opCode))
                    {
                        return storeToLoad[opCode];
                    }

                    throw new ArgumentException($"Can't convert instruction {opCode.ToString()} to a load instruction!");
                }
                [HarmonyTranspiler]
                [HarmonyPatch(typeof(GameHistoryData), "UnlockTechFunction")]
                public static IEnumerable<CodeInstruction> MultiplyTechValues(IEnumerable<CodeInstruction> instructions)
                {

                    CodeMatcher matcher = new CodeMatcher(instructions)
                        .MatchForward(false, new CodeMatch(OpCodes.Switch));

                    Label[] jumpTable = (Label[])matcher.Operand;

                    matcher.MoveToLabel(jumpTable[17]) // 18 - 1
                        .MatchForward(false, new CodeMatch(OpCodes.Add))
                        .InsertAndAdvance(Transpilers.EmitDelegate<Func<int>>(() => GigaStationsPlugin.droneCapacityMultiplier))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Mul));

                    matcher.MoveToLabel(jumpTable[18]) // 19 - 1
                        .MatchForward(false, new CodeMatch(OpCodes.Add))
                        .InsertAndAdvance(Transpilers.EmitDelegate<Func<int>>(() => GigaStationsPlugin.vesselCapacityMultiplier))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Mul));

                    return matcher.InstructionEnumeration();
                }

                public delegate void RefAction<in T1, T2>(T1 arg, ref T2 arg2);

                [HarmonyTranspiler]
                [HarmonyPatch(typeof(TechProto), "UnlockFunctionText")]
                public static IEnumerable<CodeInstruction> MultiplyUnlockText(IEnumerable<CodeInstruction> instructions)
                {

                    CodeMatcher matcher = new CodeMatcher(instructions)
                        .MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(TechProto), nameof(TechProto.UnlockFunctions))))
                        .MatchForward(false, new CodeMatch(instr => instr.IsStloc()));

                    OpCode typeStlocOpcode = matcher.Opcode.ToLoad();
                    object typeStlocOperand = matcher.Operand;

                    matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(TechProto), nameof(TechProto.UnlockValues))))
                        .MatchForward(false, new CodeMatch(OpCodes.Stloc_S));

                    object arg = matcher.Operand;
                    matcher.Advance(1)
                        .InsertAndAdvance(new CodeInstruction(typeStlocOpcode, typeStlocOperand))
                        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloca_S, arg))
                        .InsertAndAdvance(Transpilers.EmitDelegate<RefAction<int, int>>((int type, ref int value) =>
                        {
                            if (type == 18)
                            {
                                value *= GigaStationsPlugin.droneCapacityMultiplier;
                            }
                            else if (type == 19)
                            {
                                value *= GigaStationsPlugin.vesselCapacityMultiplier;
                            }
                        }));

                    return matcher.InstructionEnumeration();
                }*/

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), "Init")]
        public static void InitPostfix(StationComponent __instance)
        {
            if (__instance.isCollector)
            {
                var store = __instance.storage;
                lock(store)
                {
                    for (int i = 0; i < store.Length; i++)
                    {
                        store[i].localLogic = ELogisticStorage.Supply;
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), "Import")]
        public static void ImportPostfix(StationComponent __instance)
        {
            if (__instance.isCollector)
            {
                var store = __instance.storage;
                lock (store)
                {
                    for (int i = 0; i < store.Length; i++)
                    {
                        store[i].localLogic = ELogisticStorage.Supply;
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "NewConsumerComponent")]
        public static bool NewConsumerComponentPrefix(PowerSystem __instance, ref int entityId, ref long work, ref long idle)
        {
            var x = LDB.items.Select(__instance.factory.entityPool[entityId].protoId).ID;
            if (x != DSPFactorySpaceStationsPlugin.factorySpaceStationItem.ID)
            {
                return true;
            }

            work = 1000000;

            return true;

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), "Init")] // maybe swap with normal VFPreload if not supporting modded tesla towers? or later preloadpostpatch LDBTool one again if already done
        public static void StationComponentInitPostfix(StationComponent __instance, ref int _id, ref int _entityId, ref int _pcId, ref PrefabDesc _desc, ref EntityData[] _entityPool) // Do when LDB is done
        {
            if (_entityPool[_entityId].protoId != DSPFactorySpaceStationsPlugin.factorySpaceStationItem.ID) 
            {
                return;
            }

            __instance.needs = new int[13];

            __instance.energyMax = _desc.stationMaxEnergyAcc;
            __instance.warperMaxCount = 100;
            // FIXME: Choose sensible value and move these configs to central place
            __instance.energyPerTick = 1000000;

            __instance.storage = new StationStore[12];
            // demand antimatter rods
            __instance.storage[0].itemId = 1803;
            __instance.storage[0].localLogic = ELogisticStorage.Demand;
            __instance.storage[0].remoteLogic = ELogisticStorage.Demand;
            __instance.storage[0].max = 750;
            // demand warpers
            __instance.storage[1].itemId = 1210;
            __instance.storage[1].localLogic = ELogisticStorage.Demand;
            __instance.storage[1].remoteLogic = ELogisticStorage.Demand;
            __instance.storage[1].max = 600;

            __instance.slots = new SlotData[12];
            for (var i = 0; i < __instance.storage.Length; i++)
            {
                __instance.slots[i].storageIdx = i;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), "InternalTickLocal")]
        public static void InternalTickLocal(StationComponent __instance, PlanetFactory factory, float dt)
        {
            ItemProto itemProto = LDB.items.Select(factory.entityPool[__instance.entityId].protoId);
            if (itemProto.ID != DSPFactorySpaceStationsPlugin.factorySpaceStationItem.ID) 
            {
                return;
            }

            // Simulate getting energy from antimatter rods
            // No state available, so removing them probabilistically for now
            StationStore[] store = __instance.storage;
            lock (store)
            {
                if (__instance.storage[0].count == 0)
                {
                    __instance.energyPerTick = 0;
                }
                else if (__instance.energy < __instance.energyMax) {
                    __instance.energyPerTick = 1_500_000_000;
                    var rng = new System.Random();
                    if (rng.Next((int)Math.Round(1.0 / dt)) == 0)
                    {
                        store[0].inc -= 1;
                        store[0].count -= 1;
                    }
                }
                __instance.energy = Math.Min(__instance.energyMax, __instance.energy + (int)Math.Round(__instance.energyPerTick * dt));
            }

            // FIXME: Stop abusing minerId field to store recipe ID
            if (__instance.minerId == 0)
            {
                return;
            }

            var recipe = LDB.recipes.Select(__instance.minerId);
            StarSpaceStationsState spaceStationsState = CommonAPI.Systems.StarExtensionSystem.GetExtension<StarSpaceStationsState>(factory.planet.star, DSPFactorySpaceStationsPlugin.spaceStationsStateRegistryId);
            if (spaceStationsState.spaceStations[__instance.id].construction is SpaceStationConstruction)
            {
                var construction = (SpaceStationConstruction) spaceStationsState.spaceStations[__instance.id].construction;
                if (!construction.Complete())
                {
                    lock (store)
                    {
                        for (int i = 2; i < store.Length; i++)
                        {
                            if (store[i].itemId == 0)
                            {
                                continue;
                            }
                            construction.remainingConstructionItems[store[i].itemId] -= store[i].count;
                            if (construction.remainingConstructionItems[store[i].itemId] <= 0)
                            {
                                var unused = -construction.remainingConstructionItems[store[i].itemId];
                                store[i].count = unused;
                                store[i].inc = unused;
                                store[i].max = 0;
                                construction.remainingConstructionItems[store[i].itemId] = 0;
                            } else {
                                store[i].count = 0;
                                store[i].inc = 0;
                                store[i].max = Math.Min(store[i].max, construction.remainingConstructionItems[store[i].itemId]);
                            }
                        }
                        if (!construction.Complete())
                        {
                            return;
                        }

                        var maxStorage = itemProto.prefabDesc.stationMaxItemCount + factory.gameData.history.remoteStationExtraStorage;

                        for (int i = 2; i < 12; i++) {
                            store[i] = new StationStore();
                        }

                        // FIXME: Handle warpers or antimatter rods being part of the recipe
                        for (var i = 0; i < recipe.Items.Length; i++)
                        {
                            store[i + 2].itemId = recipe.Items[i];
                            store[i + 2].localLogic = ELogisticStorage.Demand;
                            store[i + 2].remoteLogic = ELogisticStorage.Demand;
                            store[i + 2].max = maxStorage;
                        }
                        for (var i = 0; i < recipe.Results.Length; i++)
                        {
                            store[i + 2 + recipe.Items.Length].itemId = recipe.Results[i];
                            store[i + 2 + recipe.Items.Length].localLogic = ELogisticStorage.Supply;
                            store[i + 2 + recipe.Items.Length].remoteLogic = ELogisticStorage.Supply;
                            store[i + 2 + recipe.Items.Length].max = maxStorage;
                        }
                    }

                    // Try to prevent glitches when construction items are also part of the production items
                    factory.transport.RefreshTraffic(__instance.id);
                    factory.gameData.galacticTransport.RefreshTraffic(__instance.gid);
                }

                lock (store)
                {
                    // Stop producing if any recipe results are full
                    // FIXME: Be willing to dump some things like hydrogen
                    for (int i = 0; i < recipe.Results.Length; i++)
                    {
                        if (store[2 + i + recipe.Items.Length].count >= store[2 + i + recipe.Items.Length].max)
                        {
                            return;
                        }
                    }

                    var maxRateOfProduction = 30 * 8;
                    var maxProductions = maxRateOfProduction;
                    for (int i = 0; i < recipe.Items.Length; i++)
                    {
                        var possibleProductionsFromItem = store[2 + i].count / recipe.ItemCounts[i];
                        maxProductions = Math.Min(maxProductions, possibleProductionsFromItem);
                    }
                    var energyPerProduction = 1_000_000_000 / maxRateOfProduction;
                    var possibleProductionsFromEnergy = __instance.energy / energyPerProduction;
                    maxProductions = Math.Min(maxProductions, (int)possibleProductionsFromEnergy);

                    var producedSinceLastTick = (int)Math.Round(maxProductions * dt);
                    for (int i = 0; i < recipe.Items.Length; i++)
                    {
                        var itemConsumed = producedSinceLastTick * recipe.ItemCounts[i];
                        store[2 + i].inc -= itemConsumed;
                        store[2 + i].count -= itemConsumed;
                    }
                    for (int i = 0; i < recipe.Results.Length; i++)
                    {
                        var itemProduced = producedSinceLastTick * recipe.ResultCounts[i];
                        store[2 + recipe.Items.Length + i].inc += itemProduced;
                        store[2 + recipe.Items.Length + i].count += itemProduced;
                    }
                    __instance.energy -= energyPerProduction * producedSinceLastTick;
                }
            }
        }

        /*
                [HarmonyTranspiler]
                [HarmonyPatch(typeof(StationComponent), "Import")]
                public static IEnumerable<CodeInstruction> StationImportTranspiler(IEnumerable<CodeInstruction> instructions)
                {

                    return new CodeMatcher(instructions)
                        .MatchForward(false, // false = move at the start of the match, true = move at the end of the match
                    new CodeMatch(OpCodes.Ldarg_0),
                            new CodeMatch(OpCodes.Ldc_I4_6),
                            new CodeMatch(OpCodes.Newarr),
                            new CodeMatch(i => i.opcode == OpCodes.Stfld && ((FieldInfo)i.operand).Name == nameof(StationComponent.needs)))
                        .Advance(1)
                        .Set(OpCodes.Ldc_I4, 13)
                        .InstructionEnumeration();

                }
        */

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), "UpdateNeeds")]
        public static bool UpdateNeeds(StationComponent __instance)
        {
            // Do for all, should not matter

            int num = __instance.storage.Length;
            if (num > 12)
            {
                num = 12;
            }
            for (int i = 0; i <= num; i++)
            {
                if (i == num && !__instance.isCollector)
                {
                    __instance.needs[num] = !__instance.isStellar || __instance.warperCount >= __instance.warperMaxCount ? 0 : 1210; // HIDDEN SLOT?!?!
                }
                else if (i < __instance.needs.Length)
                {
                    __instance.needs[i] = i >= num || __instance.storage[i].count >= __instance.storage[i].max ? 0 : __instance.storage[i].itemId;
                }
            }
            return false;
        }


        // Fixing Belt cannot input for item-slots 7-12
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoPath), "TryPickItemAtRear", new[] { typeof(int[]), typeof(int), typeof(byte), typeof(byte) }, new[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out })]
        // ReSharper disable once RedundantAssignment
        public static bool TryPickItemAtRear(CargoPath __instance, int[] needs, out int needIdx, out byte stack, out byte inc, ref int __result)
        {
            needIdx = -1;
            stack = 1;
            inc = 0;

            int num = __instance.bufferLength - 5 - 1;
            if (__instance.buffer[num] == 250)
            {
                int num2 = __instance.buffer[num + 1] - 1 + (__instance.buffer[num + 2] - 1) * 100 + (__instance.buffer[num + 3] - 1) * 10000 + (__instance.buffer[num + 4] - 1) * 1000000;
                int item = __instance.cargoContainer.cargoPool[num2].item;
                stack = __instance.cargoContainer.cargoPool[num2].stack;
                inc = __instance.cargoContainer.cargoPool[num2].inc;

                for (int i = 0; i < needs.Length; i++)
                {
                    if (item == needs[i])
                    {
                        Array.Clear(__instance.buffer, num - 4, 10);
                        int num3 = num + 5 + 1;
                        if (__instance.updateLen < num3)
                        {
                            __instance.updateLen = num3;
                        }
                        __instance.cargoContainer.RemoveCargo(num2);
                        needIdx = i;
                        __result = item;
                        return false;
                    }
                }
            }
            __result = 0;
            return false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), "TakeItem", new[] { typeof(int), typeof(int), typeof(int[]), typeof(int) }, new[] { ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Out })]
        public static bool TakeItemPrefix(StationComponent __instance, ref int _itemId, ref int _count, ref int[] _needs, out int _inc)
        {
            _inc = 0;
            bool itemIsNeeded = false;
            if (_needs == null)
            {
                itemIsNeeded = true;
            }
            else
            {
                foreach (var need in _needs)
                {
                    if (need == _itemId)
                    {
                        itemIsNeeded = true;
                    }
                }
            }

            if (_itemId > 0 && _count > 0 && itemIsNeeded)
            {
                StationStore[] obj = __instance.storage;
                lock (obj)
                {
                    int num = __instance.storage.Length;
                    for (int i = 0; i < num; i++)
                    {
                        if (__instance.storage[i].itemId == _itemId && __instance.storage[i].count > 0)
                        {
                            _count = _count >= __instance.storage[i].count ? __instance.storage[i].count : _count;
                            _itemId = __instance.storage[i].itemId;
                            _inc = (int)(__instance.storage[i].inc / (double)__instance.storage[i].count * _count + 0.5);
                            StationStore[] array = __instance.storage;

                            array[i].count -= _count;
                            array[i].inc -= _inc;
                            return false;
                        }
                    }
                }
            }
            _itemId = 0;
            _count = 0;
            _inc = 0;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), "AddItem")]
        // ReSharper disable once RedundantAssignment
        public static bool AddItemPrefix(StationComponent __instance, int itemId, int count, int inc, ref int __result)
        {
            __result = 0;
            if (itemId <= 0) return false;

            StationStore[] obj = __instance.storage;
            lock (obj)
            {
                for (int i = 0; i < __instance.storage.Length; i++)
                {
                    if (__instance.storage[i].itemId != itemId) continue;

                    StationStore[] array = __instance.storage;
                    array[i].count += count;
                    array[i].inc += inc;

                    __result = count;
                    return false;
                }
            }

            return false;
        }
    }
}