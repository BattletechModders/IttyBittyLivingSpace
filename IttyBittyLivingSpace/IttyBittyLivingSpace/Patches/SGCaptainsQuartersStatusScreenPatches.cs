using BattleTech;
using BattleTech.UI;
using Harmony;
using Localize;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace IttyBittyLivingSpace.Patches
{
    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
    [HarmonyAfter(new string[] { "de.morphyum.MechMaintenanceByCost", "dZ.Zappo.MonthlyTechAdjustment" })]
    public static class SGCaptainsQuartersStatusScreen_RefreshData
    {
        public static void Postfix(SGCaptainsQuartersStatusScreen __instance, EconomyScale expenditureLevel, bool showMoraleChange,
            Transform ___SectionOneExpensesList, TextMeshProUGUI ___SectionOneExpensesField, SimGameState ___simState)
        {

            SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;
            if (__instance == null || ___SectionOneExpensesList == null || ___SectionOneExpensesField == null || simGameState == null)
            {
                Mod.Log.Info?.Write($"SGCQSS:RD - skipping");
                return;
            }

            // TODO: Add this to mech parts maybe?
            //float expenditureCostModifier = simGameState.GetExpenditureCostModifier(expenditureLevel);

            Mod.Log.Info?.Write($"SGCQSS:RD - entered. Parsing current keys.");

            List<KeyValuePair<string, int>> currentKeys = GetCurrentKeys(___SectionOneExpensesList, ___simState);
            // Extract the active mechs from the list, then re-add the updated price
            List<KeyValuePair<string, int>> filteredKeys = Helper.FilterActiveMechs(currentKeys, ___simState);
            List<KeyValuePair<string, int>> activeMechs = Helper.GetUpkeepLabels(___simState);
            filteredKeys.AddRange(activeMechs);

            // Add the new costs
            int activeMechCosts = Helper.CalculateTotalForUpkeep(___simState);

            double gearInventorySize = Helper.GetGearInventorySize(___simState);
            int gearStorageCost = Helper.CalculateTotalForGearCargo(___simState, gearInventorySize);
            string gearLabel = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Cargo_Gear], new object[] { gearInventorySize }).ToString();
            filteredKeys.Add(new KeyValuePair<string, int>(gearLabel, gearStorageCost));

            double mechPartsTonnage = Helper.CalculateTonnageForAllMechParts(___simState);
            int mechPartsStorageCost = Helper.CalculateTotalForMechPartsCargo(___simState, mechPartsTonnage);
            string mechPartsLabel = new Text(Mod.LocalizedText.Labels[ModText.LT_Label_Cargo_Mech_Parts], new object[] { mechPartsTonnage }).ToString();
            filteredKeys.Add(new KeyValuePair<string, int>(mechPartsLabel, mechPartsStorageCost));

            filteredKeys.Sort(new ExpensesSorter());

            Mod.Log.Info?.Write($"SGCQSS:RD - Clearing items");
            ClearListLineItems(___SectionOneExpensesList, ___simState);

            Mod.Log.Info?.Write($"SGCQSS:RD - Adding listLineItems");
            int totalCost = 0;
            try
            {
                foreach (KeyValuePair<string, int> kvp in filteredKeys)
                {
                    Mod.Log.Info?.Write($"SGCQSS:RD - Adding key: '{kvp.Key}' value: '{kvp.Value}'");
                    totalCost += kvp.Value;
                    AddListLineItem(___SectionOneExpensesList, ___simState, kvp.Key, SimGameState.GetCBillString(kvp.Value));
                }

            }
            catch (Exception e)
            {
                Mod.Log.Info?.Write($"SGCQSS:RD - failed to add lineItemParts due to: {e.Message}");
            }

            // Update summary costs
            int newCosts = totalCost;
            string newCostsS = SimGameState.GetCBillString(newCosts);
            Mod.Log.Debug?.Write($"SGCQSS:RD - total:{newCosts} = activeMechs:{activeMechCosts} + gearStorage:{gearStorageCost} + partsStorage:{mechPartsStorageCost}");

            try
            {
                ___SectionOneExpensesField.SetText(SimGameState.GetCBillString(newCosts));
                Mod.Log.Debug?.Write($"SGCQSS:RD - updated ");
            }
            catch (Exception e)
            {
                Mod.Log.Info?.Write($"SGCQSS:RD - failed to update summary costs section due to: {e.Message}");
            }
        }

        public static List<KeyValuePair<string, int>> GetCurrentKeys(Transform container, SimGameState sgs)
        {

            List<KeyValuePair<string, int>> currentKeys = new List<KeyValuePair<string, int>>();
            IEnumerator enumerator = container.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    object obj = enumerator.Current;
                    Transform transform = (Transform)obj;
                    SGKeyValueView component = transform.gameObject.GetComponent<SGKeyValueView>();

                    Mod.Log.Debug?.Write($"SGCQSS:RD - Reading key from component:{component.name}.");
                    Traverse keyT = Traverse.Create(component).Field("Key");
                    TextMeshProUGUI keyText = (TextMeshProUGUI)keyT.GetValue();
                    string key = keyText.text;
                    Mod.Log.Debug?.Write($"SGCQSS:RD - key found as: {key}");

                    Traverse valueT = Traverse.Create(component).Field("Value");
                    TextMeshProUGUI valueText = (TextMeshProUGUI)valueT.GetValue();
                    string valueS = valueText.text;
                    string digits = Regex.Replace(valueS, @"[^\d]", "");
                    Mod.Log.Debug?.Write($"SGCQSS:RD - rawValue:{valueS} digits:{digits}");
                    int value = Int32.Parse(digits);

                    Mod.Log.Debug?.Write($"SGCQSS:RD - found existing pair: {key} / {value}");
                    KeyValuePair<string, int> kvp = new KeyValuePair<string, int>(key, value);
                    currentKeys.Add(kvp);

                }
            }
            catch (Exception e)
            {
                Mod.Log.Info?.Write($"Failed to get key-value pairs: {e.Message}");
            }

            return currentKeys;
        }

        private static void AddListLineItem(Transform list, SimGameState sgs, string key, string value)
        {
            GameObject gameObject = sgs.DataManager.PooledInstantiate("uixPrfPanl_captainsQuarters_quarterlyReportLineItem-element",
                BattleTechResourceType.UIModulePrefabs, null, null, list);
            SGKeyValueView component = gameObject.GetComponent<SGKeyValueView>();
            gameObject.transform.localScale = Vector3.one;
            component.SetData(key, value);
        }

        private static void ClearListLineItems(Transform container, SimGameState sgs)
        {
            List<GameObject> list = new List<GameObject>();
            IEnumerator enumerator = container.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    object obj = enumerator.Current;
                    Transform transform = (Transform)obj;
                    list.Add(transform.gameObject);
                }
            }
            finally
            {
                IDisposable disposable;
                if ((disposable = (enumerator as IDisposable)) != null)
                {
                    disposable.Dispose();
                }
            }
            while (list.Count > 0)
            {
                GameObject gameObject = list[0];
                sgs.DataManager.PoolGameObject("uixPrfPanl_captainsQuarters_quarterlyReportLineItem-element", gameObject);
                list.Remove(gameObject);
            }
        }
    }
}
