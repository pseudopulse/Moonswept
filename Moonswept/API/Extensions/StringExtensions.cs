using System;
using System.Collections.Generic;

namespace Moonswept.Utils.Extensions.Text {
    public static class StringExtensions {
        /// <summary>
        /// Filters unsafe characters from a string. Default characters: \n ' (whitespace) ! ` - () {} | @ . \
        /// </summary>
        /// <returns>the filtered string</returns>
        public static string RemoveUnsafeCharacters(this string str) {
            string[] unsafeCharacters = { "\n", "'", " ", "!", "`", "&", "-", ")", "(", "{", "}", "|", "@", "<", ">", ".", "\\"};
            string filtered = str;

            foreach (string c in unsafeCharacters) {
                filtered = filtered.Replace(c, "");
            }

            return filtered;
        }

        /// <summary>
        /// Filters unsafe characters from a string.
        /// </summary>
        /// <param name="unsafeChars">an array of characters to filter out</param>
        /// <returns>the filtered string</returns>
        public static string RemoveUnsafeCharacters(this string str, string[] unsafeChars) {
            string filtered = str;

            foreach (string c in unsafeChars) {
                filtered = filtered.Replace(c, "");
            }

            return filtered;
        }
    }   
}