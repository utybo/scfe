/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using JetBrains.Annotations;
using Viu.Components;

namespace Viu.Strategy
{
    public abstract class LayoutStrategy
    {
        /// <summary>
        ///     Lay out the underlying components of a parent
        /// </summary>
        /// <param name="p"></param>
        public abstract void ApplyLayoutStrategy([NotNull] Parent p);

        /// <summary>
        ///     Compute the predicted width and height of the parent based on this layout
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public abstract Dimensions ComputeDimensions([NotNull] Parent p);

        /// <summary>
        ///     Checks if the given hint is valid or not
        /// </summary>
        /// <param name="hint"></param>
        /// <returns></returns>
        public abstract bool IsHintAllowed([CanBeNull] object hint);

        /// <summary>
        ///     A method called whenever a component is added to a parent with this layout. This is called after the
        ///     component is added to the parent's children.
        /// </summary>
        /// <param name="p">The parent to which a component is added</param>
        /// <param name="c">The added component</param>
        /// <param name="hint">
        ///     The layout hint used on the component, which can be null. This parameter is guaranteed
        ///     to be such that IsHintAllowed(hint)
        /// </param>
        public virtual void ComponentAdded([NotNull] Parent p, [NotNull] Component c, [CanBeNull] object hint)
        {
        }

        /// <summary>
        ///     A method called whenever a component is added to a parent with this layout. This is called after the
        ///     component is added to the parent's children.
        /// </summary>
        /// <param name="p">The parent to which a component is added</param>
        /// <param name="c">The added component</param>
        public virtual void ComponentRemoved([NotNull] Parent p, [NotNull] Component c)
        {
        }

        /// <summary>
        ///     Get the focusable element that is before the current focusable element. The returned element has to be
        ///     focusable (IsFocusable == true).
        ///     Usually called when the left arrow key is pressed.
        /// </summary>
        /// <param name="parent">The parent component in which the focused element is</param>
        /// <param name="previousFocus">
        ///     The element that was previously in focus. Can be null, in which case the
        ///     strategy must give the first element that is supposed to be accessed when going "left" from outside of the
        ///     parent.
        /// </param>
        /// <returns>
        ///     The element that should be focused that is before previousFocus, usually on the left of
        ///     previousFocus, or null if the previously focused element was the first focusable element of the parent.
        /// </returns>
        [CanBeNull]
        public abstract IFocusable GetPreviousFocusableElement([NotNull] Parent parent,
            [CanBeNull] Component previousFocus);

        /// <summary>
        ///     Get the focusable element that is after the current focusable element. The returned element has to be
        ///     focusable (IsFocusable == true)
        ///     Usually called when the right arrow key is pressed.
        /// </summary>
        /// <param name="parent">The parent component in which the focused element is</param>
        /// <param name="previousFocus">
        ///     The element that was previously in focus. Can be null, in which case the
        ///     strategy must give the first element that is supposed to be accessed when going "right" from outside of the
        ///     parent.
        /// </param>
        /// <returns>
        ///     The element that should be focused next, usually on the right of previousFocus, or null if the
        ///     previously focused element was the last focusable element of the parent.
        /// </returns>
        public abstract IFocusable
            GetNextFocusableElement([NotNull] Parent parent, [CanBeNull] Component previousFocus);

        /// <summary>
        ///     Get the focusable element that is above the current focusable element. The returned element has to be
        ///     focusable (IsFocusable == true)
        ///     Usually called when the up arrow key is pressed.
        ///     If not overriden, this always returns null.
        /// </summary>
        /// <param name="parent">The parent component in which the focused element is</param>
        /// <param name="previousFocus">
        ///     The element that was previously in focus. Can be null, in which case the
        ///     strategy must give the first element that is supposed to be accessed when going "up" from outside of the
        ///     parent.
        /// </param>
        /// <returns>
        ///     The element that should be focused next, usually above previousFocus, or null if the
        ///     previously focused element was the focusable element at the top of the parent. Can also return null if there
        ///     is no way to go "up".
        /// </returns>
        public virtual IFocusable GetUpFocusableElement([NotNull] Parent parent, [CanBeNull] Component previousFocus)
        {
            if (previousFocus == null)
                GetPreviousFocusableElement(parent, null);
            return null;
        }

        /// <summary>
        ///     Get the focusable element that is below the current focusable element. The returned element has to be
        ///     focusable (IsFocusable == true)
        ///     Usually called when the down arrow key is pressed.
        /// </summary>
        /// <param name="parent">The parent component in which the focused element is</param>
        /// <param name="previousFocus">
        ///     The element that was previously in focus. Can be null, in which case the
        ///     strategy must give the first element that is supposed to be accessed when going "down" from outside of the
        ///     parent.
        /// </param>
        /// <returns>
        ///     The element that should be focused next, usually below previousFocus, or null if the
        ///     previously focused element was the focusable element at the bottom of the parent. Can also return null if
        ///     there is no way to go "down".
        /// </returns>
        public virtual IFocusable GetDownFocusableElement([NotNull] Parent parent, [CanBeNull] Component previousFocus)
        {
            if (previousFocus == null)
                GetNextFocusableElement(parent, null);
            return null;
        }
    }
}
