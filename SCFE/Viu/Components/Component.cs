/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;

namespace Viu.Components
{
    /// <summary>
    ///     A Component is an on-screen element. Classes that have children should be subclasses of Container instead of
    ///     Component.
    /// </summary>
    public abstract class Component
    {
        /// <summary>
        ///     Using this value for any coordinate in Pref, Max or Min values will result in the layout automatically
        ///     attributing the values computed by the component.
        /// </summary>
        public const int Computed = -1;

        private Container _parent;

        /// <summary>
        ///     The X coordinate of the element on the screen (relative to the console, not to the parent).
        ///     This value is constantly changed by the layout strategy.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        ///     The Y coordinate of the element on the screen (relative to the console, not to the parent).
        ///     This value is constantly changed by the layout strategy.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        ///     The width of the element on the screen
        ///     This value is constantly changed by the layout strategy.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        ///     The height of the element on the screen
        ///     This value is constantly changed by the layout strategy.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        ///     The width the component wishes to have. Said width will be requested to the layout strategy but might not
        ///     necessarily be respected.
        /// </summary>
        public int PrefWidth { get; set; } = Computed;

        /// <summary>
        ///     The height the component wishes to have. Said width will be requested to the layout strategy but might not
        ///     necessarily be respected.
        /// </summary>
        public int PrefHeight { get; set; } = Computed;

        // Not used yet
        public int MinWidth { get; set; } = Computed;

        // Not used yet
        public int MinHeight { get; set; } = Computed;

        // Not used yet
        public int MaxWidth { get; set; } = int.MaxValue;

        // Not used yet
        public int MaxHeight { get; set; } = int.MaxValue;

        /// <summary>
        ///     Whether the component is visible or not. A component that is not visible does not participate in its
        ///     parent's layout, as if it was never added.
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        ///     Additional information that might be used by the layout strategy. This value is set when calling the Add
        ///     method to a parent.
        /// </summary>
        public object LayoutInformation { get; internal set; }

        /// <summary>
        ///     The parent of this component.
        /// </summary>
        public Container Parent
        {
            get => _parent;
            internal set
            {
                _parent = value;
                InputMap.Parent = value?.InputMap;
                ActionMap.Parent = value?.ActionMap;
            }
        }

        public AbstractHierarchicalDictionary<KeyStroke, string> InputMap { get; set; } =
            new BaseHierarchicalDictionary<KeyStroke, string>();

        public AbstractHierarchicalDictionary<string, Action<object, ActionEventArgs>> ActionMap { get; set; } =
            new BaseHierarchicalDictionary<string, Action<object, ActionEventArgs>>();

        /// <summary>
        ///     Print the representation of this component on the screen. This method MUST ensure that what it prints stays
        ///     within its bounds.
        /// </summary>
        /// <param name="g"></param>
        public abstract void Print(GraphicsContext g);

        /// <summary>
        ///     Get the default graphics context, if any. This method should only be used as a last resort if there are no
        ///     other ways to get the graphics context
        /// </summary>
        /// <returns></returns>
        [Obsolete("This method should only be used as a last resort.")] // This generates a full warning
        protected virtual GraphicsContext GetGraphicsContext()
        {
            if (Parent == null)
                throw new NullReferenceException("No graphical context provided by parent components tree");
            return Parent.GetGraphicsContext();
        }

        /// <summary>
        ///     Computes the dimensions this component should have. These can be considered to be "bare minimum" dimensions.
        /// </summary>
        /// <returns>The dimensions the component should have</returns>
        public abstract Dimensions ComputeDimensions();

        /// <summary>
        ///     Shortcut to define the component's x and y coordinates as well as its width and height
        /// </summary>
        /// <param name="x">The X coordinate this component should have</param>
        /// <param name="y">The Y coordinate this component should have</param>
        /// <param name="width">The width this component should have</param>
        /// <param name="height">The height this component should have</param>
        public void SetBounds(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public virtual IEventThreadManager GetEventThread()
        {
            return Parent?.GetEventThread();
        }
    }
}
