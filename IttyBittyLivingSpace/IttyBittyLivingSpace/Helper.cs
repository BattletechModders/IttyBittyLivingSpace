
using BattleTech;
using CustomComponents;
using System;
using System.Collections.Generic;

namespace IttyBittyLivingSpace {

    [CustomComponent("Storage")]
    public class Storage : SimpleCustomComponent {
        public float Size;
    }

    [CustomComponent("Upkeep")]
    public class Upkeep : SimpleCustomComponent {
        public float CostMulti;
        public float StorageSize;
    }

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
        
        public static int CalculateActiveMechCosts(SimGameState sgs) {
            Mod.Log.Info($" === Calculating Active Mech Costs === ");

            int totalCosts = 0;
            foreach (KeyValuePair<int, MechDef> entry in sgs.ActiveMechs) {
                totalCosts += CalculateMechCost(entry.Value);
            }

            return totalCosts;
        }

        public static List<KeyValuePair<string, int>> GetActiveMechLabels(SimGameState sgs) {
            Mod.Log.Info($" === Calculating Active Mech Labels === ");

            List<KeyValuePair<string, int>> labels = new List<KeyValuePair<string, int>>();
            foreach (KeyValuePair<int, MechDef> entry in sgs.ActiveMechs) {
                MechDef mechDef = entry.Value;
                int mechCost = CalculateMechCost(mechDef);
                Mod.Log.Info($"  Adding mech:{mechDef.Name} with cost:{mechCost}");
                labels.Add(new KeyValuePair<string, int>("MECH: " + mechDef.Name, mechCost));
            }

            return labels;
        }

        private static int CalculateMechCost(MechDef mechDef) {
            if (mechDef == null || mechDef.Description == null) {
                Mod.Log.Info($"  MechDef or mechDef description is null, skipping.");
            }

            Mod.Log.Info($"  Active Mech found with item: {mechDef.Description.Id}");

            double baseCost = mechDef.Description.Cost * Mod.Config.UpkeepChassisMulti;
            Mod.Log.Info($"  rawCost:{mechDef.Description.Cost} x {Mod.Config.UpkeepChassisMulti} = {baseCost}");

            double modifiedCost = 0;
            if (mechDef.Chassis != null && mechDef.Chassis.ChassisTags != null) {
                foreach (string chassisTag in mechDef.Chassis.ChassisTags) {
                    if (chassisTag == null) {
                        Mod.Log.Debug($"  tag:({chassisTag}) skipping");
                        continue;
                    } else {
                        Mod.Log.Debug($"  processing tag:({chassisTag}) ");
                    }

                    if (Mod.Config.UpkeepChassisMultis.ContainsKey(chassisTag)) {
                        float multi = Mod.Config.UpkeepChassisMultis[chassisTag];
                        modifiedCost += mechDef.Description.Cost * multi;
                        Mod.Log.Debug($"  tag:{chassisTag} multi:{multi} x cost:{mechDef.Description.Cost} = {mechDef.Description.Cost * multi}");
                    }
                }
            } else {
                Mod.Log.Debug($"  chassis has null chassisTags... skipping.");
            }

            if (modifiedCost == 0) {
                modifiedCost = baseCost;
                Mod.Log.Debug($"  No modifying tags, defaulting to baseCost.");
            }

            return (int)Math.Ceiling(modifiedCost);
        }

        public static float GetGearInventorySize(SimGameState sgs) {
            float totalUnits = 0.0f;
            foreach (MechComponentRef mcRef in sgs.GetAllInventoryItemDefs()) {
                int itemCount = sgs.GetItemCount(mcRef.Def.Description, mcRef.Def.GetType(), sgs.GetItemCountDamageType(mcRef));
                string itemId = mcRef.Def.Description.Id;
                float itemSize = mcRef.Def.InventorySize;
                Mod.Log.Debug($"  Inventory item:({mcRef.Def.Description.Id}) qty:{itemCount} size:{mcRef.Def.InventorySize}");

                if (mcRef.Is<Storage>(out Storage storage)) {
                    Mod.Log.Debug($"  Overriding size:{storage.Size} instead of:{mcRef.Def.InventorySize}");
                    itemSize = storage.Size;
                } 

                totalUnits += itemSize;
            }

            Mod.Log.Debug($"  Total storage units: {totalUnits}u");
            return totalUnits;
        }

        public static int CalculateGearCost(SimGameState sgs, double totalUnits) {
            Mod.Log.Info($" === Calculating Gear Costs === ");

            double factoredSize = Math.Ceiling(totalUnits * Mod.Config.GearFactor);
            Mod.Log.Info($"  totalUnits:{totalUnits} x factor:{Mod.Config.GearFactor} = {factoredSize}");

            double scaledUnits = Math.Pow(factoredSize, Mod.Config.GearExponent);
            int totalCost = (int)(Mod.Config.GearCostPerUnit * scaledUnits);
            Mod.Log.Info($"  scaledUnits:{scaledUnits} x costPerUnit:{Mod.Config.GearCostPerUnit} = {totalCost}");

            return (int)Math.Ceiling((double)totalCost);
        }

        public static double GetMechPartsTonnage(SimGameState sgs) {
            double allPartsTonnage = 0;
            foreach (ChassisDef cDef in sgs.GetAllInventoryMechDefs(true)) {
                int itemCount = cDef.MechPartCount;
                Mod.Log.Debug($"ChassisDef: {cDef.Description.Id} has count: x{cDef.MechPartCount} with max: x{cDef.MechPartMax}");

                double rawPartsTonnage = 0.0;
                if (cDef.MechPartCount == 0) {
                    Mod.Log.Debug($"  Complete chassis, adding tonnage:{cDef.Tonnage}");
                    rawPartsTonnage = cDef.Tonnage;
                } else {
                    float mechPartRatio = (float)cDef.MechPartCount / (float)cDef.MechPartMax;
                    float fractionalTonnage = cDef.Tonnage * mechPartRatio;
                    Mod.Log.Debug($"  Mech parts ratio: {mechPartRatio} mechTonnage:{cDef.Tonnage} fractionalTonnage:{fractionalTonnage}");

                    double roundedTonnage = Math.Ceiling(fractionalTonnage / 5); 
                    double normalizedTonnage = roundedTonnage * 5;
                    Mod.Log.Debug($"  RoundedTonnage:{roundedTonnage} normaliedTonnage:{normalizedTonnage}");
                    rawPartsTonnage = normalizedTonnage;
                }

                double chassisTonnage = 0;
                // Check for tags that influence the tonnage
                foreach (string chassisTag in cDef.ChassisTags) {
                    if (Mod.Config.PartsStorageMulti.ContainsKey(chassisTag)) {
                        float multi = Mod.Config.PartsStorageMulti[chassisTag];
                        chassisTonnage += rawPartsTonnage * multi;
                        Mod.Log.Debug($"  tag:{chassisTag} multi:{multi} yields:{rawPartsTonnage * multi}");
                    }
                }

                if (chassisTonnage == 0) {
                    chassisTonnage = rawPartsTonnage;
                    Mod.Log.Debug($"  No chassis multipliers found, defaulting to rawParts tonnage.");
                }

                allPartsTonnage += chassisTonnage;
            }

            Mod.Log.Debug($"Total tonnage from mech parts:{allPartsTonnage}");
            return allPartsTonnage;
        }

        public static int CalculateMechPartsCost(SimGameState sgs, double totalTonnage) {
            Mod.Log.Info($" === Calculating Mech Parts === ");

            double factoredTonnage = Math.Ceiling(totalTonnage * Mod.Config.PartsFactor);
            Mod.Log.Info($"  totalUnits:{totalTonnage} x factor:{Mod.Config.PartsFactor} = {factoredTonnage}");

            double scaledTonnage = Math.Pow(factoredTonnage, Mod.Config.PartsExponent);
            int totalCost = (int)(Mod.Config.PartsCostPerTon * scaledTonnage);
            Mod.Log.Info($"  scaledTonnage:{scaledTonnage} x costPerUnit:{Mod.Config.GearCostPerUnit} = {totalCost}");

            return (int)Math.Ceiling((double)totalCost);
        }

        public static List<KeyValuePair<string, int>> FilterActiveMechs(List<KeyValuePair<string, int>> keysAndValues, SimGameState sgs) {
            
            // Find active mechs
            List<string> mechNames = new List<string>();
            foreach (KeyValuePair<int, MechDef> entry in sgs.ActiveMechs) {
                MechDef mechDef = entry.Value;
                mechNames.Add(mechDef.Name);
                Mod.Log.Info($"SGCQSS:RD - excluding mech name:({mechDef.Name})");
            }

            List<KeyValuePair<string, int>> filteredList = new List<KeyValuePair<string, int>>();
            foreach (KeyValuePair<string, int> kvp in keysAndValues) {
                if (!mechNames.Contains(kvp.Key)) {
                    filteredList.Add(kvp);
                } else {
                    mechNames.Remove(kvp.Key);
                }
            }

            return filteredList;
        }

    }
}
