/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
namespace SCFE
{
    public static class ScfeActions
    {
        public const string CopyFile = "file_copy";

        public const string CutFile = "file_cut";

        public const string PasteFile = "file_paste";

        public const string DeleteFile = "file_delete";

        public const string GitStage = "git_addfile";

        public const string GitUnstage = "git_removefile";

        public const string GitInit = "git_initrepo";

        public const string ChangeMode = "change_mode";

        public const string GoDownFast = "move_downfast";

        public const string GoUpFast = "move_upfast";

        public const string ToggleShowHiddenFiles = "show_hidden_files:tggl";

        public const string CreateFile = "file_create";

        public const string CreateFolder = "folder_create";

        public const string GoToFolder = "action_gotofolder";

        public const string Refresh = "action_refresh";

        public const string CurrDirOptions = "action_seecurrdiroptions";

        public const string Rename = "action_rename";

        public const string SelectAll = "select_all";

        public const string ToggleSelection = "select_toggle";

        public const string ChangeSort = "change_sortingmethod";
        
        public const string GitCommit = "git_commit";
        
        public const string GitPush = "git_push";
        
        public const string GitPull = "git_pull";
        
        public const string GitClone = "git_clone";

        public const string ComMode = "change_mode_com";

        public const string ToggleSortOrder = "toggle_sort_order";
    }
}
