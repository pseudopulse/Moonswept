using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Moonswept
{
    [BepInPlugin("MoonsweptTeam.Moonswept", "Moonswept", "0.5.0")]
    [BepInIncompatibility("com.potatoepet.AdvancedCompany")]
    public class Main : BaseUnityPlugin
    {
        public static ConfigFile config;
        public static AssetBundle assets;
        private void Awake()
        {
            config = Config;
            assets = AssetBundle.LoadFromFile(Path.GetDirectoryName(Info.Location) + "/moonswept");

            InitializeNetworkBehaviours();

            // AutoRunCollector.HandleAutoRun();
            // ConfigManager.HandleConfigAttributes(Assembly.GetExecutingAssembly(), config);

            ContentScanner.ScanTypes<GenericBase>(Assembly.GetExecutingAssembly(), x => x.Initialize());
        }

        private static void InitializeNetworkBehaviours() {
            // See https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#preparing-mods-for-patching
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        } 
    }
}
