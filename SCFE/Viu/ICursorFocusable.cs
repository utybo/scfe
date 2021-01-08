/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
namespace Viu
{
    public interface ICursorFocusable : IFocusable
    {
        /// <summary>
        ///     Called after all print events, including when the component is NOT focused. Sets the cursor to the position where
        ///     it
        ///     needs to be displayed, as well as making it visible if necessary or adjusting any other thing about the component.
        /// </summary>
        void UpdateCursorState(GraphicsContext g);
    }
}
