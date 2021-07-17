using BattleTech;
using BattleTech.UI.Tooltips;
using Harmony;
using Localize;
using System;
using TMPro;

namespace IttyBittyLivingSpace {

    [HarmonyPatch(typeof(TooltipPrefab_Chassis), "SetData")]
    [HarmonyAfter(new string[] { "us.frostraptor.IRUITweaks" })]
    public static class TooltipPrefab_Chassis_SetData {
        public static void Postfix(object data, TextMeshProUGUI ___descriptionText) {
            Mod.Log.Debug?.Write($"TP_C:SD - Init");
            if (data != null && ___descriptionText != null) {
                ChassisDef chassisDef = (ChassisDef)data;
                double storageTons = Helper.CalculateChassisTonnage(chassisDef);

                // Calculate total tonnage costs
                SimGameState sgs = UnityGameInstance.BattleTechGame.Simulation;
                double totalTonnage = Helper.CalculateTonnageForAllMechParts(sgs);

                int storageCost = 0;
                if (totalTonnage > 0) {
                    int totalCost = Helper.CalculateTotalForMechPartsCargo(sgs, totalTonnage);
                    double tonnageFraction = storageTons / totalTonnage;
                    storageCost = (int)Math.Ceiling(totalCost * tonnageFraction);
                } else {
                    double factoredTonnage = Math.Ceiling(storageTons * Mod.Config.PartsFactor);
                    double scaledTonnage = Math.Pow(factoredTonnage, Mod.Config.PartsExponent);
                    storageCost = (int)(Mod.Config.PartsCostPerTon * scaledTonnage);
                }

                string costLabel = new Text(Mod.LocalizedText.Tooltips[ModText.LT_Tooltip_Cargo_Chassis], 
                    new object[] { SimGameState.GetCBillString(storageCost), storageTons }).ToString();
                Text newDetails =  new Text(chassisDef.Description.Details + costLabel);
                Mod.Log.Debug?.Write($"  Setting details: {newDetails}u");
                ___descriptionText.SetText(newDetails.ToString());
            } else {
                Mod.Log.Debug?.Write($"TP_C:SD - Skipping");
            }
        }
    }

    [HarmonyPatch(typeof(TooltipPrefab_Equipment), "SetData")]
    [HarmonyAfter(new string[] { "us.frostraptor.IRUITweaks" })]
    public static class TooltipPrefab_Equipment_SetData {
        public static void Postfix(object data, TextMeshProUGUI ___detailText) {
            Mod.Log.Debug?.Write($"TP_E:SD - Init");
            SimGameState sgs = UnityGameInstance.BattleTechGame.Simulation;
            if (data != null && ___detailText != null && sgs != null) {

                // Calculate total gear storage size
                MechComponentDef mcDef = (MechComponentDef)data;
                float componentStorageSize = Helper.CalculateGearStorageSize(mcDef);
                double totalSize = Helper.GetGearInventorySize(sgs);

                int storageCost = 0;
                if (totalSize > 0) {
                    // Handle exponentiation of cost
                    int totalCost = Helper.CalculateTotalForGearCargo(sgs, totalSize);

                    double sizeFraction = componentStorageSize / totalSize;
                    storageCost = (int)Math.Ceiling(totalCost * sizeFraction);
                    Mod.Log.Debug?.Write($"    totalCost: {totalCost}  storageSize: {componentStorageSize}  sizeFraction: {sizeFraction}  fractionalCost: {storageCost}");
                } else {
                    // Assume no exponentiation when there is no gear
                    double factoredSize = Math.Ceiling(componentStorageSize * Mod.Config.GearFactor);
                    double scaledUnits = Math.Pow(factoredSize, Mod.Config.GearExponent);
                    storageCost = (int)(Mod.Config.GearCostPerUnit * scaledUnits);
                    Mod.Log.Info?.Write($"  totalUnits:{componentStorageSize} x factor:{Mod.Config.GearFactor} = {factoredSize}");
                }

                string costLabel = new Text(Mod.LocalizedText.Tooltips[ModText.LT_Tooltip_Cargo_Chassis], 
                    new object[] { SimGameState.GetCBillString(storageCost), componentStorageSize }).ToString();
                Text newDetails = new Text(mcDef.Description.Details + costLabel);
                Mod.Log.Debug?.Write($"  Setting details: {newDetails}u");
                ___detailText.SetText(newDetails.ToString());
            } else {
                Mod.Log.Debug?.Write($"TP_E:SD - Skipping");
            }
        }
    }

    [HarmonyPatch(typeof(TooltipPrefab_Weapon), "SetData")]
    [HarmonyAfter(new string[] { "us.frostraptor.IRUITweaks" })]
    public static class TooltipPrefab_Weapon_SetData {
        public static void Postfix(object data, TextMeshProUGUI ___body) {
            Mod.Log.Debug?.Write($"TP_W:SD - Init - data:{data} body:{___body}");
            SimGameState sgs = UnityGameInstance.BattleTechGame.Simulation;
            if (data != null && ___body != null && sgs != null) {
                WeaponDef weaponDef = (WeaponDef)data;
                float weaponStorageSize = Helper.CalculateGearStorageSize(weaponDef);

                // Calculate total gear storage size
                double totalSize = Helper.GetGearInventorySize(sgs);

                int storageCost = 0;
                if (totalSize > 0) {
                    // Handle exponentiation of cost
                    int totalCost = Helper.CalculateTotalForGearCargo(sgs, totalSize);
                    double sizeFraction = weaponStorageSize / totalSize;
                    storageCost = (int)Math.Ceiling(totalCost * sizeFraction);
                    Mod.Log.Debug?.Write($"    totalCost: {totalCost}  storageSize: {weaponStorageSize}  sizeFraction: {sizeFraction}  fractionalCost: {storageCost}");
                } else {
                    // Assume no exponentiation when there is no gear
                    double factoredSize = Math.Ceiling(weaponStorageSize * Mod.Config.GearFactor);
                    double scaledUnits = Math.Pow(factoredSize, Mod.Config.GearExponent);
                    storageCost = (int)(Mod.Config.GearCostPerUnit * scaledUnits);
                    Mod.Log.Info?.Write($"  totalUnits:{weaponStorageSize} x factor:{Mod.Config.GearFactor} = {factoredSize}");
                }

                string costLabel = new Text(Mod.LocalizedText.Tooltips[ModText.LT_Tooltip_Cargo_Chassis],
                    new object[] { SimGameState.GetCBillString(storageCost), weaponStorageSize }).ToString();
                Text newDetails = new Text(weaponDef.Description.Details + costLabel);
                Mod.Log.Debug?.Write($"  Setting details: {newDetails}u");
                ___body.SetText(newDetails.ToString());
            } else {
                Mod.Log.Debug?.Write($"TP_W:SD - Skipping");
            }
        }
    }
}

