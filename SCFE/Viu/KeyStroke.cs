/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using System.Diagnostics;

namespace Viu
{
    public struct KeyStroke
    {
        public ConsoleKey? Key { get; }
        public char? KeyLetter { get; }
        public bool? Shift { get; }
        public bool? Alt { get; }
        public bool? Control { get; }

        public KeyStroke(ConsoleKey key, bool? control, bool? alt, bool? shift)
        {
            Key = key;
            KeyLetter = null;
            Alt = alt;
            Shift = shift;
            Control = control;
        }

        public KeyStroke(char key, bool? control, bool? alt, bool? shift)
        {
            KeyLetter = key;
            Key = null;
            Alt = alt;
            Shift = shift;
            Control = control;
        }

        public bool Matches(ConsoleKeyInfo cki)
        {
            var altMatch = Alt == null || Alt.Value == ((cki.Modifiers & ConsoleModifiers.Alt) != 0);
            var ctrlMatch = Control == null ||
                            Control.Value == ((cki.Modifiers & ConsoleModifiers.Control) != 0);
            var shiftMatch = Shift == null ||
                             Shift.Value == ((cki.Modifiers & ConsoleModifiers.Shift) != 0);

            if (Key == null)
            {
                Debug.Assert(KeyLetter != null, nameof(KeyLetter) + " != null");
                var keyName = Enum.GetName(typeof(ConsoleKey), cki.Key);
                if (keyName == null)
                    return false;
                return altMatch && ctrlMatch && shiftMatch &&
                       (char.ToLower(cki.KeyChar) == char.ToLower(KeyLetter.Value) ||
                        keyName.Length == 1 && char.ToLower(keyName[0]) == char.ToLower(KeyLetter.Value));
            }

            return altMatch && ctrlMatch && shiftMatch && cki.Key == Key;
        }

        public override bool Equals(object obj)
        {
            if (obj is KeyStroke other)
                return Key == other.Key && KeyLetter == other.KeyLetter && Shift == other.Shift && Alt == other.Alt &&
                       Control == other.Control;

            return false;
        }

        public bool Equals(KeyStroke other)
        {
            return Key == other.Key && KeyLetter == other.KeyLetter && Shift == other.Shift && Alt == other.Alt &&
                   Control == other.Control;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Key.GetHashCode();
                hashCode = (hashCode * 397) ^ KeyLetter.GetHashCode();
                hashCode = (hashCode * 397) ^ Shift.GetHashCode();
                hashCode = (hashCode * 397) ^ Alt.GetHashCode();
                hashCode = (hashCode * 397) ^ Control.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(KeyStroke left, KeyStroke right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(KeyStroke left, KeyStroke right)
        {
            return !left.Equals(right);
        }
    }
}
