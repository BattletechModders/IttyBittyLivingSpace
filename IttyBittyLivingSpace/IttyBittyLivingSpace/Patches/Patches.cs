using BattleTech;
using BattleTech.UI;
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

            int activeMechCosts = Helper.CalculateActiveMechCosts(__instance);

            double gearInventorySize = Helper.GetGearInventorySize(__instance);
            int gearStorageCosts = Helper.CalculateGearCost(__instance, gearInventorySize);

            double mechPartsTonnage = Helper.GetMechPartsTonnage(__instance);
            int mechPartsStorageCost = Helper.CalculateMechPartsCost(__instance, mechPartsTonnage);

            __result = __result + activeMechCosts + gearStorageCosts + mechPartsStorageCost;
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
            List<KeyValuePair<string, int>> activeMechs = Helper.GetActiveMechLabels(___simState);
            filteredKeys.AddRange(activeMechs);

            double gearInventorySize = Helper.GetGearInventorySize(___simState);
            int gearStorageCost = Helper.CalculateGearCost(___simState, gearInventorySize);
            filteredKeys.Add(new KeyValuePair<string, int>($"WHSE: Gear ({gearInventorySize} units)", gearStorageCost));

            double mechPartsTonnage = Helper.GetMechPartsTonnage(___simState);
            int mechPartsStorageCost = Helper.CalculateMechPartsCost(___simState, mechPartsTonnage);
            filteredKeys.Add(new KeyValuePair<string, int>($"WHSE: Mech Parts ({mechPartsTonnage} tons)", mechPartsStorageCost));

            filteredKeys.Sort(new ExpensesSorter());

            Mod.Log.Info($"SGCQSS:RD - Clearing items");
            ClearListLineItems(___SectionOneExpensesList, ___simState);

            Mod.Log.Info($"SGCQSS:RD - Adding listLineItems");
            try {
                foreach (KeyValuePair<string, int> kvp in filteredKeys) {
                    Mod.Log.Info($"SGCQSS:RD - Adding key:{kvp.Key} value:{kvp.Value}");
                    AddListLineItem(___SectionOneExpensesList, ___simState, kvp.Key, SimGameState.GetCBillString(kvp.Value));
                }

            } catch (Exception e) {
                Mod.Log.Info($"SGCQSS:RD - failed to add lineItemParts due to: {e.Message}");
            }

            // Update summary costs
            string rawSectionOneCosts = ___SectionOneExpensesField.text;
            string sectionOneCostsS = Regex.Replace(rawSectionOneCosts, @"[^\d]", "");
            int sectionOneCosts = int.Parse(sectionOneCostsS);
            Mod.Log.Debug($"SGCQSS:RD raw costs:{rawSectionOneCosts} costsS:{sectionOneCostsS} sectionOneCosts:{sectionOneCosts}");

            int newCosts = sectionOneCosts + gearStorageCost + mechPartsStorageCost;
            string newCostsS = SimGameState.GetCBillString(newCosts);
            Mod.Log.Debug($"SGCQSS:RD - == COSTS == sectionOne:{sectionOneCosts} gear:{gearStorageCost} + parts:{mechPartsStorageCost} = {newCosts}");

            try {
                Traverse setFieldT = Traverse.Create(__instance).Method("SetField", new object[] { typeof(TextMeshProUGUI), typeof(string) });
                setFieldT.GetValue(new object[] { ___SectionOneExpensesField, SimGameState.GetCBillString(newCosts) });
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


}

