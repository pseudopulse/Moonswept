using System;
using Moonswept;
using System.Linq;
using BepInEx.Configuration;
using System.Reflection;
using System.Collections.Generic;

namespace Moonswept.Utils.ContentBases {
    public static class ContentScanner {
        public static void ScanTypes<T>(Assembly assembly, Action<T> action) {
            IEnumerable<Type> types = assembly.GetTypes().Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(T)));

            foreach (Type type in types) {
                T instance = (T)Activator.CreateInstance(type);
                action(instance);
            }
        }
    }
}