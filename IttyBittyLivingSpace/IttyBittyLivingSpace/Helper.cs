
using BattleTech;
using CustomComponents;
using System;
using System.Collections.Generic;

namespace IttyBittyLivingSpace {

    [CustomComponent("IBLS")]
    public class IBLS : SimpleCustomComponent {
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

            int totalCost = 0;
            foreach (KeyValuePair<int, MechDef> entry in sgs.ActiveMechs) {
                totalCost += CalculateMechCost(entry.Value);
            }

            Mod.Log.Info($" Total cost for active mechs:{totalCost}");
            return totalCost;
        }

        public static List<KeyValuePair<string, int>> GetActiveMechLabels(SimGameState sgs) {
            Mod.Log.Info($" === Calculating Active Mech Labels === ");

            List<KeyValuePair<string, int>> labels = new List<KeyValuePair<string, int>>();
            foreach (KeyValuePair<int, MechDef> entry in sgs.ActiveMechs) {
                MechDef mechDef = entry.Value;
                int mechCost = CalculateMechCost(mechDef);
                Mod.Log.Debug($"  Adding mech:{mechDef.Name} with cost:{mechCost}");
                labels.Add(new KeyValuePair<string, int>("MECH: " + mechDef.Name, mechCost));
            }

            return labels;
        }

        private static int CalculateMechCost(MechDef mechDef) {
            if (mechDef == null || mechDef.Description == null) {
                Mod.Log.Debug($"  MechDef or mechDef description is null, skipping.");
            }

            Mod.Log.Info($"  Active Mech found with item: {mechDef.Description.Id}");

            // Calculate any chassis multipliers
            double tagsMultiplier = 1.0;
            if (mechDef.Chassis != null && mechDef.Chassis.ChassisTags != null) {
                foreach (string chassisTag in mechDef.Chassis.ChassisTags) {
                    if (chassisTag == null) {
                        Mod.Log.Debug($"  tag:({chassisTag}) skipping");
                        continue;
                    } else {
                        Mod.Log.Debug($"  processing tag:({chassisTag}) ");
                    }

                    if (Mod.Config.UpkeepChassisTagMultis.ContainsKey(chassisTag)) {
                        tagsMultiplier += Mod.Config.UpkeepChassisTagMultis[chassisTag];
                        Mod.Log.Debug($"  tag:{chassisTag} adding multi:{Mod.Config.UpkeepChassisTagMultis[chassisTag]}");
                    }
                }
            } else {
                Mod.Log.Debug($"  chassis has no chassisTags");
            }

            int chassisCost = (int)Math.Ceiling(mechDef.Description.Cost * Mod.Config.UpkeepChassisMulti * tagsMultiplier);
            Mod.Log.Info($" chassisCost:{chassisCost} = rawCost:{mechDef.Description.Cost} x {Mod.Config.UpkeepChassisMulti} x {tagsMultiplier}");

            // Calculate any component costs
            int componentCost = 0;
            foreach (MechComponentRef mcRef in mechDef.Inventory) {
                string compName = mcRef.Def.Description.Name;
                int compRawCost = mcRef.Def.Description.Cost;

                int modifiedCost = 0;
                if (mcRef.Is<IBLS>(out IBLS ibls)) {
                    Mod.Log.Debug($"  Override multiplier:{ibls.CostMulti} instead of:{Mod.Config.UpkeepGearMulti}");
                    modifiedCost = (int)Math.Ceiling(compRawCost * ibls.CostMulti);
                } else {
                    modifiedCost = (int)Math.Ceiling(compRawCost * Mod.Config.UpkeepGearMulti);
                }

                Mod.Log.Debug($"  component:{compName} rawCost:{compRawCost} modifiedCost:{modifiedCost}");
                componentCost += modifiedCost;
            }

            int upkeepCost = chassisCost + componentCost;
            Mod.Log.Info($" upkeepCost:{upkeepCost} = chassisCost:{chassisCost} + componentCost:{componentCost}");

            SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;
            Statistic upkeepStat = simGameState.CompanyStats.GetStatistic(ModConfig.SC_Upkeep);
            if (upkeepStat != null) {
                try {
                    float statMulti = upkeepStat.Value<float>();
                    Mod.Log.Debug($" Statistic {ModConfig.SC_Upkeep} has multi:{statMulti}");
                    float modifiedCosts = upkeepCost * statMulti;
                    Mod.Log.Info($" Statistic modified cost:{modifiedCosts} = statMulti:{statMulti} x upkeepCost:{upkeepCost}");
                    upkeepCost = (int)Math.Ceiling(modifiedCosts);
                } catch (Exception e) {
                    Mod.Log.Info($"Failed to read {ModConfig.SC_Upkeep} due to exception:{e.Message}");
                }
            }

            return upkeepCost;
        }

        public static float GetGearInventorySize(SimGameState sgs) {
            float totalUnits = 0.0f;
            foreach (MechComponentRef mcRef in sgs.GetAllInventoryItemDefs()) {
                int itemCount = sgs.GetItemCount(mcRef.Def.Description, mcRef.Def.GetType(), sgs.GetItemCountDamageType(mcRef));
                float itemSize = CalculateGearStorageSize(mcRef.Def);
                Mod.Log.Debug($"  Inventory item:({mcRef.Def.Description.Id}) size:{itemSize} qty:{itemCount}");
                totalUnits += itemSize;
            }

            Mod.Log.Debug($"  Total storage units: {totalUnits}u");
            return totalUnits;
        }

        public static float CalculateGearStorageSize(MechComponentDef mcDef) {
            
            string itemId = mcDef.Description.Id;
            float itemSize = mcDef.InventorySize;
            Mod.Log.Debug($"  Inventory item:({mcDef.Description.Id}) size:{mcDef.InventorySize}");

            if (mcDef.Is<IBLS>(out IBLS ibls)) {
                Mod.Log.Debug($"  Overriding size:{ibls.StorageSize} instead of:{mcDef.InventorySize}");
                itemSize = ibls.StorageSize;
            }

            return itemSize;
        }

        public static int CalculateGearCost(SimGameState sgs, double totalUnits) {
            Mod.Log.Debug($" === Calculating Gear Costs === ");

            double factoredSize = Math.Ceiling(totalUnits * Mod.Config.GearFactor);
            Mod.Log.Info($"  totalUnits:{totalUnits} x factor:{Mod.Config.GearFactor} = {factoredSize}");

            double scaledUnits = Math.Pow(factoredSize, Mod.Config.GearExponent);
            int totalCost = (int)(Mod.Config.GearCostPerUnit * scaledUnits);
            Mod.Log.Info($"  scaledUnits:{scaledUnits} x costPerUnit:{Mod.Config.GearCostPerUnit} = {totalCost}");

            SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;
            Statistic upkeepStat = simGameState.CompanyStats.GetStatistic(ModConfig.SC_Cargo);
            if (upkeepStat != null) {
                try {
                    float statMulti = upkeepStat.Value<float>();
                    Mod.Log.Debug($" Statistic {ModConfig.SC_Cargo} has multi:{statMulti}");
                    float modifiedCosts = totalCost * statMulti;
                    Mod.Log.Info($" Statistic modified cost:{modifiedCosts} = statMulti:{statMulti} x totalCosts:{totalCost}");
                    totalCost = (int)Math.Ceiling(modifiedCosts);
                } catch (Exception e) {
                    Mod.Log.Info($"Failed to read {ModConfig.SC_Cargo} due to exception:{e.Message}");
                }
            }

            return (int)Math.Ceiling((double)totalCost);
        }

        public static double GetMechPartsTonnage(SimGameState sgs) {
            double allPartsTonnage = 0;
            foreach (ChassisDef cDef in sgs.GetAllInventoryMechDefs(true)) {

                double chassisTonnage = CalculateChassisTonnage(cDef);
                allPartsTonnage += chassisTonnage;
            }

            Mod.Log.Debug($"Total tonnage from mech parts:{allPartsTonnage}");
            return allPartsTonnage;
        }

        public static double CalculateChassisTonnage(ChassisDef cDef) {
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
            if (cDef.ChassisTags != null) {
                foreach (string chassisTag in cDef.ChassisTags) {
                    if (chassisTag == null) {
                        Mod.Log.Debug($"  tag:({chassisTag}) skipping");
                        continue;
                    } else {
                        Mod.Log.Debug($"  processing tag:({chassisTag}) ");
                    }

                    if (Mod.Config.PartsStorageTagMultis.ContainsKey(chassisTag)) {
                        float multi = Mod.Config.PartsStorageTagMultis[chassisTag];
                        chassisTonnage += rawPartsTonnage * multi;
                        Mod.Log.Debug($"  tag:{chassisTag} multi:{multi} yields:{rawPartsTonnage * multi}");
                    }
                }
            } else {
                Mod.Log.Debug($"  chassis has null chassisTags... skipping.");
            }

            if (chassisTonnage == 0) {
                chassisTonnage = rawPartsTonnage;
                Mod.Log.Debug($"  No chassis multipliers found, defaulting to rawParts tonnage.");
            }

            return Math.Ceiling(chassisTonnage);
        }

        public static int CalculateMechPartsCost(SimGameState sgs, double totalTonnage) {
            Mod.Log.Debug($" === Calculating Mech Parts === ");

            double factoredTonnage = Math.Ceiling(totalTonnage * Mod.Config.PartsFactor);
            Mod.Log.Debug($"  factoredTonnage:{factoredTonnage} = totalTonnage:{totalTonnage} x factor:{Mod.Config.PartsFactor}");

            double scaledTonnage = Math.Pow(factoredTonnage, Mod.Config.PartsExponent);
            Mod.Log.Debug($"  scaledTonnage:{scaledTonnage} = factoredTonnage:{factoredTonnage} ^ partsExponent:{Mod.Config.PartsExponent}");

            int totalCost = (int)(Mod.Config.PartsCostPerTon * scaledTonnage);
            Mod.Log.Info($"  totalCost:{totalCost} = scaledTonnage:{scaledTonnage} x costPerTon:{Mod.Config.PartsCostPerTon}");

            SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;
            Statistic upkeepStat = simGameState.CompanyStats.GetStatistic(ModConfig.SC_Cargo);
            if (upkeepStat != null) {
                try {
                    float statMulti = upkeepStat.Value<float>();
                    Mod.Log.Debug($" Statistic {ModConfig.SC_Cargo} has multi:{statMulti}");
                    float modifiedCosts = totalCost * statMulti;
                    Mod.Log.Info($" Statistic modified cost:{modifiedCosts} = statMulti:{statMulti} x totalCosts:{totalCost}");
                    totalCost = (int)Math.Ceiling(modifiedCosts);
                } catch (Exception e) {
                    Mod.Log.Info($"Failed to read {ModConfig.SC_Cargo} due to exception:{e.Message}");
                }
            }

            return (int)Math.Ceiling((double)totalCost);
        }

        public static List<KeyValuePair<string, int>> FilterActiveMechs(List<KeyValuePair<string, int>> keysAndValues, SimGameState sgs) {
            
            // Find active mechs
            List<string> mechNames = new List<string>();
            foreach (KeyValuePair<int, MechDef> entry in sgs.ActiveMechs) {
                MechDef mechDef = entry.Value;
                mechNames.Add(mechDef.Name);
                Mod.Log.Debug($"SGCQSS:RD - excluding mech name:({mechDef.Name})");
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
