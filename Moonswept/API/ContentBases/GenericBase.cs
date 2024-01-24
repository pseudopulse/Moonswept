using System;
using UnityEngine;
using BepInEx.Configuration;
using Moonswept.Utils.Extensions.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Moonswept.Utils.ContentBases {
    public abstract class GenericBase<T> : GenericBase where T : GenericBase<T>{
        public static T Instance { get; private set; }

        public GenericBase() {
            if (Instance == null) {
                Instance = this as T;
            }
        }
    }

    public abstract class GenericBase {
        public ConfigFile config;

        public virtual void Initialize() {
            PostCreation();
        }

        public virtual void PostCreation() {

        }
    }
}