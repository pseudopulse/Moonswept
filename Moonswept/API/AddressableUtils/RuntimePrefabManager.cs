using Moonswept;
using UnityEngine;
using UnityEngine.Networking;
using Moonswept.Utils.Attributes;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using Unity.Netcode;

namespace Moonswept.Utils.AddressableUtils {
    public static class RuntimePrefabManager {
        internal static GameObject PrefabParent;

        [AutoRun]
        internal static void Setup() {
            PrefabParent = new("MoonsweptPrefabParent");
            PrefabParent.SetActive(false);
            GameObject.DontDestroyOnLoad(PrefabParent);
        }

        public static GameObject CreatePrefab(this GameObject gameObject, string name) {
            GameObject clone = GameObject.Instantiate(gameObject, PrefabParent.transform);
            clone.name = name;
            if (clone.GetComponent<NetworkObject>()) {
                MakeNetworkPrefab(clone);
            }
            return clone;
        }

        public static void MakeNetworkPrefab(GameObject gameObject) {
            NetworkObject obj = gameObject.GetComponent<NetworkObject>();
            NetworkManager.Singleton.AddNetworkPrefab(gameObject);
        }
    }
}