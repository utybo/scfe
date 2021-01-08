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
    /// <summary>
    ///     Alignment along the horizontal axis
    /// </summary>
    public enum HorizontalAlignment
    {
        /// <summary>
        ///     Align to the left
        /// </summary>
        Left = -1,

        /// <summary>
        ///     Align in the center
        /// </summary>
        Centered = 0,

        /// <summary>
        ///     Align to the right
        /// </summary>
        Right = 1
    }

    /// <summary>
    /// Alignment along the vertical axis
    /// </summary>
    public enum VerticalAlignment
    {
        /// <summary>
        /// Align to the top
        /// </summary>
        Top = -1,
        
        /// <summary>
        /// Align to the center 
        /// </summary>
        Centered = 0,
        
        /// <summary>
        /// Align to the bottom
        /// </summary>
        Bottom = 1
    }

    /// <summary>
    /// Orientation along either the horizontal or vertical axis
    /// </summary>
    public enum Orientation
    {
        Horizontal = 0,
        Vertical = 1
    }

    /// <summary>
    /// Standard action names used inside Viu.
    /// </summary>
    public static class StandardActionNames
    {
        /// <summary>
        ///     Name for the action of moving up in a list, menu, etc
        /// </summary>
        public const string MoveUp = "mv_up";

        /// <summary>
        ///     Name for the action of going left in a table, menu, etc
        /// </summary>
        public const string MoveLeft = "mv_left";

        /// <summary>
        /// Name for the action of going left by one word
        /// </summary>
        public const string MoveLeftWord = "mv_left_word";

        /// <summary>
        /// Name for the action of moving to the beginning (e.g. of a line)
        /// </summary>
        public const string MoveLineStart = "mv_start";

        /// <summary>
        ///     Name for the action of going right in a table, menu, etc
        /// </summary>
        public const string MoveRight = "mv_right";

        /// <summary>
        /// Name for the action of going right by one word
        /// </summary>
        public const string MoveRightWord = "mv_right_word";

        public const string MoveLineEnd = "mv_end";

        /// <summary>
        ///     Name for the action of going down in a list, menu, etc
        /// </summary>
        public const string MoveDown = "mv_down";

        /// <summary>
        ///     Name for the base action: for example activating a button
        /// </summary>
        public const string BaseAction = "action_base";

        public const string SecondaryAction = "acion_secondary";

        /// <summary>
        ///     The act of cancelling an action, whatever it may be
        /// </summary>
        public const string CancelAction = "action_cancel";

        public const string SelectAction = "action_select";

        public const string DeleteToTheLeftAction = "action_delleft";

        public const string DeleteWordToTheLeftAction = "action_delwleft";

        public const string DeleteToTheRightAction = "action_delright";

        public const string DeleteWordToTheRightAction = "action_delwright";
    }
}
