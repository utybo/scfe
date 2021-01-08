/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System.Collections.Generic;

namespace Viu
{
    /// <summary>
    ///     Basic implementation of a HierarchicalDictionary that uses a Dictionary for storing its values
    /// </summary>
    /// <typeparam name="TK">The type of the keys</typeparam>
    /// <typeparam name="TV">The type of the values</typeparam>
    public class BaseHierarchicalDictionary<TK, TV> : AbstractHierarchicalDictionary<TK, TV>
    {
        public override AbstractHierarchicalDictionary<TK, TV> Parent { get; set; }

        /// <summary>
        ///     The backing dictionary used to store the values of this hierarchical dictionary
        /// </summary>
        private Dictionary<TK, TV> BackingDictionary { get; } = new Dictionary<TK, TV>();

        public override Dictionary<TK, TV> Compile()
        {
            var dic = Parent?.Compile() ?? new Dictionary<TK, TV>();
            foreach (var kv in BackingDictionary)
                if (dic.ContainsKey(kv.Key))
                    dic[kv.Key] = kv.Value;
                else
                    dic.Add(kv.Key, kv.Value);

            return dic;
        }

        public override TV Get(TK key)
        {
            if (BackingDictionary.ContainsKey(key))
                return BackingDictionary[key];

            return Parent != null ? Parent.Get(key) : default;
        }

        public override void Put(TK key, TV value)
        {
            if (BackingDictionary.ContainsKey(key))
                BackingDictionary[key] = value;
            else
                BackingDictionary.Add(key, value);
        }
    }
}
