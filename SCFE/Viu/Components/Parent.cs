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
using System.Diagnostics;
using System.Linq;
using Viu.Strategy;

namespace Viu.Components
{
    /// <inheritdoc cref="Container" />
    /// <inheritdoc cref="IFocusable" />
    /// <summary>
    ///     A Parent is a Component which has children and organizes them according to a specific Layout Strategy.
    ///     Children can be added to the parent using the AddComponent method, with or without the layout hint, which is
    ///     used by the Layout Strategy to properly organize the components.
    ///     By default, parents use a FlowStrategy which organizes elements into a single row.
    /// </summary>
    public class Parent : Container, IFocusable
    {
        /// <summary>
        ///     Create a standard parent with a FlowStrategy
        /// </summary>
        public Parent()
        {
            Strategy = new FlowStrategy();
        }

        /// <summary>
        ///     Create a parent with the given layout strategy
        /// </summary>
        /// <param name="strategy"></param>
        public Parent(LayoutStrategy strategy)
        {
            Strategy = strategy;
        }

        /// <summary>
        ///     The strategy that should be used by the parent upon validation
        /// </summary>
        public LayoutStrategy Strategy { get; set; }

        public virtual bool AcceptInput(ConsoleKeyInfo keyPressed, GraphicsContext g)
        {
            foreach (var c in Components)
                if (c is IFocusable foc && foc.IsFocusable() && foc.IsFocused())
                {
                    if (foc.AcceptInput(keyPressed, g)) return true;

                    var ins = c.InputMap.Compile();
                    var str = Utils.GetActionNameForKey(ins, keyPressed);
                    if (str != null)
                    {
                        var actMap = c.ActionMap.Compile();
                        if (actMap.ContainsKey(str))
                        {
                            actMap[str](c, new ActionEventArgs(c, keyPressed, g));
                            return true;
                        }
                    }
                }

            // If we are still here it means that none of the focusable components have accepted any input
            // Use our own shortcuts to switch to the next focusable component

            var prev = GetFocusedElement(false);
            var inputs = InputMap.Compile();
            IFocusable next = null;

            if (Utils.KeyCorresponds(inputs, keyPressed, StandardActionNames.MoveUp))
                next = Strategy.GetUpFocusableElement(this, prev);
            else if (Utils.KeyCorresponds(inputs, keyPressed, StandardActionNames.MoveLeft))
                next = Strategy.GetPreviousFocusableElement(this, prev);
            else if (Utils.KeyCorresponds(inputs, keyPressed, StandardActionNames.MoveRight))
                next = Strategy.GetNextFocusableElement(this, prev);
            else if (Utils.KeyCorresponds(inputs, keyPressed, StandardActionNames.MoveDown))
                next = Strategy.GetDownFocusableElement(this, prev);

            if (next != null)
            {
                if (prev != null)
                {
                    var f = prev as IFocusable;
                    Debug.Assert(f != null, nameof(f) + " != null");
                    f.SetFocused(false, g);
                }

                next.SetFocused(true, g);
                return true;
            }


            return false;
        }

        public bool IsFocusable()
        {
            return Components.Any(x => (x as IFocusable)?.IsFocusable() ?? false);
        }

        public bool IsFocused()
        {
            return Components.Any(x => (x as IFocusable)?.IsFocused() ?? false);
        }

        public void SetFocused(bool focused, GraphicsContext g)
        {
            if (focused)
            {
                var f = Strategy.GetNextFocusableElement(this, null);
                if (f == null)
                    foreach (var c in Components)
                        if (c.Visible && c is IFocusable foc && foc.IsFocusable())
                        {
                            f = foc;
                            break;
                        }

                f?.SetFocused(true, g);
                (f as ICursorFocusable)?.UpdateCursorState(g);
            }
            else
            {
                foreach (var c in Components)
                {
                    var f = c as IFocusable;
                    var b = (f?.IsFocused() ?? false) && f is ICursorFocusable;
                    f?.SetFocused(false, g);
                    if (b)
                        (f as ICursorFocusable).UpdateCursorState(g);
                }
            }
        }

        /// <summary>
        ///     Add a component to this parent. This throws an ArgumentException of the component already has a parent
        ///     elsewhere, or if the layout hint is not allowed by the layout strategy.
        /// </summary>
        /// <param name="c">The component to add</param>
        /// <param name="layoutHint">
        ///     The layout hint to use. It is used as a tip on where to place the component for the
        ///     strategy
        /// </param>
        /// <exception cref="ArgumentException">
        ///     If the component already has a parent or if the strategy does not accept
        ///     the given hint
        /// </exception>
        public void AddComponent(Component c, object layoutHint)
        {
            if (c.Parent != null)
                throw new ArgumentException("A component cannot have multiple parents");
            if (!Strategy.IsHintAllowed(layoutHint))
                throw new ArgumentException("Hint not accepted by layout strategy");

            Components.Add(c);
            Strategy.ComponentAdded(this, c, layoutHint);
            c.Parent = this;
            c.LayoutInformation = layoutHint;
        }

        /// <summary>
        ///     Add a component to this parent. This throws an ArgumentException of the component already has a parent
        ///     elsewhere, or if the layout hint does not allow adding components without any layout hint.
        /// </summary>
        /// <param name="c">The component to add</param>
        /// <exception cref="ArgumentException">
        ///     If the component already has a parent or if the strategy does not accept
        ///     hint-less addition of components, in which case you should use the overload with an additional object
        ///     argument.
        /// </exception>
        public void AddComponent(Component c)
        {
            if (c.Parent != null)
                throw new ArgumentException("A component cannot have multiple parents");
            if (!Strategy.IsHintAllowed(null))
                throw new ArgumentException("Component must have a layout hint according to the strategy");

            Components.Add(c);
            Strategy.ComponentAdded(this, c, null);
            c.Parent = this;
            c.LayoutInformation = null;
        }

        public bool RemoveComponent(Component c)
        {
            var b = Components.Remove(c);
            if (b)
                c.Parent = null;
            Strategy.ComponentRemoved(this, c);
            return b;
        }

        public override void Validate()
        {
            Strategy.ApplyLayoutStrategy(this);

            foreach (var c in Components) (c as Container)?.Validate();
        }

        public override Dimensions ComputeDimensions()
        {
            return Strategy.ComputeDimensions(this);
        }

        public List<Component> GetChildren()
        {
            return Components;
        }

        public override void Print(GraphicsContext g)
        {
            if (!Visible)
                return;

            base.Print(g);
            (GetFocusedElement(true) as ICursorFocusable)?.UpdateCursorState(g);
        }

        public Component GetFocusedElement(bool indepth)
        {
            try
            {
                var c = Components.First(x => (x as IFocusable)?.IsFocused() ?? false);
                if (indepth && c is Parent parent)
                    return parent.GetFocusedElement(true);
                return c;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}
