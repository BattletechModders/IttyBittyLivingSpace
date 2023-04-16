using IRBTModUtils.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace IttyBittyLivingSpace
{

    public static class Mod
    {

        public const string HarmonyPackage = "us.frostraptor.IttyBittyLivingSpace";
        public const string LogName = "itty_bitty_living_space";
        public const string LogLabel = "IBLS";

        public static readonly Random Random = new Random();

        public static DeferringLogger Log;

        public static string ModDir;
        public static ModConfig Config;
        public static ModText LocalizedText;

        public static void Init(string modDirectory, string settingsJSON)
        {
            Mod.ModDir = modDirectory;

            Exception settingsE = null;
            try
            {
                Mod.Config = JsonConvert.DeserializeObject<ModConfig>(settingsJSON);
            }
            catch (Exception e)
            {
                settingsE = e;
                Mod.Config = new ModConfig();
            }

            Mod.Log = new DeferringLogger(modDirectory, LogName, LogLabel, Config.Debug, Config.Trace);

            // Read localization
            string localizationPath = Path.Combine(ModDir, "./mod_localized_text.json");
            try
            {
                string jsonS = File.ReadAllText(localizationPath);
                Mod.LocalizedText = JsonConvert.DeserializeObject<ModText>(jsonS);
            }
            catch (Exception e)
            {
                Mod.LocalizedText = new ModText();
                Log.Error?.Write(e, $"Failed to read localizations from: {localizationPath} due to error!");
            }
            Mod.LocalizedText.InitUnsetValues();

            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            Mod.Log.Info?.Write($"Assembly version: {fvi.ProductVersion}");

            Mod.Log.Debug?.Write($"ModDir is:{modDirectory}");
            Mod.Log.Debug?.Write($"mod.json settings are:({settingsJSON})");
            Mod.Config.LogConfig();

            if (settingsE != null)
            {
                Mod.Log.Info?.Write($"ERROR reading settings file! Error was: {settingsE}");
            }
            else
            {
                Mod.Log.Info?.Write($"INFO: No errors reading settings file.");
            }

            // Initialize custom components
            CustomComponents.Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());

            // Initialize harmony patches
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), HarmonyPackage);
        }

    }

}
