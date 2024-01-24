using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Moonswept
{
    [BepInPlugin("MoonsweptTeam.Moonswept", "Moonswept", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        public static ConfigFile config;
        private void Awake()
        {
            config = Config;
            AutoRunCollector.HandleAutoRun();
            ConfigManager.HandleConfigAttributes(Assembly.GetExecutingAssembly(), config);
        }
    }
}