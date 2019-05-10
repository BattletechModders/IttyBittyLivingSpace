using BattleTech;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace IttyBittyLivingSpace {

    [HarmonyPatch(typeof(SimGameState), "GetExpenditures")]
    [HarmonyAfter(new string[] { "de.morphyum.MechMaintenanceByCost" })]
    public static class SimGameState_GetExpenditures {
        public static void Postfix(SimGameState __instance, ref int __result, bool proRate) {
            Mod.Log.Info($"SGS:GE entered with {__result}");

            // Subtract the base cost of mechs
            float expenditureCostModifier = __instance.GetExpenditureCostModifier(__instance.ExpenditureLevel);
            int defaultMechCosts = 0;
            foreach (MechDef mechDef in __instance.ActiveMechs.Values) {
                defaultMechCosts += Mathf.RoundToInt(expenditureCostModifier * (float)__instance.Constants.Finances.MechCostPerQuarter);
            }

            // Add the new costs
            int activeMechCosts = Helper.CalculateTotalForUpkeep(__instance);

            double gearInventorySize = Helper.GetGearInventorySize(__instance);
            int gearStorageCosts = Helper.CalculateTotalForGearCargo(__instance, gearInventorySize);

            double mechPartsTonnage = Helper.CalculateTonnageForAllMechParts(__instance);
            int mechPartsStorageCost = Helper.CalculateTotalForMechPartsCargo(__instance, mechPartsTonnage);

            int total = __result - defaultMechCosts + activeMechCosts + gearStorageCosts + mechPartsStorageCost;
            Mod.Log.Info($"SGS:GE - total:{total} ==> result:{__result} - defaultMechCosts:{defaultMechCosts} = {__result - defaultMechCosts} + activeMechs:{activeMechCosts} + gearStorage:{gearStorageCosts} + partsStorage:{mechPartsStorageCost}");
            __result = total;
        }
    }

    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
    [HarmonyAfter(new string[] { "de.morphyum.MechMaintenanceByCost", "dZ.Zappo.MonthlyTechAdjustment" })]
    public static class SGCaptainsQuartersStatusScreen_RefreshData {
        public static void Postfix(SGCaptainsQuartersStatusScreen __instance, bool showMoraleChange,
            Transform ___SectionOneExpensesList, TextMeshProUGUI ___SectionOneExpensesField, SimGameState ___simState) {

            SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;
            if (__instance == null || ___SectionOneExpensesList == null || ___SectionOneExpensesField == null || simGameState == null) {
                Mod.Log.Info($"SGCQSS:RD - skipping");
                return;
            }

            Mod.Log.Info($"SGCQSS:RD - entered. Parsing current keys.");
            List<KeyValuePair<string, int>> currentKeys = GetCurrentKeys(___SectionOneExpensesList, ___simState);

            // Extract the active mechs from the list, then re-add the updated price
            List<KeyValuePair<string, int>> filteredKeys = Helper.FilterActiveMechs(currentKeys, ___simState);
            List<KeyValuePair<string, int>> activeMechs = Helper.GetUpkeepLabels(___simState);
            filteredKeys.AddRange(activeMechs);

            // Add the new costs
            int activeMechCosts = Helper.CalculateTotalForUpkeep(___simState);

            double gearInventorySize = Helper.GetGearInventorySize(___simState);
            int gearStorageCost = Helper.CalculateTotalForGearCargo(___simState, gearInventorySize);
            filteredKeys.Add(new KeyValuePair<string, int>($"WHSE: Gear ({gearInventorySize} units)", gearStorageCost));

            double mechPartsTonnage = Helper.CalculateTonnageForAllMechParts(___simState);
            int mechPartsStorageCost = Helper.CalculateTotalForMechPartsCargo(___simState, mechPartsTonnage);
            filteredKeys.Add(new KeyValuePair<string, int>($"WHSE: Mech Parts ({mechPartsTonnage} tons)", mechPartsStorageCost));

            filteredKeys.Sort(new ExpensesSorter());

            Mod.Log.Info($"SGCQSS:RD - Clearing items");
            ClearListLineItems(___SectionOneExpensesList, ___simState);

            Mod.Log.Info($"SGCQSS:RD - Adding listLineItems");
            int totalCost = 0;
            try {
                foreach (KeyValuePair<string, int> kvp in filteredKeys) {
                    Mod.Log.Info($"SGCQSS:RD - Adding key:{kvp.Key} value:{kvp.Value}");
                    totalCost += kvp.Value;
                    AddListLineItem(___SectionOneExpensesList, ___simState, kvp.Key, SimGameState.GetCBillString(kvp.Value));
                }

            } catch (Exception e) {
                Mod.Log.Info($"SGCQSS:RD - failed to add lineItemParts due to: {e.Message}");
            }

            // Update summary costs
            int newCosts = totalCost;
            string newCostsS = SimGameState.GetCBillString(newCosts);
            Mod.Log.Debug($"SGCQSS:RD - total:{newCosts} = activeMechs:{activeMechCosts} + gearStorage:{gearStorageCost} + partsStorage:{mechPartsStorageCost}");

            try {
                ___SectionOneExpensesField.SetText(SimGameState.GetCBillString(newCosts));
                Mod.Log.Debug($"SGCQSS:RD - updated ");
            } catch (Exception e) {
                Mod.Log.Info($"SGCQSS:RD - failed to update summary costs section due to: {e.Message}");
            }
        }

        public static List<KeyValuePair<string, int>> GetCurrentKeys(Transform container, SimGameState sgs) {

            List<KeyValuePair<string, int>> currentKeys = new List<KeyValuePair<string, int>>();
            IEnumerator enumerator = container.GetEnumerator();
            try {
                while (enumerator.MoveNext()) {
                    object obj = enumerator.Current;
                    Transform transform = (Transform)obj;
                    SGKeyValueView component = transform.gameObject.GetComponent<SGKeyValueView>();

                    Mod.Log.Debug($"SGCQSS:RD - Reading key from component:{component.name}.");
                    Traverse keyT = Traverse.Create(component).Field("Key");
                    TextMeshProUGUI keyText = (TextMeshProUGUI)keyT.GetValue();
                    string key = keyText.text;
                    Mod.Log.Debug($"SGCQSS:RD - key found as: {key}");

                    Traverse valueT = Traverse.Create(component).Field("Value");
                    TextMeshProUGUI valueText = (TextMeshProUGUI)valueT.GetValue();
                    string valueS = valueText.text;
                    string digits = Regex.Replace(valueS, @"[^\d]", "");
                    Mod.Log.Debug($"SGCQSS:RD - rawValue:{valueS} digits:{digits}");
                    int value = Int32.Parse(digits);

                    Mod.Log.Debug($"SGCQSS:RD - found existing pair: {key} / {value}");
                    KeyValuePair<string, int> kvp = new KeyValuePair<string, int>(key, value);
                    currentKeys.Add(kvp);

                }
            } catch (Exception e) {
                Mod.Log.Info($"Failed to get key-value pairs: {e.Message}");
            }

            return currentKeys;
        }

        private static void AddListLineItem(Transform list, SimGameState sgs, string key, string value) {
            GameObject gameObject = sgs.DataManager.PooledInstantiate("uixPrfPanl_captainsQuarters_quarterlyReportLineItem-element", 
                BattleTechResourceType.UIModulePrefabs, null, null, list);
            SGKeyValueView component = gameObject.GetComponent<SGKeyValueView>();
            gameObject.transform.localScale = Vector3.one;
            component.SetData(key, value);
        }

        private static void ClearListLineItems(Transform container, SimGameState sgs) {
            List<GameObject> list = new List<GameObject>();
            IEnumerator enumerator = container.GetEnumerator();
            try {
                while (enumerator.MoveNext()) {
                    object obj = enumerator.Current;
                    Transform transform = (Transform)obj;
                    list.Add(transform.gameObject);
                }
            } finally {
                IDisposable disposable;
                if ((disposable = (enumerator as IDisposable)) != null) {
                    disposable.Dispose();
                }
            }
            while (list.Count > 0) {
                GameObject gameObject = list[0];
                sgs.DataManager.PoolGameObject("uixPrfPanl_captainsQuarters_quarterlyReportLineItem-element", gameObject);
                list.Remove(gameObject);
            }
        }
    }

    [HarmonyPatch(typeof(TooltipPrefab_Chassis), "SetData")]
    [HarmonyAfter(new string[] { "us.frostraptor.IRUITweaks" })]
    public static class TooltipPrefab_Chassis_SetData {
        public static void Postfix(TooltipPrefab_Chassis __instance, object data, TextMeshProUGUI ___descriptionText) {
            Mod.Log.Debug($"TP_C:SD - Init");
            if (data != null && ___descriptionText != null) {
                ChassisDef chassisDef = (ChassisDef)data;

                // Calculate total tonnage costs
                SimGameState sgs = UnityGameInstance.BattleTechGame.Simulation;
                double totalTonnage = Helper.CalculateTonnageForAllMechParts(sgs);
                int totalCost = Helper.CalculateTotalForMechPartsCargo(sgs, totalTonnage);

                double storageTons = Helper.CalculateChassisTonnage(chassisDef);
                double tonnageFraction = storageTons / totalTonnage;
                int fractionalCost = (int)Math.Ceiling(totalCost * tonnageFraction);

                string newDetails = chassisDef.Description.Details + $"\n\n<color=#FF0000>Cargo Cost:{SimGameState.GetCBillString(fractionalCost)} from {storageTons} t</color>";
                Mod.Log.Debug($"  Setting details: {newDetails}u");
                ___descriptionText.SetText(newDetails, new object[0]);
            } else {
                Mod.Log.Debug($"TP_C:SD - Skipping");
            }
        }
    }

    [HarmonyPatch(typeof(TooltipPrefab_Equipment), "SetData")]
    [HarmonyAfter(new string[] { "us.frostraptor.IRUITweaks" })]
    public static class TooltipPrefab_Equipment_SetData {
        public static void Postfix(TooltipPrefab_Equipment __instance, object data, TextMeshProUGUI ___detailText) {
            Mod.Log.Debug($"TP_E:SD - Init");
            if (data != null && ___detailText != null) {
                MechComponentDef mcDef = (MechComponentDef)data;

                // Calculate total gear storage size
                SimGameState sgs = UnityGameInstance.BattleTechGame.Simulation;
                double totalSize = Helper.GetGearInventorySize(sgs);
                int totalCost = Helper.CalculateTotalForGearCargo(sgs, totalSize);

                float storageSize = Helper.CalculateGearStorageSize(mcDef);
                double sizeFraction = storageSize / totalSize;
                int fractionalCost = (int)Math.Ceiling(totalCost * sizeFraction);

                string newDetails = mcDef.Description.Details + $"\n\n<color=#FF0000>Cargo Cost:{SimGameState.GetCBillString(fractionalCost)} from size:{storageSize}u</color>";
                Mod.Log.Debug($"  Setting details: {newDetails}u");
                ___detailText.SetText(newDetails, new object[0]);
            } else {
                Mod.Log.Debug($"TP_E:SD - Skipping");
            }
        }
    }

    [HarmonyPatch(typeof(TooltipPrefab_Weapon), "SetData")]
    [HarmonyAfter(new string[] { "us.frostraptor.IRUITweaks" })]
    public static class TooltipPrefab_Weapon_SetData {
        public static void Postfix(TooltipPrefab_Weapon __instance, object data, TextMeshProUGUI ___body) {
            Mod.Log.Debug($"TP_W:SD - Init - data:{data} body:{___body}");
            if (data != null && ___body != null) {
                WeaponDef weaponDef = (WeaponDef)data;

                float storageSize = Helper.CalculateGearStorageSize(weaponDef);
                string newDetails = weaponDef.Description.Details + $"\n\n<color=#FF0000>Storage Size: {storageSize}</color>";
                Mod.Log.Debug($"  Setting details: {newDetails}u");
                ___body.SetText(newDetails, new object[0]);
            } else {
                Mod.Log.Debug($"TP_W:SD - Skipping");
            }
        }
    }
}

