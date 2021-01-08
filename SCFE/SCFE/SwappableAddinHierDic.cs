/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System.Collections.Generic;
using Viu;

namespace SCFE
{
    public class SwappableAddinHierDic<TK, TV> : AbstractHierarchicalDictionary<TK, TV>
    {
        private readonly AbstractHierarchicalDictionary<TK, TV> _backing;

        public SwappableAddinHierDic(AbstractHierarchicalDictionary<TK, TV> backingHierarchicalDictionary)
        {
            _backing = backingHierarchicalDictionary;
        }

        public Dictionary<TK, TV> AddInDictionary { get; set; }

        public override AbstractHierarchicalDictionary<TK, TV> Parent
        {
            get => _backing.Parent;
            set => _backing.Parent = value;
        }

        public override TV Get(TK key)
        {
            if (AddInDictionary.ContainsKey(key))
                return AddInDictionary[key];
            return _backing.Get(key);
        }

        public override Dictionary<TK, TV> Compile()
        {
            var dic = new Dictionary<TK, TV>();
            if (AddInDictionary != null)
                foreach (var (key, value) in AddInDictionary)
                    if (dic.ContainsKey(key))
                        dic[key] = value;
                    else
                        dic.Add(key, value);

            var comp = _backing.Compile();
            foreach (var (key, value) in comp)
                if (!dic.ContainsKey(key))
                    dic.Add(key, value);

            return dic;
        }

        public override void Put(TK key, TV value)
        {
            _backing.Put(key, value);
        }
    }
}
