using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace GigaStations
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


        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "NewConsumerComponent")]
        public static bool NewConsumerComponentPrefix(PowerSystem __instance, ref int entityId, ref long work, ref long idle)
        {
            var x = LDB.items.Select(__instance.factory.entityPool[entityId].protoId).ID;
            if (x != GigaStationsPlugin.collector.ID)
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
            if (_entityPool[_entityId].protoId != GigaStationsPlugin.collector.ID) // not my stations
            {
                return;
            }

            __instance.needs = new int[13];

            _desc.stationMaxEnergyAcc = Convert.ToInt64(GigaStationsPlugin.ilsMaxAcuGJ * 1000000000);
            __instance.energyMax = GigaStationsPlugin.ilsMaxAcuGJ * 1000000000;
            __instance.warperMaxCount = GigaStationsPlugin.ilsMaxWarps;
            __instance.energyPerTick = 1000000;

            __instance.storage = new StationStore[2 + 2 + 1];
            // demand casimir crystals
            __instance.storage[0].itemId = 1126;
            __instance.storage[0].localLogic = ELogisticStorage.Demand;
            __instance.storage[0].remoteLogic = ELogisticStorage.Demand;
            __instance.storage[0].max = GigaStationsPlugin.ilsMaxStorage;
            // demand titanium glass
            __instance.storage[1].itemId = 1119;
            __instance.storage[1].localLogic = ELogisticStorage.Demand;
            __instance.storage[1].remoteLogic = ELogisticStorage.Demand;
            __instance.storage[1].max = GigaStationsPlugin.ilsMaxStorage;
            // supply plane filters
            __instance.storage[2].itemId = 1304;
            __instance.storage[2].localLogic = ELogisticStorage.Supply;
            __instance.storage[2].remoteLogic = ELogisticStorage.Supply;
            __instance.storage[2].max = GigaStationsPlugin.ilsMaxStorage;
            // demand warpers
            __instance.storage[3].itemId = 1210;
            __instance.storage[3].localLogic = ELogisticStorage.Demand;
            __instance.storage[3].remoteLogic = ELogisticStorage.Demand;
            __instance.storage[3].max = 1000;
            // demand antimatter rods
            __instance.storage[4].itemId = 1803;
            __instance.storage[4].localLogic = ELogisticStorage.Demand;
            __instance.storage[4].remoteLogic = ELogisticStorage.Demand;
            __instance.storage[4].max = 1000;

            __instance.slots = new SlotData[__instance.storage.Length];
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
            if (itemProto.ID != GigaStationsPlugin.collector.ID) // not my stations
            {
                return;
            }
            
            var planeFilterRecipe = LDB.recipes.Select(38);
            StationStore[] store = __instance.storage;
            lock (store)
            {
                if (__instance.storage[4].count == 0)
                {
                    __instance.energyPerTick = 0;
                }
                else
                {
                    __instance.energyPerTick = 1_500_000_000;
                    var rng = new System.Random();
                    if (rng.Next((int) Math.Round(1.0 / dt)) == 0)
                    {
                        store[4].inc -= 1;
                        store[4].count -= 1;
                    }
                }
                __instance.energy = Math.Min(__instance.energyMax, __instance.energy + (int)Math.Round(__instance.energyPerTick * dt));

                if (store[2].count >= store[2].max)
                {
                    return;
                }

                var rateOfProduction = 30 * 8;
                var maxProductions = rateOfProduction;
                for (int i = 0; i < planeFilterRecipe.Items.Length; i++)
                {
                    var itemId = planeFilterRecipe.Items[i];
                    var requiredForOne = planeFilterRecipe.ItemCounts[i];
                    var present = store.Where(s => s.itemId == itemId).Single().count;
                    var possibleProductions = present / requiredForOne;
                    maxProductions = Math.Min(maxProductions, possibleProductions);
                }

                // FIXME: Work for multiple-results
                var producedSinceLastTick = (int)Math.Round(maxProductions * dt);

                var neededEnergy = (1_000_000_000 / rateOfProduction) * producedSinceLastTick;
                if (__instance.energy < neededEnergy)
                {
                    return;
                }

                // FIXME: Accumulate the floating point error somewhere?
                store[2].inc += producedSinceLastTick * planeFilterRecipe.ResultCounts[0];
                store[2].count += producedSinceLastTick * planeFilterRecipe.ResultCounts[0];
                GigaStationsPlugin.logger.LogInfo(producedSinceLastTick);

                store[0].inc -= producedSinceLastTick;
                store[1].inc -= producedSinceLastTick;
                store[0].count -= producedSinceLastTick;
                store[1].count -= producedSinceLastTick;

                __instance.energy -= neededEnergy;


                /*for (int i = 0; i < planeFilterRecipe.Items.Length; i++)
                {
                    var itemId = planeFilterRecipe.Items[i];
                    var requiredForOne = planeFilterRecipe.ItemCounts[i];
                    var ingredientStorage = store.Where(s => s.itemId == itemId).Single();
                    ingredientStorage.inc -= producedSinceLastTick * requiredForOne;
                    ingredientStorage.count -= producedSinceLastTick * requiredForOne;
                }*/
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