using Harmony;
using IRBTModUtils.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Reflection;

namespace IttyBittyLivingSpace {

    public static class Mod {
        public static DeferringLogger Log;
        public static string ModDir;
        public static ModConfig Config;
    }

    public static class ModInit {

        public const string HarmonyPackage = "us.frostraptor.IttyBittyLivingSpace";
        public const string LogName = "itty_bitty_living_space";

        public static readonly Random Random = new Random();

        public static void Init(string modDirectory, string settingsJSON) {
            Mod.ModDir = modDirectory;

            Exception settingsE = null;
            try {
                Mod.Config = JsonConvert.DeserializeObject<ModConfig>(settingsJSON);
            } catch (Exception e) {
                settingsE = e;
                Mod.Config = new ModConfig();
            }

            Mod.Log = new DeferringLogger(modDirectory, LogName, Mod.Config.Debug, Mod.Config.Trace);

            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            Mod.Log.Info?.Write($"Assembly version: {fvi.ProductVersion}");

            Mod.Log.Debug?.Write($"ModDir is:{modDirectory}");
            Mod.Log.Debug?.Write($"mod.json settings are:({settingsJSON})");
            Mod.Config.LogConfig();

            if (settingsE != null) {
                Mod.Log.Info?.Write($"ERROR reading settings file! Error was: {settingsE}");
            } else {
                Mod.Log.Info?.Write($"INFO: No errors reading settings file.");
            }

            // Initialize custom components
            CustomComponents.Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());

            // Initialize harmony patches
            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

    }
}
