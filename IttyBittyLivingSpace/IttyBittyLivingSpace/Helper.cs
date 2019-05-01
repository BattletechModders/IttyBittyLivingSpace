
using BattleTech;
using System;

namespace IttyBittyLivingSpace {

    public static class Helper {
        
        public static int CalculateActiveGearCost(SimGameState sgs) {
            Mod.Log.Info($" === Calculating Active Mech Costs === ");

            return 0;
        }

        public static int CalculateGearCost(SimGameState sgs) {
            Mod.Log.Info($" === Calculating Gear Costs === ");

            int totalUnits = 0;
            foreach (MechComponentRef mcRef in sgs.GetAllInventoryItemDefs()) {
                int itemCount = sgs.GetItemCount(mcRef.Def.Description, mcRef.Def.GetType(), sgs.GetItemCountDamageType(mcRef));
                int itemSize = mcRef.Def.InventorySize;
                string itemId = mcRef.Def.Description.Id;
                Mod.Log.Debug($"  Inventory item:({mcRef.Def.Description.Id}) qty:{itemCount} size:{mcRef.Def.InventorySize}");
                totalUnits += itemSize;
            }

            double calculatedUnits = Math.Pow(totalUnits, 2);
            int totalCost = (int)(Mod.Config.GearCostPerUnit * calculatedUnits);
            Mod.Log.Info($"  calculatedUnits:{calculatedUnits} x costPerUnit:{Mod.Config.GearCostPerUnit} = {totalCost}");

            return totalCost;
        }

        public static int CalculateMechPartsCost(SimGameState sgs) {
            Mod.Log.Info($" === Calculating Mech Parts === ");

            double totalTonnage = 0;
            foreach (ChassisDef cDef in sgs.GetAllInventoryMechDefs(true)) {
                int itemCount = cDef.MechPartCount;
                Mod.Log.Debug($"  ChassisDef: {cDef.Description.Id} has count: x{cDef.MechPartCount} with max: {cDef.MechPartMax}");

                if (cDef.MechPartCount == 0) {
                    Mod.Log.Debug($"  Complete chassis, adding tonnage:{cDef.Tonnage}");
                    totalTonnage += cDef.Tonnage;
                } else {
                    float mechPartRatio = (cDef.MechPartCount / cDef.MechPartMax);
                    float fractionalTonnage = cDef.Tonnage * mechPartRatio;
                    Mod.Log.Debug($"  Mech parts ratio: {mechPartRatio} mechTonnage:{cDef.Tonnage} fractionalTonnage:{fractionalTonnage}");

                    double roundedTonnage = Math.Ceiling(fractionalTonnage / 5);
                    double tonnageToAdd = roundedTonnage * 5;
                    Mod.Log.Debug($"  RoundedTonnage:{roundedTonnage} tonnageToAdd:{tonnageToAdd}");
                    totalTonnage += tonnageToAdd;
                }
            }
            Mod.Log.Debug($"TotalTonnage from mech parts:{totalTonnage}");
            double calculatedUnits = Math.Pow(totalTonnage, 2);
            int totalCost = (int)(Mod.Config.GearCostPerUnit * calculatedUnits);
            Mod.Log.Info($"  calculatedUnits:{calculatedUnits} x costPerUnit:{Mod.Config.GearCostPerUnit} = {totalCost}");

            return totalCost;
        }
        
    }
}
