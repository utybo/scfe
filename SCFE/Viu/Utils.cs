/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace Viu
{
    public static class Utils
    {
        /// <summary>
        ///     Check if a key corresponds to an action according to a given input dictionary. The input dictionary should
        ///     be a compiled InputMap (InputMap.Compile())
        /// </summary>
        /// <param name="dic">A compiled InputMap (InputMap.Compiled())</param>
        /// <param name="keyInfo">The key that needs to check</param>
        /// <param name="action">The action that we want to see if it is associated with the keyInfo</param>
        /// <returns>Whether the dictionary contains a match between the keyInfo and the action</returns>
        public static bool KeyCorresponds(Dictionary<KeyStroke, string> dic, ConsoleKeyInfo keyInfo, string action)
        {
            return dic.Any(kv =>
                kv.Value == action && kv.Key.Matches(keyInfo));
        }

        /// <summary>
        ///     Return the action name associated with the given keyInfo
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="keyInfo"></param>
        /// <returns></returns>
        public static string GetActionNameForKey(Dictionary<KeyStroke, string> dic, ConsoleKeyInfo keyInfo)
        {
            if (dic.Any(kv => kv.Key.Matches(keyInfo))) return dic.First(kv => kv.Key.Matches(keyInfo)).Value;

            return null;
        }
    }
}
