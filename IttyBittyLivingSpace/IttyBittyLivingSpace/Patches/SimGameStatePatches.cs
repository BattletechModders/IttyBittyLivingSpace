using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace IttyBittyLivingSpace.Patches
{
    [HarmonyPatch(typeof(SimGameState), "GetExpenditures")]
    [HarmonyPatch(new Type[] { typeof(EconomyScale), typeof(bool) })]
    [HarmonyAfter(new string[] { "de.morphyum.MechMaintenanceByCost" })]
    public static class SimGameState_GetExpenditures
    {
        public static void Postfix(SimGameState __instance, ref int __result, EconomyScale expenditureLevel, bool proRate)
        {
            Mod.Log.Info?.Write($"SGS:GE entered with {__result}");

            // Subtract the base cost of mechs
            float expenditureCostModifier = __instance.GetExpenditureCostModifier(expenditureLevel);
            int defaultMechCosts = 0;
            foreach (MechDef mechDef in __instance.ActiveMechs.Values)
            {
                defaultMechCosts += Mathf.RoundToInt(expenditureCostModifier * (float)__instance.Constants.Finances.MechCostPerQuarter);
            }

            // Add the new costs
            int activeMechCosts = Helper.CalculateTotalForUpkeep(__instance);

            double gearInventorySize = Helper.GetGearInventorySize(__instance);
            int gearStorageCosts = Helper.CalculateTotalForGearCargo(__instance, gearInventorySize);

            double mechPartsTonnage = Helper.CalculateTonnageForAllMechParts(__instance);
            int mechPartsStorageCost = Helper.CalculateTotalForMechPartsCargo(__instance, mechPartsTonnage);

            int total = __result - defaultMechCosts + activeMechCosts + gearStorageCosts + mechPartsStorageCost;
            Mod.Log.Info?.Write($"SGS:GE - total:{total} ==> result:{__result} - defaultMechCosts:{defaultMechCosts} = {__result - defaultMechCosts} + activeMechs:{activeMechCosts} + gearStorage:{gearStorageCosts} + partsStorage:{mechPartsStorageCost}");
            __result = total;
        }
    }
}
