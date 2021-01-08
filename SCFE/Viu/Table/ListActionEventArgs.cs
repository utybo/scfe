/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;

namespace Viu.Table
{
    public class ListActionEventArgs<T> : EventArgs
    {
        public ListActionEventArgs(IActionable component, ConsoleKeyInfo? sourceKeys, T item, GraphicsContext g)
        {
            Component = component;
            KeySource = sourceKeys;
            Item = item;
            Graphics = g;
        }

        public IActionable Component { get; }

        public ConsoleKeyInfo? KeySource { get; }

        public T Item { get; }

        public GraphicsContext Graphics { get; }
    }
}
