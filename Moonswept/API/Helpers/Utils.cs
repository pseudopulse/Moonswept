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
}