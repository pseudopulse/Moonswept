using Moonswept;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Moonswept.Utils.Extensions.Enumeration {
    public static class EnumeratorExtensions {
        /// <summary>Gets a random element from the collection</summary>
        /// <returns>the chosen element</returns>
        public static T GetRandom<T>(this IEnumerable<T> self) {
            return self.ElementAt(Random.Range(0, self.Count()));
        }

        /// <summary>Gets a random element from the collection that matches the predicate</summary>
        /// <param name="predicate">the predicate to match</param>
        /// <returns>the chosen element</returns>
        public static T GetRandom<T>(this IEnumerable<T> self, System.Func<T, bool> predicate) {
            try {
                return self.Where(predicate).ElementAt(Random.Range(0, self.Count()));
            }
            catch {
                return default(T);
            }
        }
    }
}