using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Moonswept.Utils.Helpers {
    public class Utils {

        ///<summary>Returns the Forward vector that would make an object face another object</summary>
        ///<param name="self">the object that you want to look from</param>
        ///<param name="target">the object you want to look at</param>
        public static Vector3 FindLookRotation(GameObject self, GameObject target) {
            return (target.transform.position - self.transform.position).normalized;
        }
    }

    public class StopwatchArray {
        private Dictionary<string, float> watches;

        public StopwatchArray() {
            watches = new();
        }

        public float this[string key] {
            get {
                if (!watches.ContainsKey(key)) watches.Add(key, 0f);

                return watches[key];
            }

            set {
                if (!watches.ContainsKey(key)) watches.Add(key, 0f);

                watches[key] = value;
            }
        }

        public float this[Enum key] {
            get => this[key.ToString()];
            set => this[key.ToString()] = value;
        }

        public float this[int key] {
            get => this[key.ToString()];
            set => this[key.ToString()] = value;
        }
    }
}