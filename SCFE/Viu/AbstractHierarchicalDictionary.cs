/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Viu
{
    /// <summary>
    ///     A hierarchical dictionary: for any key K, it returns the value V stored in its internal dictionary if this
    ///     value is present. If not present in the internal dictionary, it returns the value from its parent
    ///     HierarchicalDictionary.
    ///     Used in ActionMap and InputMaps so that components inherit key bindings from their parent hierarchy.
    /// </summary>
    /// <typeparam name="TK">The type of the keys</typeparam>
    /// <typeparam name="TV">The type of the values</typeparam>
    public abstract class AbstractHierarchicalDictionary<TK, TV>
    {
        /// <summary>
        ///     The parent of this hierarchical dictionary.
        /// </summary>
        [CanBeNull]
        public abstract AbstractHierarchicalDictionary<TK, TV> Parent { get; set; }

        /// <summary>
        ///     Retrieve a value from this hierarchical dictionary. If not found in the current hierarchical dictionary,
        ///     it returns the result of Get for its parent hierarchical dictionary, or null if this is dictionary has no
        ///     parent dictionary.
        /// </summary>
        /// <param name="key">The key for which to get the value</param>
        /// <returns>
        ///     The value matching the key in this dictionary, or in its parent if not found, or null if this
        ///     dictionary does not have a parent
        /// </returns>
        [CanBeNull]
        public abstract TV Get([NotNull] TK key);

        /// <summary>
        ///     Put a value in the current dictionary. Where the value is actually stored depends on the implementation
        ///     (e.g. can be in a backing Dictionary)
        /// </summary>
        /// <param name="key">The key to add in the dictionary</param>
        /// <param name="value">The value matching the key to add.</param>
        public abstract void Put([NotNull] TK key, [NotNull] TV value);

        /// <summary>
        ///     Compiles the dictionary's hierarchy: it returns a (non-hierarchical) dictionary with all of the keys that
        ///     are present in the hierarchy and their matching values, corresponding the hierarchical order. This gives
        ///     the same result as creating a Dictionary by using Get on every key present in the hierarchy.
        ///     This only considers the parents in the hierarchy, as a HierarchicalDictionary has no information on its
        ///     children.
        /// </summary>
        /// <returns>The compiled dictionary that corresponds to this hierarchy.</returns>
        [NotNull]
        public abstract Dictionary<TK, TV> Compile();
    }
}
