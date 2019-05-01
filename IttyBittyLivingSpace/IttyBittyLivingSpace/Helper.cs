
using BattleTech;
using System;
using System.Collections.Generic;

namespace IttyBittyLivingSpace {

    public class ExpensesSorter : IComparer<KeyValuePair<string, int>> {
        public int Compare(KeyValuePair<string, int> x, KeyValuePair<string, int> y) {
            int cmp = y.Value.CompareTo(x.Value);
            if (cmp == 0) {
                cmp = y.Key.CompareTo(x.Key);
            }
            return cmp;
        }
    }

    public static class Helper {
        
        public static int CalculateActiveGearCost(SimGameState sgs) {
            Mod.Log.Info($" === Calculating Active Mech Costs === ");



            return 0;
        }

        public static int GetGearInventorySize(SimGameState sgs) {
            int totalUnits = 0;
            foreach (MechComponentRef mcRef in sgs.GetAllInventoryItemDefs()) {
                int itemCount = sgs.GetItemCount(mcRef.Def.Description, mcRef.Def.GetType(), sgs.GetItemCountDamageType(mcRef));
                int itemSize = mcRef.Def.InventorySize;
                string itemId = mcRef.Def.Description.Id;
                Mod.Log.Debug($"  Inventory item:({mcRef.Def.Description.Id}) qty:{itemCount} size:{mcRef.Def.InventorySize}");
                totalUnits += itemSize;
            }
            return totalUnits;
        }

        public static int CalculateGearCost(SimGameState sgs, double totalUnits) {
            Mod.Log.Info($" === Calculating Gear Costs === ");

            double factoredSize = Math.Ceiling(totalUnits * Mod.Config.GearFactor);
            Mod.Log.Info($"  totalUnits:{totalUnits} x factor:{Mod.Config.GearFactor} = {factoredSize}");

            double scaledUnits = Math.Pow(factoredSize, Mod.Config.GearExponent);
            int totalCost = (int)(Mod.Config.GearCostPerUnit * scaledUnits);
            Mod.Log.Info($"  scaledUnits:{scaledUnits} x costPerUnit:{Mod.Config.GearCostPerUnit} = {totalCost}");

            return totalCost;
        }

        public static double GetMechPartsTonnage(SimGameState sgs) {
            double totalTonnage = 0;
            foreach (ChassisDef cDef in sgs.GetAllInventoryMechDefs(true)) {
                int itemCount = cDef.MechPartCount;
                Mod.Log.Debug($"ChassisDef: {cDef.Description.Id} has count: x{cDef.MechPartCount} with max: x{cDef.MechPartMax}");

                if (cDef.MechPartCount == 0) {
                    Mod.Log.Debug($"  Complete chassis, adding tonnage:{cDef.Tonnage}");
                    totalTonnage += cDef.Tonnage;
                } else {
                    float mechPartRatio = (float)cDef.MechPartCount / (float)cDef.MechPartMax;
                    float fractionalTonnage = cDef.Tonnage * mechPartRatio;
                    Mod.Log.Debug($"  Mech parts ratio: {mechPartRatio} mechTonnage:{cDef.Tonnage} fractionalTonnage:{fractionalTonnage}");

                    double roundedTonnage = Math.Ceiling(fractionalTonnage / 5);
                    double tonnageToAdd = roundedTonnage * 5;
                    Mod.Log.Debug($"  RoundedTonnage:{roundedTonnage} tonnageToAdd:{tonnageToAdd}");
                    totalTonnage += tonnageToAdd;
                }
            }
            Mod.Log.Debug($"TotalTonnage from mech parts:{totalTonnage}");
            return totalTonnage;
        }

        public static int CalculateMechPartsCost(SimGameState sgs, double totalTonnage) {
            Mod.Log.Info($" === Calculating Mech Parts === ");

            double factoredTonnage = Math.Ceiling(totalTonnage * Mod.Config.MechPartsFactor);
            Mod.Log.Info($"  totalUnits:{totalTonnage} x factor:{Mod.Config.MechPartsFactor} = {factoredTonnage}");

            double scaledTonnage = Math.Pow(factoredTonnage, Mod.Config.MechPartsExponent);
            int totalCost = (int)(Mod.Config.MechPartsCostPerTon * scaledTonnage);
            Mod.Log.Info($"  scaledTonnage:{scaledTonnage} x costPerUnit:{Mod.Config.GearCostPerUnit} = {totalCost}");

            return totalCost;
        }
        
    }
}
