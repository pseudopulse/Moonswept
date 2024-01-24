using System.Reflection;
using System.Collections;
using System;

namespace Moonswept.Utils.Attributes {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AutoRunAttribute : Attribute {
        public AutoRunAttribute() {

        }
    }

    internal sealed class AutoRunCollector {
        public static void HandleAutoRun() {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types) {
                TypeInfo tInfo = type.GetTypeInfo();
                foreach (MethodInfo info in tInfo.GetMethods((BindingFlags)(-1))) {
                    AutoRunAttribute attr = info.GetCustomAttribute<AutoRunAttribute>();
                    if (attr != null && info.IsStatic) {
                        info.Invoke(null, null);
                    }
                }
            }
        }
    }
}