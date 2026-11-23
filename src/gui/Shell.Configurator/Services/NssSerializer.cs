using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Shell.Configurator.Models;

namespace Shell.Configurator.Services;

public static class NssSerializer
{
    public const string SidecarFileName = "shell.configurator.json";

    private static readonly HashSet<string> StandardImports = new(StringComparer.OrdinalIgnoreCase)
    {
        "imports/theme.nss",
        "imports/images.nss",
        "imports/modify.nss",
        "imports/terminal.nss",
        "imports/file-manage.nss",
        "imports/develop.nss",
        "imports/goto.nss",
        "imports/taskbar.nss",
        "imports/user.nss"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string L(bool useLoc, string locKey, string fallback)
        => useLoc ? $"loc.{locKey}" : $"\"{EscapeDoubleQuoted(fallback)}\"";

    public static Dictionary<string, string> GenerateAllFiles(ShellConfig config)
    {
        bool useLoc = config.General.UseLocalization;
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["shell.nss"] = GenerateShellNss(config, useLoc),
            [@"imports\theme.nss"] = GenerateThemeNss(config.Theme),
            [@"imports\modify.nss"] = GenerateModifyNss(config.Modify),
            [@"imports\terminal.nss"] = GenerateTerminalNss(config.Terminal),
            [@"imports\file-manage.nss"] = GenerateFileManageNss(config.FileManage, useLoc),
            [@"imports\develop.nss"] = GenerateDevelopNss(config.Develop, useLoc),
            [@"imports\goto.nss"] = GenerateGotoNss(config.Goto, useLoc),
            [@"imports\taskbar.nss"] = GenerateTaskbarNss(config.Taskbar, useLoc),
            [@"imports\user.nss"] = GenerateUserNss(config.CustomItems)
        };
        return files;
    }

    public static string GenerateShellNss(ShellConfig config, bool? useLocOverride = null)
    {
        var g = config.General;
        bool useLoc = useLocOverride ?? g.UseLocalization;

        var sb = new StringBuilder();
        sb.AppendLine("settings");
        sb.AppendLine("{");
        sb.AppendLine($"\tpriority={g.Priority}");
        if (g.ExcludeNonExplorer)
            sb.AppendLine("\texclude.where = !process.is_explorer");
        sb.AppendLine($"\tshowdelay = {g.ShowDelay}");
        sb.AppendLine("\t// Options to allow modification of system items");
        sb.AppendLine($"\tmodify.remove.duplicate={(g.RemoveDuplicates ? 1 : 0)}");
        sb.AppendLine($"\ttip.enabled={(g.TipsEnabled ? "true" : "false")}");
        sb.AppendLine("}");

        if (useLoc)
        {
            sb.AppendLine();
            sb.AppendLine("// localization");
            sb.AppendLine(@"$loc_path='imports\lang\'");
            sb.AppendLine("import lang loc_path + \"en.nss\"");
            if (string.Equals(g.Language, "auto", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("import lang if(path.exists(loc_path + sys.lang + \".nss\"),");
                sb.AppendLine("               loc_path + sys.lang + \".nss\",");
                sb.AppendLine("               loc_path + \"en.nss\")");
            }
            else if (!string.Equals(g.Language, "en", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"import lang loc_path + \"{EscapeDoubleQuoted(g.Language)}.nss\"");
            }
        }

        sb.AppendLine();
        AppendImport(sb, g.EnableThemeImport, "imports/theme.nss");
        AppendImport(sb, g.EnableImagesImport, "imports/images.nss");
        AppendImport(sb, g.EnableModifyImport, "imports/modify.nss");

        if (g.EnablePinUnpinMenu)
        {
            sb.AppendLine();
            sb.AppendLine($"menu(mode=\"multiple\" title={L(useLoc, "pin_unpin", "Pin/Unpin")} image=icon.pin)");
            sb.AppendLine("{");
            sb.AppendLine("}");
        }

        if (g.EnableMoreOptionsMenu)
        {
            sb.AppendLine();
            sb.AppendLine("menu(mode=\"multiple\" title=title.more_options image=icon.more_options)");
            sb.AppendLine("{");
            sb.AppendLine("}");
        }

        sb.AppendLine();
        AppendImport(sb, g.EnableTerminalImport, "imports/terminal.nss");
        AppendImport(sb, g.EnableFileManageImport, "imports/file-manage.nss");
        AppendImport(sb, g.EnableDevelopImport, "imports/develop.nss");
        AppendImport(sb, g.EnableGotoImport, "imports/goto.nss");
        AppendImport(sb, g.EnableTaskbarImport, "imports/taskbar.nss");

        if (config.CustomItems.Any(i => i.Enabled))
            AppendImport(sb, g.EnableCustomImport, "imports/user.nss");
        else
            sb.AppendLine("// import 'imports/user.nss'");

        if (g.PreservedCustomImports.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("// Preserved custom imports from existing config");
            foreach (string extra in g.PreservedCustomImports.Distinct(StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"import '{EscapeSingleQuoted(extra)}'");
        }

        return sb.ToString();
    }

    public static string GenerateThemeNss(ThemeSettings t)
    {
        var sb = new StringBuilder();
        sb.AppendLine("theme");
        sb.AppendLine("{");
        sb.AppendLine($"\tname=\"{EscapeDoubleQuoted(t.Name)}\"");
        if (!string.Equals(t.View, "auto", StringComparison.OrdinalIgnoreCase))
            sb.AppendLine($"\tview=view.{t.View}");
        sb.AppendLine($"\tdark={t.DarkMode}");
        sb.AppendLine("\tbackground");
        sb.AppendLine("\t{");
        sb.AppendLine($"\t\tcolor={(string.IsNullOrWhiteSpace(t.BackgroundColor) ? "auto" : t.BackgroundColor.Trim())}");
        sb.AppendLine($"\t\topacity={(t.UseAutoOpacity ? "auto" : t.BackgroundOpacity.ToString())}");
        sb.AppendLine($"\t\teffect={MapEffectValue(t.BackgroundEffect)}");
        sb.AppendLine("\t}");

        sb.AppendLine("\tborder");
        sb.AppendLine("\t{");
        sb.AppendLine($"\t\tenabled={(t.BorderEnabled ? "true" : "false")}");
        sb.AppendLine($"\t\tsize={t.BorderSize}");
        sb.AppendLine($"\t\tradius={t.BorderRadius}");
        sb.AppendLine("\t}");

        sb.AppendLine("\tshadow");
        sb.AppendLine("\t{");
        sb.AppendLine($"\t\tenabled={(t.ShadowEnabled ? "true" : "false")}");
        sb.AppendLine($"\t\tsize={t.ShadowSize}");
        sb.AppendLine($"\t\topacity={t.ShadowOpacity}");
        sb.AppendLine("\t}");

        if (t.UseCustomFont)
        {
            sb.AppendLine("\tfont");
            sb.AppendLine("\t{");
            if (!string.IsNullOrWhiteSpace(t.FontName))
                sb.AppendLine($"\t\tname=\"{EscapeDoubleQuoted(t.FontName)}\"");
            sb.AppendLine($"\t\tsize={t.FontSize}");
            sb.AppendLine("\t}");
        }

        sb.AppendLine($"\timage.align={t.ImageAlign}");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string GenerateModifyNss(ModifySettings m)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// modify items");

        var removedIds = new List<string>();
        if (m.RemoveRestorePreviousVersions) removedIds.Add("id.restore_previous_versions");
        if (m.RemoveCastToDevice) removedIds.Add("id.cast_to_device");

        if (removedIds.Count > 0)
        {
            sb.AppendLine("// Remove items by identifiers");
            sb.AppendLine("modify(mode=mode.multiple");
            sb.AppendLine($"\twhere=this.id({string.Join(",", removedIds)})");
            sb.AppendLine("\tvis=vis.remove)");
            sb.AppendLine();
        }

        if (m.PromoteEmptyRecycleBin)
        {
            sb.AppendLine("modify(type=\"recyclebin\" where=window.is_desktop and this.id==id.empty_recycle_bin pos=1 sep)");
            sb.AppendLine();
        }

        if (m.GroupPinUnpin)
        {
            sb.AppendLine("modify(find=\"unpin*\" pos=\"bottom\" menu=\"Pin/Unpin\")");
            sb.AppendLine("modify(find=\"pin*\" pos=\"top\" menu=\"Pin/Unpin\")");
            sb.AppendLine();
        }

        if (m.MoveCopyAsPathToFileManage)
            sb.AppendLine("modify(where=this.id==id.copy_as_path menu=\"file manage\")");

        if (m.MoveCustomizeFolderToFileManage)
            sb.AppendLine("modify(type=\"dir.back|drive.back\" where=this.id==id.customize_this_folder pos=1 sep=\"top\" menu=\"file manage\")");

        if (m.MoveCopyAsPathToFileManage || m.MoveCustomizeFolderToFileManage)
            sb.AppendLine();

        if (m.MoveTerminalItemsToTerminal)
        {
            sb.AppendLine("modify(where=str.equals(this.name, [\"open in terminal\", \"open linux shell here\"]) || this.id==id.open_powershell_window_here");
            sb.AppendLine("\tpos=\"bottom\" menu=\"Terminal\")");
            sb.AppendLine();
        }

        var moreIds = new List<string>();
        if (m.MoreSendTo) moreIds.Add("id.send_to");
        if (m.MoreShare) moreIds.Add("id.share");
        if (m.MoreCreateShortcut) moreIds.Add("id.create_shortcut");
        if (m.MoreSetDesktopBackground) moreIds.Add("id.set_as_desktop_background");
        if (m.MoreRotateLeft) moreIds.Add("id.rotate_left");
        if (m.MoreRotateRight) moreIds.Add("id.rotate_right");
        if (m.MoreMapNetworkDrive) moreIds.Add("id.map_network_drive");
        if (m.MoreDisconnectNetworkDrive) moreIds.Add("id.disconnect_network_drive");
        if (m.MoreFormat) moreIds.Add("id.format");
        if (m.MoreEject) moreIds.Add("id.eject");
        if (m.MoreGiveAccessTo) moreIds.Add("id.give_access_to");
        if (m.MoreIncludeInLibrary) moreIds.Add("id.include_in_library");
        if (m.MorePrint) moreIds.Add("id.print");

        if (moreIds.Count > 0)
        {
            sb.AppendLine("modify(mode=mode.multiple");
            sb.AppendLine("\twhere=this.id(");
            for (int i = 0; i < moreIds.Count; i++)
            {
                string comma = i < moreIds.Count - 1 ? "," : "";
                sb.AppendLine($"\t\t{moreIds[i]}{comma}");
            }
            sb.AppendLine("\t)");
            sb.AppendLine("\tpos=1 menu=title.more_options)");
        }

        return sb.ToString();
    }

    public static string GenerateTerminalNss(TerminalSettings t)
    {
        var sb = new StringBuilder();
        sb.AppendLine("menu(type='*' where=(sel.count or wnd.is_taskbar or wnd.is_edit) title=title.terminal sep=sep.top image=icon.run_with_powershell)");
        sb.AppendLine("{");
        if (t.EnableAdminModifier)
        {
            sb.AppendLine("\t$tip_run_admin=[\"\\xE1A7 Press [SHIFT key] or [RIGHT-CLICK] to run \" + this.title + \" as administrator\", tip.warning, 1.0]");
            sb.AppendLine("\t$has_admin=key.shift() or key.rbutton()");
            sb.AppendLine();
        }

        string adminAttr = t.EnableAdminModifier ? " tip=tip_run_admin admin=has_admin" : "";

        if (t.CommandPrompt)
            sb.AppendLine($"\titem(title=title.command_prompt{adminAttr} image cmd-prompt=`/K TITLE Command Prompt &ver& PUSHD \"@sel.dir\"`)");

        if (t.WindowsPowerShell)
            sb.AppendLine($"\titem(title=title.windows_powershell{adminAttr} image cmd-ps=`-noexit -command Set-Location -Path '@sel.dir'`)");

        if (t.WindowsTerminal)
        {
            sb.AppendLine("\titem(where=package.exists(\"WindowsTerminal\")");
            sb.AppendLine("\t\t\ttitle=title.Windows_Terminal");
            if (t.EnableAdminModifier)
            {
                sb.AppendLine("\t\t\ttip=tip_run_admin");
                sb.AppendLine("\t\t\tadmin=has_admin");
            }
            sb.AppendLine("\t\t\timage='@package.path(\"WindowsTerminal\")\\WindowsTerminal.exe'");
            sb.AppendLine("\t\t\tcmd=\"wt.exe\"");
            sb.AppendLine("\t\t\targ=`-d \"@sel.path\\.\"`)");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string GenerateFileManageNss(FileManageSettings f, bool useLoc = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"menu(where=sel.count>0 type='file|dir|drive|namespace|back' mode=\"multiple\" title={L(useLoc, "file_manage", "File Manage")} image=\\uE253)");
        sb.AppendLine("{");

        if (f.CopyPathMenu)
        {
            sb.AppendLine("\tmenu(separator=\"after\" title=title.copy_path image=icon.copy_path)");
            sb.AppendLine("\t{");
            if (f.CopyMultiplePaths)
            {
                string multiTitle = useLoc ? "loc.copy_multiple_paths" : "'Copy (@sel.count) items selected'";
                sb.AppendLine($"\t\titem(where=sel.count > 1 title={multiTitle} cmd=command.copy(sel(false, \"\\n\")))");
            }
            if (f.CopyFullPath)
                sb.AppendLine("\t\titem(mode=\"single\" title=@sel.path tip=sel.path cmd=command.copy(sel.path))");
            if (f.CopyShortcutTarget)
                sb.AppendLine("\t\titem(mode=\"single\" type='file' separator=\"before\" where=length(sel.lnk)>0 title=sel.lnk cmd=command.copy(sel.lnk))");
            if (f.CopyParentPath)
            {
                sb.AppendLine("\t\tseparator");
                sb.AppendLine("\t\titem(mode=\"single\" where=@sel.parent.len>3 title=sel.parent cmd=@command.copy(sel.parent))");
            }
            if (f.CopyFileName || f.CopyFileTitle || f.CopyFileExtension)
            {
                sb.AppendLine("\t\tseparator");
                if (f.CopyFileName)
                    sb.AppendLine("\t\titem(mode=\"single\" type='file|dir|back.dir' title=sel.file.name cmd=command.copy(sel.file.name))");
                if (f.CopyFileTitle)
                    sb.AppendLine("\t\titem(mode=\"single\" type='file' where=sel.file.len != sel.file.title.len title=@sel.file.title cmd=command.copy(sel.file.title))");
                if (f.CopyFileExtension)
                    sb.AppendLine("\t\titem(mode=\"single\" type='file' where=sel.file.ext.len>0 title=sel.file.ext cmd=command.copy(sel.file.ext))");
            }
            sb.AppendLine("\t}");
            sb.AppendLine();
        }

        if (f.ChangeExtension)
        {
            string extTitle = L(useLoc, "change_extension", "Change extension");
            sb.AppendLine($"\titem(mode=\"single\" type=\"file\" title={extTitle} image=\\uE0B5 cmd=if(input({extTitle}, \"Type extension\"),");
            sb.AppendLine("\t\tio.rename(sel.path, path.join(sel.dir, sel.file.title + \".\" + input.result))))");
            sb.AppendLine();
        }

        if (f.SelectMenu)
        {
            sb.AppendLine("\tmenu(separator=\"after\" image=\\uE290 title=title.select)");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\titem(title={L(useLoc, "all", "All")} image=icon.select_all cmd=command.select_all)");
            sb.AppendLine($"\t\titem(title={L(useLoc, "invert", "Invert")} image=icon.invert_selection cmd=command.invert_selection)");
            sb.AppendLine($"\t\titem(title={L(useLoc, "none", "None")} image=icon.select_none cmd=command.select_none)");
            sb.AppendLine("\t}");
            sb.AppendLine();
        }

        if (f.TakeOwnership)
        {
            sb.AppendLine($"\titem(type='file|dir|back.dir|drive' title={L(useLoc, "take_ownership", "Take Ownership")} image=[\\uE194,#f00] admin");
            sb.AppendLine("\t\tcmd args='/K takeown /f \"@sel.path\" @if(sel.type==1,null,\"/r /d y\") && icacls \"@sel.path\" /grant *S-1-5-32-544:F @if(sel.type==1,\"/c /l\",\"/t /c /l /q\")')");
            sb.AppendLine("\tseparator");
        }

        if (f.ShowHideMenu)
        {
            sb.AppendLine($"\tmenu(title={L(useLoc, "show_hide", "Show/Hide")} image=icon.show_hidden_files)");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\titem(title={L(useLoc, "system_files", "System files")} image=inherit cmd='@command.togglehidden')");
            sb.AppendLine($"\t\titem(title={L(useLoc, "file_name_extensions", "File name extensions")} image=icon.show_file_extensions cmd='@command.toggleext')");
            sb.AppendLine("\t}");
            sb.AppendLine();
        }

        if (f.AttributesMenu)
        {
            sb.AppendLine($"\tmenu(type='file|dir|back.dir' mode=\"single\" title={L(useLoc, "attributes", "Attributes")} image=icon.properties)");
            sb.AppendLine("\t{");
            sb.AppendLine("\t\t$atrr = io.attributes(sel.path)");
            sb.AppendLine($"\t\titem(title={L(useLoc, "hidden", "Hidden")} checked=io.attribute.hidden(atrr)");
            sb.AppendLine("\t\t\tcmd args='/c ATTRIB @if(io.attribute.hidden(atrr),\"-\",\"+\")H \"@sel.path\"' window=hidden)");
            sb.AppendLine($"\t\titem(title={L(useLoc, "system", "System")} checked=io.attribute.system(atrr)");
            sb.AppendLine("\t\t\tcmd args='/c ATTRIB @if(io.attribute.system(atrr),\"-\",\"+\")S \"@sel.path\"' window=hidden)");
            sb.AppendLine($"\t\titem(title={L(useLoc, "readonly", "Read-Only")} checked=io.attribute.readonly(atrr)");
            sb.AppendLine("\t\t\tcmd args='/c ATTRIB @if(io.attribute.readonly(atrr),\"-\",\"+\")R \"@sel.path\"' window=hidden)");
            sb.AppendLine($"\t\titem(title={L(useLoc, "archive", "Archive")} checked=io.attribute.archive(atrr)");
            sb.AppendLine("\t\t\tcmd args='/c ATTRIB @if(io.attribute.archive(atrr),\"-\",\"+\")A \"@sel.path\"' window=hidden)");
            sb.AppendLine("\t\tseparator");
            sb.AppendLine($"\t\titem(title={L(useLoc, "created", "Created")} keys=io.dt.created(sel.path, 'y/m/d') cmd=io.dt.created(sel.path,2000,1,1) vis=label)");
            sb.AppendLine($"\t\titem(title={L(useLoc, "modified", "Modified")} keys=io.dt.modified(sel.path, 'y/m/d') cmd=io.dt.modified(sel.path,2000,1,1) vis=label)");
            sb.AppendLine($"\t\titem(title={L(useLoc, "accessed", "Accessed")} keys=io.dt.accessed(sel.path, 'y/m/d') cmd=io.dt.accessed(sel.path,2000,1,1) vis=label)");
            sb.AppendLine("\t}");
            sb.AppendLine();
        }

        if (f.RegisterServerMenu)
        {
            sb.AppendLine($"\tmenu(mode=\"single\" type='file' find='.dll|.ocx' separator=\"before\" title={L(useLoc, "register_server", "Register Server")} image=\\uea86)");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\titem(title={L(useLoc, "register", "Register")} admin cmd='regsvr32.exe' args='@sel.path.quote' invoke=\"multiple\")");
            sb.AppendLine($"\t\titem(title={L(useLoc, "unregister", "Unregister")} admin cmd='regsvr32.exe' args='/u @sel.path.quote' invoke=\"multiple\")");
            sb.AppendLine("\t}");
            sb.AppendLine();
        }

        if (f.NewFolderMenu || f.NewFileMenu)
        {
            sb.AppendLine("\tmenu(mode=\"single\" type='back' expanded=true)");
            sb.AppendLine("\t{");
            if (f.NewFolderMenu)
            {
                sb.AppendLine($"\t\tmenu(separator=\"before\" title={L(useLoc, "new_folder", "New Folder")} image=icon.new_folder)");
                sb.AppendLine("\t\t{");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "datetime", "DateTime")} cmd=io.dir.create(sys.datetime(\"ymdHMSs\")))");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "guid", "Guid")} cmd=io.dir.create(str.guid))");
                sb.AppendLine("\t\t}");
            }
            if (f.NewFileMenu)
            {
                sb.AppendLine($"\t\tmenu(title={L(useLoc, "new_file", "New File")} image=icon.new_file)");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\t$dt = sys.datetime(\"ymdHMSs\")");
                sb.AppendLine("\t\t\titem(title='TXT' cmd=io.file.create('@(dt).txt', 'Hello World!'))");
                sb.AppendLine("\t\t\titem(title='XML' cmd=io.file.create('@(dt).xml', '<root>Hello World!</root>'))");
                sb.AppendLine("\t\t\titem(title='JSON' cmd=io.file.create('@(dt).json', '[]'))");
                sb.AppendLine("\t\t\titem(title='HTML' cmd=io.file.create('@(dt).html', \"<html>\\n\\t<head>\\n\\t</head>\\n\\t<body>Hello World!\\n\\t</body>\\n</html>\"))");
                sb.AppendLine("\t\t}");
            }
            sb.AppendLine("\t}");
            sb.AppendLine();
        }

        if (f.FolderOptions)
            sb.AppendLine("\titem(where=!wnd.is_desktop title=title.folder_options image=icon.folder_options cmd=command.folder_options)");

        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string GenerateDevelopNss(DevelopSettings d, bool useLoc = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"menu(mode=\"multiple\" title={L(useLoc, "develop", "&Develop")} sep=sep.bottom image=\\uE26E)");
        sb.AppendLine("{");

        if (d.EditorsMenu && (d.VsCode || d.Notepad))
        {
            sb.AppendLine($"\tmenu(mode=\"single\" title={L(useLoc, "editors", "editors")} image=\\uE17A)");
            sb.AppendLine("\t{");
            if (d.VsCode)
                sb.AppendLine("\t\titem(title='Visual Studio Code' image=[\\uE272, #22A7F2] cmd='code' args='\"@sel.path\"')");
            if (d.VsCode && d.Notepad)
                sb.AppendLine("\t\tseparator");
            if (d.Notepad)
                sb.AppendLine($"\t\titem(type='file' mode=\"single\" title={L(useLoc, "windows_notepad", "Windows notepad")} image cmd='@sys.bin\\notepad.exe' args='\"@sel.path\"')");
            sb.AppendLine("\t}");
            sb.AppendLine();
        }

        if (d.DotnetMenu)
        {
            sb.AppendLine("\tmenu(mode=\"multiple\" title='dotnet' image=\\uE143)");
            sb.AppendLine("\t{");
            if (d.DotnetRunWatchClean)
            {
                sb.AppendLine("\t\titem(title='run' cmd-line='/K dotnet run' image=\\uE149)");
                sb.AppendLine("\t\titem(title='watch' cmd-line='/K dotnet watch')");
                sb.AppendLine("\t\titem(title='clean' image=\\uE0CE cmd-line='/K dotnet clean')");
                sb.AppendLine("\t\tseparator");
            }
            if (d.DotnetBuild)
            {
                sb.AppendLine("\t\titem(title='build debug' cmd-line='/K dotnet build')");
                sb.AppendLine("\t\titem(title='build release' cmd-line='/K dotnet build -c release /p:DebugType=None')");
            }
            if (d.DotnetPublish)
            {
                sb.AppendLine("\t\tmenu(mode=\"multiple\" sep=\"both\" title='publish' image=\\ue11f)");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\t$publish='dotnet publish -r win-x64 -c release --output publish /*/p:CopyOutputSymbolsToPublishDirectory=false*/'");
                sb.AppendLine("\t\t\titem(title='publish single file' sep=\"after\" cmd-line='/K @publish --no-self-contained /p:PublishSingleFile=true')");
                sb.AppendLine("\t\t\titem(title='framework-dependent deployment' cmd-line='/K @publish')");
                sb.AppendLine("\t\t\titem(title='framework-dependent executable' cmd-line='/K @publish --self-contained false')");
                sb.AppendLine("\t\t\titem(title='self-contained deployment' cmd-line='/K @publish --self-contained true')");
                sb.AppendLine("\t\t\titem(title='single-file' cmd-line='/K @publish /p:PublishSingleFile=true /p:PublishTrimmed=false')");
                sb.AppendLine("\t\t\titem(title='single-file-trimmed' cmd-line='/K @publish /p:PublishSingleFile=true /p:PublishTrimmed=true')");
                sb.AppendLine("\t\t}");
            }
            if (d.DotnetEf)
            {
                sb.AppendLine("\t\titem(title='ef migrations add InitialCreate' cmd-line='/K dotnet ef migrations add InitialCreate')");
                sb.AppendLine("\t\titem(title='ef database update' cmd-line='/K dotnet ef database update')");
            }
            if (d.DotnetHelpInfo)
            {
                sb.AppendLine("\t\tseparator");
                sb.AppendLine("\t\titem(title='help' image=\\uE136 cmd-line='/k dotnet -h')");
                sb.AppendLine("\t\titem(title='version' cmd-line='/k dotnet --info')");
            }
            sb.AppendLine("\t}");
            sb.AppendLine();
        }

        if (d.Reshack)
            sb.AppendLine($"\titem(type=\"file\" title=\"reshack\" sep=\"top\" image cmd='{EscapeSingleQuoted(d.ReshackPath)}' args=sel(1))");

        if (d.Peview)
            sb.AppendLine($"\titem(type=\"file\" title=\"peview\" image cmd='{EscapeSingleQuoted(d.PeviewPath)}' args=sel(1))");

        if (d.WebServer)
            sb.AppendLine($"\titem(type=\"dir|dir.back\" title=\"Web server\" vis=key.shift() cmd='{EscapeSingleQuoted(d.WebServerPath)}' args='-open -path:\"@sel.path\"')");

        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string GenerateGotoNss(GotoSettings g, bool useLoc = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("menu(type='*' where=window.is_taskbar||sel.count mode=mode.multiple title=title.go_to sep=sep.both image=\\uE14A)");
        sb.AppendLine("{");

        if (g.FoldersMenu)
        {
            sb.AppendLine($"\tmenu(title={L(useLoc, "folder", "Folder")} image=\\uE1F4)");
            sb.AppendLine("\t{");
            if (g.FolderWindows) sb.AppendLine("\t\titem(title='Windows' image=inherit cmd=sys.dir)");
            if (g.FolderSystem) sb.AppendLine("\t\titem(title='System' image=inherit cmd=sys.bin)");
            if (g.FolderProgramFiles) sb.AppendLine("\t\titem(title='Program Files' image=inherit cmd=sys.prog)");
            if (g.FolderProgramFilesX86) sb.AppendLine("\t\titem(title='Program Files x86' image=inherit cmd=sys.prog32)");
            if (g.FolderProgramData) sb.AppendLine("\t\titem(title='ProgramData' image=inherit cmd=sys.programdata)");
            if (g.FolderApplications) sb.AppendLine("\t\titem(title='Applications' image=inherit cmd='shell:appsfolder')");
            if (g.FolderUsers) sb.AppendLine("\t\titem(title='Users' image=inherit cmd=sys.users)");

            bool hasSys = g.FolderWindows || g.FolderSystem || g.FolderProgramFiles || g.FolderProgramFilesX86 || g.FolderProgramData || g.FolderApplications || g.FolderUsers;
            bool hasUsr = g.FolderDesktop || g.FolderDownloads || g.FolderPictures || g.FolderDocuments || g.FolderStartMenu || g.FolderProfile || g.FolderAppData || g.FolderTemp;
            if (hasSys && hasUsr)
                sb.AppendLine("\t\tseparator");

            if (g.FolderDesktop) sb.AppendLine("\t\titem(title='Desktop' image=inherit cmd=user.desktop)");
            if (g.FolderDownloads) sb.AppendLine("\t\titem(title='Downloads' image=inherit cmd=user.downloads)");
            if (g.FolderPictures) sb.AppendLine("\t\titem(title='Pictures' image=inherit cmd=user.pictures)");
            if (g.FolderDocuments) sb.AppendLine("\t\titem(title='Documents' image=inherit cmd=user.documents)");
            if (g.FolderStartMenu) sb.AppendLine("\t\titem(title='Startmenu' image=inherit cmd=user.startmenu)");
            if (g.FolderProfile) sb.AppendLine("\t\titem(title='Profile' image=inherit cmd=user.dir)");
            if (g.FolderAppData) sb.AppendLine("\t\titem(title='AppData' image=inherit cmd=user.appdata)");
            if (g.FolderTemp) sb.AppendLine("\t\titem(title='Temp' image=inherit cmd=user.temp)");
            sb.AppendLine("\t}");
        }

        if (g.ControlPanel)
            sb.AppendLine("\titem(title=title.control_panel image=\\uE0F3 cmd='shell:::{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}')");
        if (g.AllControlPanelItems)
            sb.AppendLine($"\titem(title={L(useLoc, "all_control_panel_items", "All Control Panel Items")} image=\\uE0F3 cmd='shell:::{{ED7BA470-8E54-465E-825C-99712043E01C}}')");
        if (g.RunDialog)
            sb.AppendLine("\titem(title=title.run image=\\uE14B cmd='shell:::{2559a1f3-21d7-11d4-bdaf-00c04f60b9f0}')");

        if (g.WindowsSettingsMenu)
        {
            sb.AppendLine("\tmenu(where=sys.ver.major >= 10 title=title.settings sep=sep.before image=\\uE0F3)");
            sb.AppendLine("\t{");
            if (g.SettingsSystem)
            {
                sb.AppendLine($"\t\titem(title={L(useLoc, "system", "System")} image=inherit cmd='ms-settings:')");
                sb.AppendLine($"\t\titem(title={L(useLoc, "about", "About")} image=inherit cmd='ms-settings:about')");
                sb.AppendLine($"\t\titem(title={L(useLoc, "your_info", "Your Info")} image=inherit cmd='ms-settings:yourinfo')");
                sb.AppendLine($"\t\titem(title={L(useLoc, "system_info", "System Info")} image=inherit cmd-line='/K systeminfo')");
                sb.AppendLine($"\t\titem(title={L(useLoc, "search", "Search")} cmd='search-ms:' image=inherit)");
                sb.AppendLine($"\t\titem(title={L(useLoc, "usb", "USB")} image=inherit cmd='ms-settings:usb')");
            }
            if (g.SettingsUpdateDefender)
            {
                sb.AppendLine($"\t\titem(title={L(useLoc, "windows_update", "Windows Update")} image=inherit cmd='ms-settings:windowsupdate')");
                sb.AppendLine($"\t\titem(title={L(useLoc, "windows_defender", "Windows Defender")} image=inherit cmd='ms-settings:windowsdefender')");
            }
            if (g.SettingsApps)
            {
                sb.AppendLine($"\t\tmenu(title={L(useLoc, "apps", "Apps")} image=inherit)");
                sb.AppendLine("\t\t{");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "apps_features", "Apps Features")} image=inherit cmd='ms-settings:appsfeatures')");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "default_apps", "Default Apps")} image=inherit cmd='ms-settings:defaultapps')");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "optional_features", "Optional Features")} image=inherit cmd='ms-settings:optionalfeatures')");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "startup", "Startup")} image=inherit cmd='ms-settings:startupapps')");
                sb.AppendLine("\t\t}");
            }
            if (g.SettingsPersonalization)
            {
                sb.AppendLine($"\t\tmenu(title={L(useLoc, "personalization", "Personalization")} image=inherit)");
                sb.AppendLine("\t\t{");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "personalization", "Personalization")} image=inherit cmd='ms-settings:personalization')");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "lockscreen", "Lockscreen")} image=inherit cmd='ms-settings:lockscreen')");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "background", "Background")} image=inherit cmd='ms-settings:personalization-background')");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "colors", "Colors")} image=inherit cmd='ms-settings:colors')");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "themes", "Themes")} image=inherit cmd='ms-settings:themes')");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "start", "Start")} image=inherit cmd='ms-settings:personalization-start')");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "taskbar", "Taskbar")} image=inherit cmd='ms-settings:taskbar')");
                sb.AppendLine("\t\t}");
            }
            if (g.SettingsNetwork)
            {
                sb.AppendLine($"\t\tmenu(title={L(useLoc, "network", "Network")} image=inherit)");
                sb.AppendLine("\t\t{");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "status", "Status")} image=inherit cmd='ms-settings:network-status')");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "ethernet", "Ethernet")} image=inherit cmd='ms-settings:network-ethernet')");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "wifi", "WLAN")} image=inherit cmd='ms-settings:network-wifi')");
                sb.AppendLine($"\t\t\titem(title={L(useLoc, "connections", "Connections")} image=inherit cmd='shell:::{{7007ACC7-3202-11D1-AAD2-00805FC1270E}}')");
                sb.AppendLine("\t\t}");
            }
            sb.AppendLine("\t}");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string GenerateTaskbarNss(TaskbarSettings t, bool useLoc = false)
    {
        var sb = new StringBuilder();
        if (t.ShellAdminMenu)
        {
            sb.AppendLine("menu(type=\"taskbar\" vis=key.shift() or key.lbutton() pos=0 title=app.name image=\\uE249)");
            sb.AppendLine("{");
            if (t.IncludeConfiguratorShortcut)
                sb.AppendLine("\titem(title=\"configurator\" image=\\uE0F3 cmd='\"@app.dir\\ShellConfigurator.exe\"')");
            sb.AppendLine("\titem(title=\"config\" image=\\uE10A cmd='\"@app.cfg\"')");
            sb.AppendLine("\titem(title=\"manager\" image=\\uE0F3 admin cmd='\"@app.exe\"')");
            sb.AppendLine("\titem(title=\"directory\" image=\\uE0E8 cmd='\"@app.dir\"')");
            sb.AppendLine("\titem(title=\"version\\t\"+@app.ver vis=label col=1)");
            sb.AppendLine("\titem(title=\"docs\" image=\\uE1C4 cmd='https://nilesoft.org/docs')");
            sb.AppendLine("\titem(title=\"donate\" image=\\uE1A7 cmd='https://nilesoft.org/donate')");
            sb.AppendLine("}");
        }

        sb.AppendLine("menu(where=@(this.count == 0) type=\"taskbar\" image=icon.settings expanded=true)");
        sb.AppendLine("{");

        if (t.AppsMenu && (t.AppPaint || t.AppEdge || t.AppCalculator || t.AppRegedit))
        {
            sb.AppendLine($"\tmenu(title={L(useLoc, "apps", "Apps")} image=\\uE254)");
            sb.AppendLine("\t{");
            if (t.AppPaint) sb.AppendLine($"\t\titem(title={L(useLoc, "paint", "Paint")} image=\\uE116 cmd='mspaint')");
            if (t.AppEdge) sb.AppendLine($"\t\titem(title={L(useLoc, "edge", "Edge")} image cmd='@sys.prog32\\Microsoft\\Edge\\Application\\msedge.exe')");
            if (t.AppCalculator) sb.AppendLine($"\t\titem(title={L(useLoc, "calculator", "Calculator")} image=\\ue1e7 cmd='calc.exe')");
            if (t.AppRegedit) sb.AppendLine("\t\titem(title=str.res('regedit.exe,-16') image cmd='regedit.exe')");
            sb.AppendLine("\t}");
        }

        if (t.WindowsMenu)
        {
            sb.AppendLine($"\tmenu(title={L(useLoc, "windows", "Windows")} image=\\uE1FB)");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\titem(title={L(useLoc, "cascade_windows", "Cascade windows")} cmd=command.cascade_windows)");
            sb.AppendLine($"\t\titem(title={L(useLoc, "Show_windows_stacked", "Show windows stacked")} cmd=command.Show_windows_stacked)");
            sb.AppendLine($"\t\titem(title={L(useLoc, "Show_windows_side_by_side", "Show windows side by side")} cmd=command.Show_windows_side_by_side)");
            sb.AppendLine("\t\tsep");
            sb.AppendLine($"\t\titem(title={L(useLoc, "minimize_all_windows", "Minimize all windows")} cmd=command.minimize_all_windows)");
            sb.AppendLine($"\t\titem(title={L(useLoc, "restore_all_windows", "Restore all windows")} cmd=command.restore_all_windows)");
            sb.AppendLine("\t}");
        }

        if (t.ShowDesktop)
            sb.AppendLine("\titem(title=title.desktop image=icon.desktop cmd=command.toggle_desktop)");
        if (t.WindowsSettings)
            sb.AppendLine("\titem(title=title.settings image=icon.settings(auto, image.color1) cmd='ms-settings:')");
        if (t.TaskManager)
            sb.AppendLine($"\titem(title={L(useLoc, "task_manager", "Task Manager")} sep=both image=icon.task_manager cmd='taskmgr.exe')");
        if (t.TaskbarSettingsLink)
            sb.AppendLine($"\titem(title={L(useLoc, "taskbar_Settings", "Taskbar Settings")} sep=both image=inherit cmd='ms-settings:taskbar')");
        if (t.RestartExplorerOnShift)
            sb.AppendLine("\titem(vis=key.shift() title=title.exit_explorer cmd=command.restart_explorer)");

        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string GenerateUserNss(IEnumerable<CustomMenuItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Custom user context menu entries");
        var enabledItems = items.Where(i => i.Enabled && !string.IsNullOrWhiteSpace(i.Title)).ToList();
        if (enabledItems.Count == 0)
        {
            sb.AppendLine("// Add custom entries from Nilesoft Shell Configurator.");
            return sb.ToString();
        }

        var topLevel = enabledItems.Where(i => string.IsNullOrWhiteSpace(i.Submenu)).ToList();
        var grouped = enabledItems
            .Where(i => !string.IsNullOrWhiteSpace(i.Submenu))
            .GroupBy(i => i.Submenu.Trim(), StringComparer.OrdinalIgnoreCase);

        foreach (var item in topLevel)
            sb.AppendLine(FormatCustomItem(item, ""));

        foreach (var group in grouped)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine($"menu(type='*' title=\"{EscapeDoubleQuoted(group.Key)}\" image=\\uE14D)");
            sb.AppendLine("{");
            foreach (var item in group)
                sb.AppendLine(FormatCustomItem(item, "\t"));
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static string FormatCustomItem(CustomMenuItem item, string indent)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.ContextType))
            parts.Add($"type='{EscapeSingleQuoted(item.ContextType.Trim())}'");

        parts.Add($"title=\"{EscapeDoubleQuoted(item.Title.Trim())}\"");

        if (!string.IsNullOrWhiteSpace(item.Separator) && !item.Separator.Equals("none", StringComparison.OrdinalIgnoreCase))
            parts.Add($"sep=\"{EscapeDoubleQuoted(item.Separator.Trim())}\"");

        if (!string.IsNullOrWhiteSpace(item.Icon))
        {
            string icon = item.Icon.Trim();
            if (icon.StartsWith("\\u", StringComparison.OrdinalIgnoreCase) || icon.StartsWith("icon.", StringComparison.OrdinalIgnoreCase) || icon.Equals("inherit", StringComparison.OrdinalIgnoreCase))
                parts.Add($"image={icon}");
            else
                parts.Add($"image='{EscapeSingleQuoted(icon)}'");
        }

        if (item.RunAsAdmin)
            parts.Add("admin");

        if (!string.IsNullOrWhiteSpace(item.Command))
            parts.Add($"cmd='{EscapeSingleQuoted(item.Command.Trim())}'");

        if (!string.IsNullOrWhiteSpace(item.Arguments))
            parts.Add($"args='{EscapeSingleQuoted(item.Arguments.Trim())}'");

        return $"{indent}item({string.Join(" ", parts)})";
    }

    public static (ShellConfig Config, bool ImportedFromOriginalNss) LoadFromDirectoryWithMeta(string directory)
    {
        bool hasLangFolder = File.Exists(Path.Combine(directory, "imports", "lang", "en.nss"));

        string sidecarPath = Path.Combine(directory, SidecarFileName);
        if (File.Exists(sidecarPath))
        {
            try
            {
                string json = File.ReadAllText(sidecarPath, Encoding.UTF8);
                var loaded = JsonSerializer.Deserialize<ShellConfig>(json, JsonOptions);
                if (loaded != null)
                {
                    // Guard against v1.9 installs that don't support `import lang`
                    if (!hasLangFolder)
                        loaded.General.UseLocalization = false;
                    return (loaded, false);
                }
            }
            catch
            {
                // Fall back to parsing .nss files directly
            }
        }

        var config = ShellConfig.CreateDefault();
        bool foundNss = ParseExistingNssFiles(directory, config);
        if (!hasLangFolder)
            config.General.UseLocalization = false;
        return (config, foundNss);
    }

    public static ShellConfig LoadFromDirectory(string directory)
        => LoadFromDirectoryWithMeta(directory).Config;

    public static void SaveToDirectory(string directory, ShellConfig config, bool createBackup = true)
    {
        Directory.CreateDirectory(directory);
        string importsDir = Path.Combine(directory, "imports");
        Directory.CreateDirectory(importsDir);

        // Safety guard: never emit `import lang` if imports\lang\en.nss does not exist (v1.9.x compatibility)
        if (config.General.UseLocalization && !File.Exists(Path.Combine(importsDir, "lang", "en.nss")))
            config.General.UseLocalization = false;

        var generated = GenerateAllFiles(config);

        if (createBackup && File.Exists(Path.Combine(directory, "shell.nss")))
        {
            string backupDir = Path.Combine(directory, "backups", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(Path.Combine(backupDir, "imports"));
            foreach (string relPath in generated.Keys)
            {
                string src = Path.Combine(directory, relPath);
                if (File.Exists(src))
                {
                    string dst = Path.Combine(backupDir, relPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                    File.Copy(src, dst, overwrite: true);
                }
            }
        }

        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        foreach (var (relPath, content) in generated)
        {
            string fullPath = Path.Combine(directory, relPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content, utf8NoBom);
        }

        string sidecarPath = Path.Combine(directory, SidecarFileName);
        File.WriteAllText(sidecarPath, JsonSerializer.Serialize(config, JsonOptions), utf8NoBom);
    }

    private static bool ParseExistingNssFiles(string directory, ShellConfig config)
    {
        bool parsedAny = false;
        bool hasLangFiles = File.Exists(Path.Combine(directory, "imports", "lang", "en.nss"));

        string shellPath = Path.Combine(directory, "shell.nss");
        if (File.Exists(shellPath))
        {
            parsedAny = true;
            string active = StripComments(File.ReadAllText(shellPath));

            config.General.Priority = MatchInt(active, @"priority\s*=\s*(\d+)", config.General.Priority);
            config.General.ShowDelay = MatchInt(active, @"showdelay\s*=\s*(\d+)", config.General.ShowDelay);
            config.General.ExcludeNonExplorer = Regex.IsMatch(active, @"exclude\.where\s*=\s*!process\.is_explorer", RegexOptions.IgnoreCase);
            config.General.RemoveDuplicates = MatchInt(active, @"modify\.remove\.duplicate\s*=\s*(\d+)", 1) != 0;
            config.General.TipsEnabled = !Regex.IsMatch(active, @"tip\.enabled\s*=\s*(false|0)", RegexOptions.IgnoreCase);

            // Only enable `import lang` if the existing config uses it AND lang files exist
            config.General.UseLocalization = hasLangFiles && Regex.IsMatch(active, @"\bimport\s+lang\b", RegexOptions.IgnoreCase);

            if (!active.Contains("sys.lang", StringComparison.OrdinalIgnoreCase))
            {
                var langMatches = Regex.Matches(active, @"import\s+lang\s+.*?[""']([a-zA-Z]{2}(?:-[a-zA-Z]{2})?)\.nss[""']", RegexOptions.IgnoreCase);
                if (langMatches.Count > 0)
                    config.General.Language = langMatches[^1].Groups[1].Value;
            }
            else
            {
                config.General.Language = "auto";
            }

            config.General.EnableThemeImport = IsImportEnabled(active, "imports/theme.nss");
            config.General.EnableImagesImport = IsImportEnabled(active, "imports/images.nss");
            config.General.EnableModifyImport = IsImportEnabled(active, "imports/modify.nss");
            config.General.EnableTerminalImport = IsImportEnabled(active, "imports/terminal.nss");
            config.General.EnableFileManageImport = IsImportEnabled(active, "imports/file-manage.nss");
            config.General.EnableDevelopImport = IsImportEnabled(active, "imports/develop.nss");
            config.General.EnableGotoImport = IsImportEnabled(active, "imports/goto.nss");
            config.General.EnableTaskbarImport = IsImportEnabled(active, "imports/taskbar.nss");
            config.General.EnableCustomImport = IsImportEnabled(active, "imports/user.nss");
            config.General.EnablePinUnpinMenu = ContainsAny(active, "pin_unpin", "Pin/Unpin");
            config.General.EnableMoreOptionsMenu = ContainsAny(active, "more_options");

            foreach (Match m in Regex.Matches(active, @"^\s*import\s+['""]([^'""]+\.nss)['""]", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                string imp = m.Groups[1].Value.Replace('\\', '/');
                if (!StandardImports.Contains(imp) && !config.General.PreservedCustomImports.Contains(imp, StringComparer.OrdinalIgnoreCase))
                    config.General.PreservedCustomImports.Add(imp);
            }
        }

        string themePath = Path.Combine(directory, "imports", "theme.nss");
        if (File.Exists(themePath))
        {
            parsedAny = true;
            string active = StripComments(File.ReadAllText(themePath));

            var nameMatch = Regex.Match(active, @"\bname\s*=\s*[""']([^""']+)[""']");
            if (nameMatch.Success) config.Theme.Name = nameMatch.Groups[1].Value;

            var viewMatch = Regex.Match(active, @"\bview\s*=\s*(?:view\.)?(auto|compact|small|medium|large|wide)", RegexOptions.IgnoreCase);
            if (viewMatch.Success) config.Theme.View = viewMatch.Groups[1].Value.ToLowerInvariant();

            var darkMatch = Regex.Match(active, @"\bdark\s*=\s*(auto|true|false|default|0|1)", RegexOptions.IgnoreCase);
            if (darkMatch.Success)
            {
                config.Theme.DarkMode = darkMatch.Groups[1].Value.ToLowerInvariant() switch
                {
                    "1" or "true" => "true",
                    "0" or "false" => "false",
                    _ => "auto"
                };
            }

            string bgBlock = ExtractBlock(active, "background");
            if (!string.IsNullOrEmpty(bgBlock))
            {
                var colorMatch = Regex.Match(bgBlock, @"\bcolor\s*=\s*([^\r\n\}]+)", RegexOptions.IgnoreCase);
                if (colorMatch.Success) config.Theme.BackgroundColor = colorMatch.Groups[1].Value.Trim();

                var opMatch = Regex.Match(bgBlock, @"\bopacity\s*=\s*([^\r\n\}]+)", RegexOptions.IgnoreCase);
                if (opMatch.Success)
                {
                    string opVal = opMatch.Groups[1].Value.Trim();
                    if (int.TryParse(opVal, out int opInt))
                    {
                        config.Theme.UseAutoOpacity = false;
                        config.Theme.BackgroundOpacity = opInt;
                    }
                    else
                    {
                        config.Theme.UseAutoOpacity = true;
                    }
                }

                var effectMatch = Regex.Match(bgBlock, @"\beffect\s*=\s*([^\r\n\}]+)", RegexOptions.IgnoreCase);
                if (effectMatch.Success)
                {
                    string val = effectMatch.Groups[1].Value.Trim().ToLowerInvariant();
                    config.Theme.BackgroundEffect = val switch
                    {
                        "0" => "none",
                        "1" => "transparent",
                        "2" => "blur",
                        "3" => "acrylic",
                        _ when val.StartsWith("[3") => "acrylic",
                        _ => "auto"
                    };
                }
            }

            string borderBlock = ExtractBlock(active, "border");
            if (!string.IsNullOrEmpty(borderBlock))
            {
                config.Theme.BorderEnabled = !Regex.IsMatch(borderBlock, @"\benabled\s*=\s*(false|0)", RegexOptions.IgnoreCase);
                config.Theme.BorderSize = MatchInt(borderBlock, @"\bsize\s*=\s*(\d+)", config.Theme.BorderSize);
                config.Theme.BorderRadius = MatchInt(borderBlock, @"\bradius\s*=\s*(\d+)", config.Theme.BorderRadius);
            }

            string shadowBlock = ExtractBlock(active, "shadow");
            if (!string.IsNullOrEmpty(shadowBlock))
            {
                config.Theme.ShadowEnabled = !Regex.IsMatch(shadowBlock, @"\benabled\s*=\s*(false|0)", RegexOptions.IgnoreCase);
                config.Theme.ShadowSize = MatchInt(shadowBlock, @"\bsize\s*=\s*(\d+)", config.Theme.ShadowSize);
                config.Theme.ShadowOpacity = MatchInt(shadowBlock, @"\bopacity\s*=\s*(\d+)", config.Theme.ShadowOpacity);
            }

            string fontBlock = ExtractBlock(active, "font");
            if (!string.IsNullOrEmpty(fontBlock))
            {
                config.Theme.UseCustomFont = true;
                var fnMatch = Regex.Match(fontBlock, @"\bname\s*=\s*[""']([^""']+)[""']");
                if (fnMatch.Success) config.Theme.FontName = fnMatch.Groups[1].Value;
                config.Theme.FontSize = MatchInt(fontBlock, @"\bsize\s*=\s*(\d+)", config.Theme.FontSize);
            }
            else if (Regex.IsMatch(active, @"\bfont\.size\s*=\s*(\d+)", RegexOptions.IgnoreCase))
            {
                config.Theme.UseCustomFont = true;
                config.Theme.FontSize = MatchInt(active, @"\bfont\.size\s*=\s*(\d+)", config.Theme.FontSize);
            }

            config.Theme.ImageAlign = MatchInt(active, @"image\.align\s*=\s*(\d+)", config.Theme.ImageAlign);
        }

        string modifyPath = Path.Combine(directory, "imports", "modify.nss");
        if (File.Exists(modifyPath))
        {
            parsedAny = true;
            string active = StripComments(File.ReadAllText(modifyPath));
            var m = config.Modify;
            m.RemoveRestorePreviousVersions = ContainsToken(active, "id.restore_previous_versions");
            m.RemoveCastToDevice = ContainsToken(active, "id.cast_to_device");
            m.PromoteEmptyRecycleBin = ContainsToken(active, "id.empty_recycle_bin");
            m.GroupPinUnpin = ContainsToken(active, "Pin/Unpin");
            m.MoveCopyAsPathToFileManage = ContainsToken(active, "id.copy_as_path");
            m.MoveCustomizeFolderToFileManage = ContainsToken(active, "id.customize_this_folder");
            m.MoveTerminalItemsToTerminal = ContainsAny(active, "open_powershell_window_here", "open in terminal");

            m.MoreSendTo = ContainsToken(active, "id.send_to");
            m.MoreShare = ContainsToken(active, "id.share");
            m.MoreCreateShortcut = ContainsToken(active, "id.create_shortcut");
            m.MoreSetDesktopBackground = ContainsToken(active, "id.set_as_desktop_background");
            m.MoreRotateLeft = ContainsToken(active, "id.rotate_left");
            m.MoreRotateRight = ContainsToken(active, "id.rotate_right");
            m.MoreMapNetworkDrive = ContainsToken(active, "id.map_network_drive");
            m.MoreDisconnectNetworkDrive = ContainsToken(active, "id.disconnect_network_drive");
            m.MoreFormat = ContainsToken(active, "id.format");
            m.MoreEject = ContainsToken(active, "id.eject");
            m.MoreGiveAccessTo = ContainsToken(active, "id.give_access_to");
            m.MoreIncludeInLibrary = ContainsToken(active, "id.include_in_library");
            m.MorePrint = ContainsToken(active, "id.print");
        }

        string termPath = Path.Combine(directory, "imports", "terminal.nss");
        if (File.Exists(termPath))
        {
            parsedAny = true;
            string active = StripComments(File.ReadAllText(termPath));
            config.Terminal.EnableAdminModifier = ContainsToken(active, "has_admin");
            config.Terminal.CommandPrompt = ContainsAny(active, "command_prompt", "cmd-prompt");
            config.Terminal.WindowsPowerShell = ContainsAny(active, "windows_powershell", "cmd-ps");
            config.Terminal.WindowsTerminal = ContainsAny(active, "WindowsTerminal", "wt.exe");
        }

        string fmPath = Path.Combine(directory, "imports", "file-manage.nss");
        if (File.Exists(fmPath))
        {
            parsedAny = true;
            string active = StripComments(File.ReadAllText(fmPath));
            var f = config.FileManage;
            f.CopyPathMenu = ContainsToken(active, "copy_path");
            f.CopyMultiplePaths = ContainsAny(active, "copy_multiple_paths", "items selected");
            f.CopyFullPath = ContainsToken(active, "command.copy(sel.path)");
            f.CopyShortcutTarget = ContainsAny(active, "sel.lnk", "'.lnk'");
            f.CopyParentPath = ContainsToken(active, "sel.parent");
            f.CopyFileName = ContainsToken(active, "sel.file.name");
            f.CopyFileTitle = ContainsToken(active, "sel.file.title");
            f.CopyFileExtension = ContainsToken(active, "sel.file.ext");
            f.ChangeExtension = ContainsAny(active, "change_extension", "Change extension");
            f.SelectMenu = ContainsAny(active, "title.select", "command.select_all");
            f.TakeOwnership = ContainsAny(active, "take_ownership", "Take Ownership", "takeown");
            f.ShowHideMenu = ContainsAny(active, "show_hide", "Show/Hide", "togglehidden");
            f.AttributesMenu = ContainsAny(active, "loc.attributes", "'Attributes'", "\"Attributes\"", "io.attributes");
            f.RegisterServerMenu = ContainsAny(active, "register_server", "Register Server", "regsvr32");
            f.NewFolderMenu = ContainsAny(active, "new_folder", "New Folder", "io.dir.create");
            f.NewFileMenu = ContainsAny(active, "new_file", "New File", "io.file.create");
            f.FolderOptions = ContainsToken(active, "folder_options");
        }

        string devPath = Path.Combine(directory, "imports", "develop.nss");
        if (File.Exists(devPath))
        {
            parsedAny = true;
            string active = StripComments(File.ReadAllText(devPath));
            var d = config.Develop;
            d.EditorsMenu = ContainsAny(active, "loc.editors", "'editors'", "\"editors\"");
            d.VsCode = ContainsToken(active, "Visual Studio Code");
            d.Notepad = ContainsAny(active, "windows_notepad", "notepad.exe");
            d.DotnetMenu = ContainsAny(active, "'dotnet'", "\"dotnet\"");
            d.DotnetRunWatchClean = ContainsToken(active, "dotnet run");
            d.DotnetBuild = ContainsToken(active, "dotnet build");
            d.DotnetPublish = ContainsToken(active, "dotnet publish");
            d.DotnetEf = ContainsToken(active, "dotnet ef");
            d.DotnetHelpInfo = ContainsAny(active, "dotnet -h", "dotnet --info");

            d.Reshack = ContainsToken(active, "reshack");
            d.Peview = ContainsToken(active, "peview");
            d.WebServer = ContainsToken(active, "Web server");

            d.ReshackPath = ExtractItemCmd(active, "reshack", d.ReshackPath);
            d.PeviewPath = ExtractItemCmd(active, "peview", d.PeviewPath);
            d.WebServerPath = ExtractItemCmd(active, "Web server", d.WebServerPath);
        }

        string gotoPath = Path.Combine(directory, "imports", "goto.nss");
        if (File.Exists(gotoPath))
        {
            parsedAny = true;
            string active = StripComments(File.ReadAllText(gotoPath));
            var g = config.Goto;
            g.FoldersMenu = ContainsAny(active, "loc.folder", "'Folder'", "\"Folder\"");
            g.FolderWindows = ContainsToken(active, "cmd=sys.dir");
            g.FolderSystem = ContainsToken(active, "cmd=sys.bin");
            g.FolderProgramFiles = ContainsToken(active, "cmd=sys.prog)");
            g.FolderProgramFilesX86 = ContainsToken(active, "cmd=sys.prog32");
            g.FolderProgramData = ContainsToken(active, "cmd=sys.programdata");
            g.FolderApplications = ContainsToken(active, "shell:appsfolder");
            g.FolderUsers = ContainsToken(active, "cmd=sys.users");
            g.FolderDesktop = ContainsToken(active, "cmd=user.desktop");
            g.FolderDownloads = ContainsToken(active, "cmd=user.downloads");
            g.FolderPictures = ContainsToken(active, "cmd=user.pictures");
            g.FolderDocuments = ContainsToken(active, "cmd=user.documents");
            g.FolderStartMenu = ContainsToken(active, "cmd=user.startmenu");
            g.FolderProfile = ContainsToken(active, "cmd=user.dir");
            g.FolderAppData = ContainsToken(active, "cmd=user.appdata");
            g.FolderTemp = ContainsToken(active, "cmd=user.temp");

            g.ControlPanel = ContainsToken(active, "control_panel");
            g.AllControlPanelItems = ContainsAny(active, "all_control_panel_items", "All Control Panel Items");
            g.RunDialog = ContainsToken(active, "title.run");

            g.WindowsSettingsMenu = ContainsToken(active, "title.settings");
            g.SettingsSystem = ContainsToken(active, "ms-settings:about");
            g.SettingsUpdateDefender = ContainsToken(active, "ms-settings:windowsupdate");
            g.SettingsApps = ContainsToken(active, "ms-settings:appsfeatures");
            g.SettingsPersonalization = ContainsToken(active, "ms-settings:personalization");
            g.SettingsNetwork = ContainsToken(active, "ms-settings:network-status");
        }

        string taskbarPath = Path.Combine(directory, "imports", "taskbar.nss");
        if (File.Exists(taskbarPath))
        {
            parsedAny = true;
            string active = StripComments(File.ReadAllText(taskbarPath));
            var tb = config.Taskbar;
            tb.ShellAdminMenu = ContainsToken(active, "title=app.name");
            tb.IncludeConfiguratorShortcut = ContainsToken(active, "ShellConfigurator.exe");
            tb.AppsMenu = ContainsAny(active, "loc.apps", "'Apps'", "\"Apps\"", "mspaint");
            tb.AppPaint = ContainsToken(active, "mspaint");
            tb.AppEdge = ContainsToken(active, "msedge.exe");
            tb.AppCalculator = ContainsToken(active, "calc.exe");
            tb.AppRegedit = ContainsToken(active, "regedit.exe");
            tb.WindowsMenu = ContainsAny(active, "loc.windows", "cascade_windows");
            tb.ShowDesktop = ContainsToken(active, "toggle_desktop");
            tb.WindowsSettings = ContainsAny(active, "cmd='ms-settings:'", "cmd=\"ms-settings:\"");
            tb.TaskManager = ContainsToken(active, "taskmgr.exe");
            tb.TaskbarSettingsLink = ContainsToken(active, "ms-settings:taskbar");
            tb.RestartExplorerOnShift = ContainsToken(active, "restart_explorer");
        }

        string userPath = Path.Combine(directory, "imports", "user.nss");
        if (File.Exists(userPath))
        {
            parsedAny = true;
            ParseUserItems(StripComments(File.ReadAllText(userPath)), config);
        }

        return parsedAny;
    }

    private static void ParseUserItems(string active, ShellConfig config)
    {
        config.CustomItems.Clear();
        string currentSubmenu = "";
        int braceDepth = 0;

        foreach (string rawLine in active.Split('\n'))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var menuMatch = Regex.Match(line, @"^menu\s*\(.*?title\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (menuMatch.Success)
                currentSubmenu = menuMatch.Groups[1].Value;

            if (line.Contains('{')) braceDepth++;
            if (line.Contains('}'))
            {
                braceDepth = Math.Max(0, braceDepth - 1);
                if (braceDepth == 0) currentSubmenu = "";
            }

            if (!line.StartsWith("item(", StringComparison.OrdinalIgnoreCase))
                continue;

            var titleMatch = Regex.Match(line, @"title\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (!titleMatch.Success) continue;

            var typeMatch = Regex.Match(line, @"type\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            var cmdMatch = Regex.Match(line, @"cmd\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            var argsMatch = Regex.Match(line, @"args\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            var sepMatch = Regex.Match(line, @"sep\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            var imgMatch = Regex.Match(line, @"image\s*=\s*(?:[""']([^""']+)[""']|([^\s\)]+))", RegexOptions.IgnoreCase);

            config.CustomItems.Add(new CustomMenuItem
            {
                Enabled = true,
                Title = titleMatch.Groups[1].Value,
                ContextType = typeMatch.Success ? typeMatch.Groups[1].Value : "*",
                Submenu = braceDepth > 0 ? currentSubmenu : "",
                Command = cmdMatch.Success ? cmdMatch.Groups[1].Value : "",
                Arguments = argsMatch.Success ? argsMatch.Groups[1].Value : "",
                Separator = sepMatch.Success ? sepMatch.Groups[1].Value : "none",
                Icon = imgMatch.Success ? (imgMatch.Groups[1].Success ? imgMatch.Groups[1].Value : imgMatch.Groups[2].Value) : "",
                RunAsAdmin = Regex.IsMatch(line, @"\badmin\b", RegexOptions.IgnoreCase)
            });
        }
    }

    private static string StripComments(string text)
    {
        string noBlock = Regex.Replace(text, @"/\*[\s\S]*?\*/", "");
        var sb = new StringBuilder(noBlock.Length);
        foreach (string line in noBlock.Split('\n'))
        {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("//")) continue;
            sb.AppendLine(line.TrimEnd('\r'));
        }
        return sb.ToString();
    }

    private static string ExtractBlock(string text, string blockName)
    {
        var m = Regex.Match(text, $@"\b{Regex.Escape(blockName)}\s*\{{([^{{}}]*)\}}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return m.Success ? m.Groups[1].Value : string.Empty;
    }

    private static string ExtractItemCmd(string text, string titleToken, string fallback)
    {
        var m = Regex.Match(text, $@"title\s*=\s*[""']{Regex.Escape(titleToken)}[""'][^\r\n]*?cmd\s*=\s*['""]([^'""]+)['""]", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : fallback;
    }

    private static bool ContainsToken(string text, string token)
        => text.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static bool ContainsAny(string text, params string[] tokens)
        => tokens.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));

    private static bool IsImportEnabled(string text, string importTarget)
    {
        string normalized = importTarget.Replace('/', '\\');
        string pattern = $@"^\s*import\s+['""](?:{Regex.Escape(importTarget)}|{Regex.Escape(normalized)})['""]";
        return Regex.IsMatch(text, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
    }

    private static int MatchInt(string text, string pattern, int fallback)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return m.Success && int.TryParse(m.Groups[1].Value, out int val) ? val : fallback;
    }

    private static void AppendImport(StringBuilder sb, bool enabled, string path)
        => sb.AppendLine(enabled ? $"import '{path}'" : $"// import '{path}'");

    private static string MapEffectValue(string effect) => effect.ToLowerInvariant() switch
    {
        "none" => "0",
        "transparent" => "1",
        "blur" => "2",
        "acrylic" => "3",
        _ => "auto"
    };

    private static string EscapeDoubleQuoted(string value)
        => value.Replace("\"", "\\\"");

    private static string EscapeSingleQuoted(string value)
        => value.Replace("'", "\\'");
}
