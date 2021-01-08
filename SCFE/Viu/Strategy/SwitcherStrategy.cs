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
using JetBrains.Annotations;
using Viu.Components;

namespace Viu.Strategy
{
    public class SwitcherStrategy : LayoutStrategy
    {
        private Component _current;
        private readonly Parent _parent;

        private readonly Dictionary<object, Component> _bindings = new Dictionary<object, Component>();

        public SwitcherStrategy(Parent p)
        {
            _parent = p;
        }

        public override void ApplyLayoutStrategy(Parent p)
        {
            if (p != _parent)
                throw new ArgumentException(
                    "Cannot use a switcher strategy on a parent other than the one it was created with.");

            var children = p.GetChildren();

            foreach (var c in children)
            {
                c.X = p.X;
                c.Y = p.Y;
                c.Width = c.PrefWidth == Component.Computed ? p.Width : c.PrefWidth;
                c.Height = c.PrefHeight == Component.Computed ? p.Height : c.PrefHeight;
                c.Visible = ReferenceEquals(_current, c);
            }
        }

        public override Dimensions ComputeDimensions(Parent p)
        {
            return _current?.ComputeDimensions() ?? new Dimensions(0, 0);
        }

        public override bool IsHintAllowed(object hint)
        {
            return true;
        }

        public override void ComponentAdded(Parent p, Component c, object hint)
        {
            if (hint != null)
                _bindings.Add(hint, c);
        }

        public override void ComponentRemoved(Parent p, Component c)
        {
            if (c.LayoutInformation != null)
                _bindings.Remove(c.LayoutInformation);

            (from kv in _bindings where kv.Value == c select kv.Key).ToList().ForEach(h => _bindings.Remove(h));
        }

        public void SwitchToComponent([NotNull] Component c, [CanBeNull] GraphicsContext g)
        {
            if (_parent.GetChildren().Contains(c))
            {
                var switchFocus = _parent.IsFocused();
                if (switchFocus && g != null && ((_current as IFocusable)?.IsFocused() ?? false))
                    ((IFocusable) _current).SetFocused(false, g);
                _current = c;
                if (switchFocus && g != null)
                    (_current as IFocusable)?.SetFocused(true, g);
            }
            else
            {
                throw new ArgumentException(
                    "The component is not part of the parent attributed to this switcher strategy");
            }
        }

        public bool SwitchToComponentWithHint(object hint, GraphicsContext g)
        {
            if (!_bindings.ContainsKey(hint))
                return false;
            SwitchToComponent(_bindings[hint], g);
            return true;
        }

        public override IFocusable GetPreviousFocusableElement(Parent parent, Component previousFocus)
        {
            return previousFocus == null ? _current as IFocusable : null;
        }

        public override IFocusable GetNextFocusableElement(Parent parent, Component previousFocus)
        {
            return previousFocus == null ? _current as IFocusable : null;
        }
    }
}
