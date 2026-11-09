using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ArkFontConverter;

public partial class Form1 : Form
{
    private const string MainPatchBaseName = "ShooterGame-Windows_P";
    private static readonly string[] MainPatchFiles =
    {
        $"{MainPatchBaseName}.pak",
        $"{MainPatchBaseName}.utoc",
        $"{MainPatchBaseName}.ucas"
    };
    private static readonly string[] NameplateFiles = { "MyMod_P.pak", "MyMod_P.utoc", "MyMod_P.ucas" };

    // Empty IoStore companion pair verified with ASA UE 5.8. The game now skips a
    // standalone legacy PAK, but it still loads the PAK when this valid pair exists.
    private const string Ue58UtocBase64 = "LT09LS09PS0tPT0tLT09LQgAAACQAAAAAQAAAAEAAAAMAAAAAAAAACAAAAAAAAEAAAAAAAEAAADAKrXjhJI2aAAAAAAAAAAAAAAAAAAAAAAIAAAAAAAAAP//////////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwCq144SSNmgAAAAGAAAAAAAAAAAAQAAAAAAAQAAAQAAAAB31uDG/hwcpVQzIF4t1RRRjl0mNAAAAAA==";
    private const string Ue58UcasBase64 = "bkNvSQUAAADAKrXjhJI2aAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA8AAAAAAAAAAQAAAAAAAAAAAAAAA==";

    private readonly TextBox _installPath = new();
    private readonly ComboBox _language = new();
    private readonly TextBox _customFontFolder = new();
    private readonly CheckBox _backup = new();
    private readonly CheckBox _openOutput = new();
    private readonly TableLayoutPanel _partsTable = new();
    private readonly TextBox _log = new();
    private readonly List<PartRow> _rows = new();
    private readonly Dictionary<string, string> _fonts = new(StringComparer.OrdinalIgnoreCase);

    private string BaseDir => AppContext.BaseDirectory;
    private string DataDir => Path.Combine(BaseDir, "DATA");
    private string BundledFontsDir => Path.Combine(BaseDir, "FontHere");
    private string NameplateTemplateDir => Path.Combine(DataDir, "NameplateTemplate");
    private string UserFontsDir => Path.Combine(BaseDir, "UserFonts");
    private string RepakPath => Path.Combine(DataDir, "Tools", "repak.exe");
    private string SettingsPath => Path.Combine(BaseDir, "settings.json");

    public Form1()
    {
        InitializeComponent();
        BuildUi();
        CreateParts();
        var settings = LoadSettings();
        LoadFonts();
        ApplySavedPartSettings(settings);
        ApplyLanguage();
        FormClosing += (_, _) => SaveSettings();
    }

    private void BuildUi()
    {
        Text = "ARK_SA_FontChanger - UE 5.8";
        Width = 1120;
        Height = 760;
        MinimumSize = new Size(1000, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(14) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            Text = "아크서바이벌 어센디드 폰트 변환 도구",
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 0);

        var top = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 5, AutoSize = true };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        root.Controls.Add(top, 0, 1);

        _installPath.Dock = DockStyle.Fill;
        _installPath.Margin = new Padding(0, 0, 8, 8);
        top.Controls.Add(_installPath, 0, 0);

        var browseGame = NewButton("BrowseGame");
        browseGame.Click += (_, _) => BrowseInstallPath();
        top.Controls.Add(browseGame, 1, 0);

        _language.DropDownStyle = ComboBoxStyle.DropDownList;
        _language.Items.AddRange(new object[] { "한국어", "English", "日本語" });
        _language.SelectedIndex = 0;
        _language.Margin = new Padding(0, 0, 0, 8);
        _language.SelectedIndexChanged += (_, _) => ApplyLanguage();
        top.Controls.Add(_language, 4, 0);

        _backup.Name = "Backup";
        _backup.Checked = true;
        _backup.AutoSize = true;
        _backup.Margin = new Padding(0, 3, 16, 8);
        top.Controls.Add(_backup, 2, 0);

        _openOutput.Name = "OpenOutput";
        _openOutput.Checked = true;
        _openOutput.AutoSize = true;
        _openOutput.Margin = new Padding(0, 3, 0, 8);
        top.Controls.Add(_openOutput, 3, 0);

        _customFontFolder.Dock = DockStyle.Fill;
        _customFontFolder.Text = UserFontsDir;
        _customFontFolder.Margin = new Padding(0, 0, 8, 8);
        top.Controls.Add(_customFontFolder, 0, 1);

        var browseFonts = NewButton("BrowseFonts");
        browseFonts.Click += (_, _) => BrowseFontFolder();
        top.Controls.Add(browseFonts, 1, 1);

        var refreshFonts = NewButton("RefreshFonts");
        refreshFonts.Click += (_, _) => LoadFonts();
        top.Controls.Add(refreshFonts, 2, 1);

        var restore = NewButton("RestoreOriginal");
        restore.Click += (_, _) => RestoreOriginalPak();
        top.Controls.Add(restore, 3, 1);

        var resetDefault = NewButton("ResetDefault");
        resetDefault.Click += async (_, _) => await ResetDefaultsAsync();
        top.Controls.Add(resetDefault, 4, 1);

        var installNameplate = NewButton("InstallNameplateTemplate");
        installNameplate.Click += (_, _) => InstallNameplateTemplate();
        top.Controls.Add(installNameplate, 0, 2);

        var removeNameplate = NewButton("RemoveNameplateTemplate");
        removeNameplate.Click += (_, _) => RemoveNameplateTemplate();
        top.Controls.Add(removeNameplate, 1, 2);

        var partsBox = new GroupBox { Name = "PartsBox", Dock = DockStyle.Fill, Padding = new Padding(10) };
        root.Controls.Add(partsBox, 0, 2);

        _partsTable.Dock = DockStyle.Top;
        _partsTable.AutoSize = true;
        _partsTable.ColumnCount = 4;
        _partsTable.RowCount = 1;
        _partsTable.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
        _partsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        _partsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _partsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        _partsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        scroll.Controls.Add(_partsTable);
        partsBox.Controls.Add(scroll);

        AddHeader("UseHeader", 0);
        AddHeader("PartHeader", 1);
        AddHeader("RegularHeader", 2);
        AddHeader("BoldHeader", 3);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, RowCount = 3, AutoSize = true };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(bottom, 0, 3);

        bottom.Controls.Add(new Label { Name = "TipLabel", Dock = DockStyle.Fill, AutoSize = true, ForeColor = Color.FromArgb(70, 70, 70), Margin = new Padding(0, 6, 8, 8) }, 0, 0);
        bottom.Controls.Add(new Label { Name = "Tip2Label", Dock = DockStyle.Fill, AutoSize = true, ForeColor = Color.FromArgb(70, 70, 70), Margin = new Padding(0, 0, 8, 8) }, 0, 1);
        bottom.Controls.Add(new Label { Name = "Tip3Label", Dock = DockStyle.Fill, AutoSize = true, ForeColor = Color.FromArgb(70, 70, 70), Margin = new Padding(0, 0, 8, 8) }, 0, 2);

        var build = NewButton("BuildPak");
        build.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        build.Click += async (_, _) => await BuildPakAsync();
        bottom.Controls.Add(build, 1, 0);
        bottom.SetRowSpan(build, 3);

        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        root.Controls.Add(_log, 0, 4);

        root.Controls.Add(new Label
        {
            Name = "FooterLabel",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            ForeColor = Color.FromArgb(70, 70, 70),
            Margin = new Padding(0, 8, 0, 0)
        }, 0, 5);
    }

    private static Button NewButton(string name) => new() { Name = name, AutoSize = true, Margin = new Padding(0, 0, 8, 8) };

    private void AddHeader(string key, int column)
    {
        _partsTable.Controls.Add(new Label { Name = key, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(4) }, column, 0);
    }

    private void CreateParts()
    {
        AddPart("Part1Label", true, new[]
        {
            "ShooterGame/Content/PrimalEarth/UI/Fonts/NotoSerifKR-Regular.ufont",
            "ShooterGame/Content/PrimalEarth/UI/Fonts/NotoSerifKR-Bold.ufont"
        });
        AddPart("Part2Label", true, new[]
        {
            "Engine/Content/EngineFonts/Faces/RobotoRegular.ufont",
            "Engine/Content/EngineFonts/Faces/RobotoBold.ufont",
            "Engine/Content/EngineFonts/Faces/RobotoLight.ufont"
        });

        for (var i = 0; i < _rows.Count; i++)
            _rows[i].Build(_partsTable, i + 1);
    }

    private void AddPart(string labelKey, bool enabled, string[] targets) => _rows.Add(new PartRow(labelKey, enabled, targets));

    private AppSettings? LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return null;
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath, Encoding.UTF8));
            if (settings is null) return null;

            if (!string.IsNullOrWhiteSpace(settings.InstallPath)) _installPath.Text = settings.InstallPath;
            if (!string.IsNullOrWhiteSpace(settings.CustomFontFolder)) _customFontFolder.Text = settings.CustomFontFolder;
            if (settings.LanguageIndex >= 0 && settings.LanguageIndex < _language.Items.Count) _language.SelectedIndex = settings.LanguageIndex;
            _backup.Checked = settings.Backup;
            _openOutput.Checked = settings.OpenOutput;
            return settings;
        }
        catch
        {
            return null;
        }
    }

    private void ApplySavedPartSettings(AppSettings? settings)
    {
        if (settings?.Parts is null) return;
        foreach (var saved in settings.Parts)
        {
            var row = _rows.FirstOrDefault(x => x.LabelKey == saved.LabelKey);
            if (row is null) continue;
            row.EnabledCheck.Checked = saved.Enabled;
            SelectIfExists(row.RegularFont, saved.RegularFont);
            SelectIfExists(row.BoldFont, saved.BoldFont);
        }
    }

    private void SaveSettings()
    {
        try
        {
            var settings = new AppSettings
            {
                InstallPath = _installPath.Text,
                CustomFontFolder = _customFontFolder.Text,
                LanguageIndex = _language.SelectedIndex,
                Backup = _backup.Checked,
                OpenOutput = _openOutput.Checked,
                Parts = _rows.Select(row => new PartSettings
                {
                    LabelKey = row.LabelKey,
                    Enabled = row.EnabledCheck.Checked,
                    RegularFont = row.RegularFont.Text,
                    BoldFont = row.BoldFont.Text
                }).ToList()
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json, Encoding.UTF8);
        }
        catch
        {
            // Settings are convenience-only; failing to save should not block the tool.
        }
    }

    private void LoadFonts()
    {
        Directory.CreateDirectory(UserFontsDir);
        _fonts.Clear();
        AddFontFilesFrom(BundledFontsDir);
        AddFontFilesFrom(_customFontFolder.Text);

        var names = _fonts.Keys.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToArray();
        foreach (var row in _rows)
            row.SetFonts(names);

        Log("FontsLoaded", _fonts.Count.ToString());
    }

    private void AddFontFilesFrom(string folder)
    {
        if (!Directory.Exists(folder)) return;
        foreach (var path in Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly))
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (!IsSupportedFontFile(ext)) continue;
            _fonts[Path.GetFileNameWithoutExtension(path)] = path;
        }
    }

    private static bool IsSupportedFontFile(string ext) => ext is ".ttf" or ".otf" or ".ttc" or ".otc";

    private void BrowseInstallPath()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK) _installPath.Text = dialog.SelectedPath;
    }

    private void BrowseFontFolder()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _customFontFolder.Text = dialog.SelectedPath;
            LoadFonts();
        }
    }

    private async Task BuildPakAsync()
    {
        try
        {
            var paksDir = RequirePaksDir();
            if (paksDir is null) return;
            EnsureArkIsClosed();
            if (!File.Exists(RepakPath)) throw new FileNotFoundException("repak.exe", RepakPath);

            var selected = _rows.Where(r => r.EnabledCheck.Checked).ToList();
            if (selected.Count == 0) { MessageBox.Show(this, T("SelectPart")); return; }

            foreach (var row in selected)
                if (!_fonts.ContainsKey(row.RegularFont.Text) || !_fonts.ContainsKey(row.BoldFont.Text))
                {
                    MessageBox.Show(this, T("SelectFont"));
                    return;
                }

            var work = NewTemp("ASAFontTool_Font_");
            var originalPak = Path.Combine(paksDir, MainPatchFiles[0]);
            if (File.Exists(originalPak))
            {
                if (MessageBox.Show(this, T("ExistingPatchConfirm"), T("Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                BackupCoreFiles(paksDir);
                Log("UnpackOriginal", originalPak);
                LogRaw(await RunProcessAsync(RepakPath, $"unpack \"{originalPak}\" --output \"{work}\" --force"));
            }
            else
            {
                BackupCoreFiles(paksDir);
                Log("CreateNewPatchPak", originalPak);
                Directory.CreateDirectory(work);
            }

            foreach (var row in selected)
            {
                var regular = _fonts[row.RegularFont.Text];
                var bold = _fonts[row.BoldFont.Text];
                foreach (var target in row.Targets)
                {
                    CopyToPakTree(work, target, IsBoldSlot(Path.GetFileName(target).ToLowerInvariant()) ? bold : regular);
                }
            }

            await PackAndReplaceAsync(work, originalPak);
            MessageBox.Show(this, T("PakDone"), "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (_openOutput.Checked) OpenFolder(paksDir);
        }
        catch (Exception ex)
        {
            LogRaw(ex.ToString());
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ResetDefaultsAsync()
    {
        try
        {
            var paksDir = RequirePaksDir();
            if (paksDir is null) return;
            EnsureArkIsClosed();
            if (!File.Exists(RepakPath)) throw new FileNotFoundException("repak.exe", RepakPath);
            BackupCoreFiles(paksDir);

            DeleteIfExists(Path.Combine(paksDir, "zzz_ASAFontTool_FontOverride_P.pak"));
            DeleteIfExists(Path.Combine(paksDir, "_Mods", "~Mods", "zzz_ASAFontTool_FontOverride_P.pak"));
            DeleteIfExists(Path.Combine(paksDir, "_Mods", "~Mods", "zzz_ASAFontTool_KERISKEDU_AllSlots_P.pak"));

            var work = NewTemp("ASAFontTool_Default_");
            var originalPak = Path.Combine(paksDir, MainPatchFiles[0]);
            if (File.Exists(originalPak))
            {
                Log("UnpackOriginal", originalPak);
                LogRaw(await RunProcessAsync(RepakPath, $"unpack \"{originalPak}\" --output \"{work}\" --force"));
            }
            CopyToPakTree(work, "ShooterGame/Content/PrimalEarth/UI/Fonts/NotoSerifKR-Regular.ufont", Path.Combine(BundledFontsDir, "NotoSansCJKkr-Black.otf"));
            CopyToPakTree(work, "ShooterGame/Content/PrimalEarth/UI/Fonts/NotoSerifKR-Bold.ufont", Path.Combine(BundledFontsDir, "NotoSansCJKkr-Bold.otf"));

            await PackAndReplaceAsync(work, originalPak);
            SetDefaultDropdowns();
            Log("DefaultsRestored");
            MessageBox.Show(this, T("DefaultsDone"), "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            LogRaw(ex.ToString());
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void InstallNameplateTemplate()
    {
        try
        {
            var paksDir = RequirePaksDir();
            if (paksDir is null) return;
            EnsureArkIsClosed();
            var modsDir = Path.Combine(paksDir, "~Mods");
            Directory.CreateDirectory(modsDir);
            foreach (var file in NameplateFiles)
                if (!File.Exists(Path.Combine(NameplateTemplateDir, file)))
                    throw new FileNotFoundException(T("NameplateTemplateMissing"), Path.Combine(NameplateTemplateDir, file));

            var exists = NameplateFiles.Any(f => File.Exists(Path.Combine(modsDir, f)));
            if (exists && MessageBox.Show(this, T("OverwriteNameplate"), T("Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var legacyModsDir = Path.Combine(paksDir, "_Mods", "~Mods");
            var nameplatePaths = NameplateFiles.Select(file => Path.Combine("~Mods", file))
                .Concat(NameplateFiles.Select(file => Path.Combine("_Mods", "~Mods", file)));
            BackupFileSet(paksDir, nameplatePaths);

            foreach (var file in NameplateFiles)
            {
                File.Copy(Path.Combine(NameplateTemplateDir, file), Path.Combine(modsDir, file), overwrite: true);
                DeleteIfExists(Path.Combine(legacyModsDir, file));
            }
            Log("NameplateInstalled");
            MessageBox.Show(this, T("NameplateInstallDone"), "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            LogRaw(ex.ToString());
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RemoveNameplateTemplate()
    {
        try
        {
            var paksDir = RequirePaksDir();
            if (paksDir is null) return;
            EnsureArkIsClosed();
            var modsDir = Path.Combine(paksDir, "~Mods");
            var legacyModsDir = Path.Combine(paksDir, "_Mods", "~Mods");
            var nameplatePaths = NameplateFiles.Select(file => Path.Combine("~Mods", file))
                .Concat(NameplateFiles.Select(file => Path.Combine("_Mods", "~Mods", file)));
            BackupFileSet(paksDir, nameplatePaths);
            foreach (var file in NameplateFiles)
            {
                DeleteIfExists(Path.Combine(modsDir, file));
                DeleteIfExists(Path.Combine(legacyModsDir, file));
            }
            Log("NameplateRemoved");
            MessageBox.Show(this, T("NameplateRemoveDone"), "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            LogRaw(ex.ToString());
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RestoreOriginalPak()
    {
        try
        {
            var paksDir = RequirePaksDir();
            if (paksDir is null) return;
            EnsureArkIsClosed();
            var backupDir = Path.Combine(paksDir, "FontToolBackup");
            var latestSet = Directory.Exists(backupDir)
                ? Directory.EnumerateDirectories(backupDir)
                    .OrderByDescending(dir => Path.GetFileName(dir))
                    .FirstOrDefault(dir => File.Exists(Path.Combine(dir, MainPatchFiles[0])))
                : null;

            if (latestSet is not null)
            {
                BackupCoreFiles(paksDir);
                foreach (var file in MainPatchFiles)
                {
                    var source = Path.Combine(latestSet, file);
                    if (File.Exists(source)) File.Copy(source, Path.Combine(paksDir, file), overwrite: true);
                }
                EnsureUe58Companions(paksDir);
                Log("OriginalRestored", latestSet);
            }
            else
            {
                var legacyBackup = Directory.Exists(backupDir)
                    ? Directory.EnumerateFiles(backupDir, $"{MainPatchFiles[0]}.*.bak").OrderByDescending(File.GetLastWriteTime).FirstOrDefault()
                    : null;
                if (legacyBackup is null) { MessageBox.Show(this, T("NoBackup")); return; }
                BackupCoreFiles(paksDir);
                File.Copy(legacyBackup, Path.Combine(paksDir, MainPatchFiles[0]), overwrite: true);
                EnsureUe58Companions(paksDir);
                Log("OriginalRestored", legacyBackup);
            }
            MessageBox.Show(this, T("RestoreDone"), "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            LogRaw(ex.ToString());
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string? RequirePaksDir()
    {
        var paksDir = ResolvePaksDir(_installPath.Text);
        if (paksDir is null) MessageBox.Show(this, T("PaksNotFound"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return paksDir;
    }

    private void BackupCoreFiles(string paksDir)
    {
        BackupFileSet(paksDir, MainPatchFiles);
    }

    private void BackupFileSet(string paksDir, IEnumerable<string> relativeFiles)
    {
        if (!_backup.Checked) return;
        var existing = relativeFiles.Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(relative => File.Exists(Path.Combine(paksDir, relative)))
            .ToList();
        if (existing.Count == 0) return;

        var snapshotDir = Path.Combine(paksDir, "FontToolBackup", DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
        foreach (var relative in existing)
        {
            var source = Path.Combine(paksDir, relative);
            var destination = Path.Combine(snapshotDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: false);
        }
        Log("BackupCreated", snapshotDir);
    }

    private async Task PackAndReplaceAsync(string work, string targetPak)
    {
        var targetDir = Path.GetDirectoryName(targetPak)!;
        var tempPak = Path.Combine(targetDir, $".{Path.GetFileName(targetPak)}.{DateTime.Now:yyyyMMdd_HHmmss_fff}.tmp");
        Log("Packing", tempPak);
        try
        {
            LogRaw(await RunProcessAsync(RepakPath, $"pack \"{work}\" \"{tempPak}\" --version V8B --mount-point ../../../"));
            if (!File.Exists(tempPak) || new FileInfo(tempPak).Length == 0)
                throw new InvalidDataException(T("PakBuildFailed"));
            File.Copy(tempPak, targetPak, overwrite: true);
            EnsureUe58Companions(targetDir);
            Log("Replaced", targetPak);
            Log("Ue58CompanionsReady");
        }
        finally
        {
            DeleteIfExists(tempPak);
        }
    }

    private void EnsureUe58Companions(string paksDir)
    {
        var utoc = Path.Combine(paksDir, MainPatchFiles[1]);
        var ucas = Path.Combine(paksDir, MainPatchFiles[2]);
        if (HasMagic(utoc, Encoding.ASCII.GetBytes("-==--==--==--==-")) &&
            HasMagic(ucas, Encoding.ASCII.GetBytes("nCoI"))) return;

        File.WriteAllBytes(utoc, Convert.FromBase64String(Ue58UtocBase64));
        File.WriteAllBytes(ucas, Convert.FromBase64String(Ue58UcasBase64));
        if (!HasMagic(utoc, Encoding.ASCII.GetBytes("-==--==--==--==-")) ||
            !HasMagic(ucas, Encoding.ASCII.GetBytes("nCoI")))
            throw new InvalidDataException(T("CompanionBuildFailed"));
    }

    private static bool HasMagic(string path, byte[] magic)
    {
        if (!File.Exists(path)) return false;
        using var stream = File.OpenRead(path);
        if (stream.Length < magic.Length) return false;
        var buffer = new byte[magic.Length];
        return stream.Read(buffer, 0, buffer.Length) == buffer.Length && buffer.SequenceEqual(magic);
    }

    private void EnsureArkIsClosed()
    {
        if (Process.GetProcessesByName("ArkAscended").Length > 0)
            throw new InvalidOperationException(T("CloseGameFirst"));
    }

    private void SetDefaultDropdowns()
    {
        foreach (var row in _rows)
        {
            SelectIfExists(row.RegularFont, "NotoSansCJKkr-Black");
            SelectIfExists(row.BoldFont, "NotoSansCJKkr-Bold");
        }
    }

    private static void SelectIfExists(ComboBox combo, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        foreach (var item in combo.Items)
            if (string.Equals(item?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
    }

    private string NewTemp(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), prefix + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static bool IsBoldSlot(string name) => name.Contains("bold") || name.Contains("black") || name.Contains("gobold") || name.Contains("sansation");

    private static void CopyToPakTree(string root, string target, string source)
    {
        var dest = Path.Combine(root, target.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(source, dest, overwrite: true);
    }

    private static string? ResolvePaksDir(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var path = input.Trim().Trim('"');
        if (!Directory.Exists(path)) return null;
        if (Path.GetFileName(path).Equals("Paks", StringComparison.OrdinalIgnoreCase)) return path;
        var direct = Path.Combine(path, "ShooterGame", "Content", "Paks");
        if (Directory.Exists(direct)) return direct;
        var nested = Path.Combine(path, "ARK Survival Ascended", "ShooterGame", "Content", "Paks");
        return Directory.Exists(nested) ? nested : null;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static void OpenFolder(string path) => Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });

    private static async Task<string> RunProcessAsync(string fileName, string args)
    {
        var start = new ProcessStartInfo(fileName, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = (await stdout) + (await stderr);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} exited with code {process.ExitCode}.{Environment.NewLine}{output}".Trim());
        return output;
    }

    private void Log(string key, params string[] args) => LogRaw(string.Format(T(key), args));

    private void LogRaw(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text.Trim()}{Environment.NewLine}");
    }

    private void ApplyLanguage()
    {
        foreach (Control c in EnumerateControls(this))
            if (c.Name.Length > 0 && Texts.TryGetValue((Lang, c.Name), out var text))
                c.Text = text;

        for (var i = 0; i < _rows.Count; i++)
            if (Texts.TryGetValue((Lang, _rows[i].LabelKey), out var text))
                _rows[i].TitleLabel.Text = text;
    }

    private string Lang => _language.SelectedIndex switch { 1 => "en", 2 => "ja", _ => "ko" };

    private string T(string key) => Texts.TryGetValue((Lang, key), out var value) ? value : Texts.GetValueOrDefault(("en", key), key);

    private static IEnumerable<Control> EnumerateControls(Control root)
    {
        foreach (Control c in root.Controls)
        {
            yield return c;
            foreach (var child in EnumerateControls(c)) yield return child;
        }
    }

    private static readonly Dictionary<(string Lang, string Key), string> Texts = new()
    {
        [("ko", "BrowseGame")] = "설치 경로 선택",
        [("ko", "BrowseFonts")] = "커스텀 폰트 폴더 선택",
        [("ko", "RefreshFonts")] = "커스텀폰트 새로불러오기",
        [("ko", "Backup")] = "패치 3파일 세트 백업",
        [("ko", "OpenOutput")] = "완료 후 폴더 열기",
        [("ko", "RestoreOriginal")] = "원본복원 (기본값 복원이 작동하지 않는 경우 사용)",
        [("ko", "ResetDefault")] = "기본값 복원",
        [("ko", "InstallNameplateTemplate")] = "이름표시 가독성 개선",
        [("ko", "RemoveNameplateTemplate")] = "이름표시 기본값 복원",
        [("ko", "PartsBox")] = "변환할 파트",
        [("ko", "UseHeader")] = "사용",
        [("ko", "PartHeader")] = "파트",
        [("ko", "RegularHeader")] = "Regular",
        [("ko", "BoldHeader")] = "Bold/Black",
        [("ko", "Part1Label")] = "파트 1 - 대부분의 UI텍스트와 채팅창",
        [("ko", "Part2Label")] = "파트 2 - 하단생물이름 / 부족로그 / 핫바 하단 폰트",
        [("ko", "TipLabel")] = "TIP1: 모국어와 영어, 숫자가 포함된 폰트를 추천합니다. (동북아시아권의 경우 한자 포함권장)",
        [("ko", "Tip2Label")] = "TIP2: 기존 번역 PAK는 내용을 유지하며 폰트만 바꿉니다. UE 5.8용 UTOC/UCAS가 없거나 잘못된 경우 자동 생성합니다.",
        [("ko", "Tip3Label")] = "TIP3: 한국어버전에서만 테스트를 완료하였으며 다른 언어에서 정상작동을 보장하지 않습니다.",
        [("ko", "FooterLabel")] = "- Youtube CHZZK 조마일즈 / mileschokr@gmail.com -",
        [("ko", "BuildPak")] = "폰트 적용하기",
        [("ko", "FontsLoaded")] = "폰트 {0}개를 불러왔습니다.",
        [("ko", "UnpackOriginal")] = "기존 패치를 풀고 있습니다: {0}",
        [("ko", "CreateNewPatchPak")] = "기존 ShooterGame-Windows_P.pak가 없어 새 폰트 패치를 만듭니다: {0}",
        [("ko", "Packing")] = "PAK을 생성 중입니다: {0}",
        [("ko", "Replaced")] = "교체 완료: {0}",
        [("ko", "PakDone")] = "UE 5.8용 PAK/UTOC/UCAS 생성 및 교체가 완료되었습니다.",
        [("ko", "DefaultsRestored")] = "기본값을 복원했습니다.",
        [("ko", "DefaultsDone")] = "번역 내용을 유지한 채 기본 폰트 복원이 완료되었습니다.",
        [("ko", "NameplateInstalled")] = "이름표시 가독성 개선 모드를 설치했습니다.",
        [("ko", "NameplateRemoved")] = "이름표시 모드를 제거했습니다.",
        [("ko", "NameplateInstallDone")] = "이름표시 가독성 개선 설치가 완료되었습니다.",
        [("ko", "NameplateRemoveDone")] = "이름표시 기본값 복원이 완료되었습니다.",
        [("ko", "OverwriteNameplate")] = "이름표시 개선 파일이 이미 설치되어 있습니다. 덮어쓰겠습니까?",
        [("ko", "ExistingPatchConfirm")] = "번역혹은 폰트 패치가 존재합니다. 해당 패치에 적용하시겠습니까?",
        [("ko", "Confirm")] = "확인",
        [("ko", "PaksNotFound")] = "Content\\Paks 폴더를 찾을 수 없습니다.",
        [("ko", "SelectPart")] = "최소 한 개 파트를 선택하세요.",
        [("ko", "SelectFont")] = "폰트를 선택하세요.",
        [("ko", "NoBackup")] = "복원할 백업을 찾을 수 없습니다.",
        [("ko", "OriginalRestored")] = "원본을 복원했습니다: {0}",
        [("ko", "RestoreDone")] = "원본 복원이 완료되었습니다.",
        [("ko", "BackupCreated")] = "3파일 백업을 만들었습니다: {0}",
        [("ko", "Ue58CompanionsReady")] = "UE 5.8용 UTOC/UCAS를 확인했습니다.",
        [("ko", "PakBuildFailed")] = "생성된 PAK가 없거나 비어 있습니다.",
        [("ko", "CompanionBuildFailed")] = "UE 5.8용 UTOC/UCAS 생성 검증에 실패했습니다.",
        [("ko", "CloseGameFirst")] = "ARK가 실행 중입니다. 게임을 완전히 종료한 뒤 다시 시도하세요.",
        [("ko", "NameplateTemplateMissing")] = "이름표 모드 템플릿 파일이 없습니다.",

        [("en", "BrowseGame")] = "Select install path",
        [("en", "BrowseFonts")] = "Select custom font folder",
        [("en", "RefreshFonts")] = "Reload custom fonts",
        [("en", "Backup")] = "Back up the three-file patch set",
        [("en", "OpenOutput")] = "Open folder when done",
        [("en", "RestoreOriginal")] = "Restore original (use if defaults fail)",
        [("en", "ResetDefault")] = "Restore defaults",
        [("en", "InstallNameplateTemplate")] = "Improve nameplate readability",
        [("en", "RemoveNameplateTemplate")] = "Restore nameplate defaults",
        [("en", "PartsBox")] = "Font targets",
        [("en", "UseHeader")] = "Use",
        [("en", "PartHeader")] = "Part",
        [("en", "RegularHeader")] = "Regular",
        [("en", "BoldHeader")] = "Bold/Black",
        [("en", "Part1Label")] = "Part 1 - Most UI text and chat window",
        [("en", "Part2Label")] = "Part 2 - Bottom creature name / tribe log / hotbar lower font",
        [("en", "TipLabel")] = "TIP1: Use a font that includes your native language, English, and numbers. East Asian users should prefer fonts with Chinese characters.",
        [("en", "Tip2Label")] = "TIP2: Existing translations are preserved while fonts are changed. Missing or invalid UE 5.8 UTOC/UCAS companions are generated automatically.",
        [("en", "Tip3Label")] = "TIP3: Testing has only been completed on the Korean version; normal operation in other languages is not guaranteed.",
        [("en", "FooterLabel")] = "- Youtube CHZZK Jomiles / mileschokr@gmail.com -",
        [("en", "BuildPak")] = "Apply font",
        [("en", "FontsLoaded")] = "Loaded {0} fonts.",
        [("en", "UnpackOriginal")] = "Unpacking existing patch: {0}",
        [("en", "CreateNewPatchPak")] = "ShooterGame-Windows_P.pak was not found. Creating a new font patch: {0}",
        [("en", "Packing")] = "Building PAK: {0}",
        [("en", "Replaced")] = "Replaced: {0}",
        [("en", "PakDone")] = "UE 5.8 PAK/UTOC/UCAS build and replacement completed.",
        [("en", "DefaultsRestored")] = "Defaults restored.",
        [("en", "DefaultsDone")] = "Default fonts restored without removing translations.",
        [("en", "NameplateInstalled")] = "Installed nameplate readability mod.",
        [("en", "NameplateRemoved")] = "Removed nameplate mod.",
        [("en", "NameplateInstallDone")] = "Nameplate readability install completed.",
        [("en", "NameplateRemoveDone")] = "Nameplate defaults restored.",
        [("en", "OverwriteNameplate")] = "Nameplate readability files are already installed. Overwrite them?",
        [("en", "ExistingPatchConfirm")] = "A translation or font patch already exists. Apply changes to that patch?",
        [("en", "Confirm")] = "Confirm",
        [("en", "PaksNotFound")] = "Content\\Paks folder was not found.",
        [("en", "SelectPart")] = "Select at least one part.",
        [("en", "SelectFont")] = "Select a font.",
        [("en", "NoBackup")] = "No backup was found.",
        [("en", "OriginalRestored")] = "Restored original: {0}",
        [("en", "RestoreDone")] = "Original restoration completed.",
        [("en", "BackupCreated")] = "Created a three-file backup: {0}",
        [("en", "Ue58CompanionsReady")] = "Verified the UE 5.8 UTOC/UCAS companions.",
        [("en", "PakBuildFailed")] = "The generated PAK is missing or empty.",
        [("en", "CompanionBuildFailed")] = "UE 5.8 UTOC/UCAS verification failed.",
        [("en", "CloseGameFirst")] = "ARK is running. Close the game completely and try again.",
        [("en", "NameplateTemplateMissing")] = "A nameplate mod template file is missing.",

        [("ja", "BrowseGame")] = "インストール先を選択",
        [("ja", "BrowseFonts")] = "カスタムフォントフォルダ選択",
        [("ja", "RefreshFonts")] = "カスタムフォント再読込",
        [("ja", "Backup")] = "3ファイルのパッチセットをバックアップ",
        [("ja", "OpenOutput")] = "完了後フォルダを開く",
        [("ja", "RestoreOriginal")] = "元に戻す（既定値復元が動作しない場合）",
        [("ja", "ResetDefault")] = "既定値に戻す",
        [("ja", "InstallNameplateTemplate")] = "名前表示の可読性改善",
        [("ja", "RemoveNameplateTemplate")] = "名前表示を既定値に戻す",
        [("ja", "PartsBox")] = "変換する部分",
        [("ja", "UseHeader")] = "使用",
        [("ja", "PartHeader")] = "部分",
        [("ja", "RegularHeader")] = "Regular",
        [("ja", "BoldHeader")] = "Bold/Black",
        [("ja", "Part1Label")] = "パート1 - 大半のUIテキストとチャット欄",
        [("ja", "Part2Label")] = "パート2 - 下部の生物名 / トライブログ / ホットバー下部フォント",
        [("ja", "TipLabel")] = "TIP1: 母国語、英語、数字を含むフォントをおすすめします。（東アジア圏では中国語を含むものを推奨）",
        [("ja", "Tip2Label")] = "TIP2: 既存の翻訳を保持したままフォントのみ変更します。UE 5.8用UTOC/UCASがない、または無効な場合は自動生成します。",
        [("ja", "Tip3Label")] = "TIP3: テストは韓国語版でのみ完了しており、他の言語での正常動作は保証しません。",
        [("ja", "FooterLabel")] = "- Youtube CHZZK ジョマイルズ / mileschokr@gmail.com -",
        [("ja", "BuildPak")] = "フォントを適用",
        [("ja", "FontsLoaded")] = "{0}個のフォントを読み込みました。",
        [("ja", "UnpackOriginal")] = "既存パッチを展開中: {0}",
        [("ja", "CreateNewPatchPak")] = "ShooterGame-Windows_P.pakがないため、新しいフォントパッチを作成します: {0}",
        [("ja", "Packing")] = "PAK作成中: {0}",
        [("ja", "Replaced")] = "置換完了: {0}",
        [("ja", "PakDone")] = "UE 5.8用PAK/UTOC/UCASの作成と置換が完了しました。",
        [("ja", "DefaultsRestored")] = "既定値を復元しました。",
        [("ja", "DefaultsDone")] = "翻訳を保持したまま既定フォントへ復元しました。",
        [("ja", "NameplateInstalled")] = "名前表示の可読性改善Modをインストールしました。",
        [("ja", "NameplateRemoved")] = "名前表示Modを削除しました。",
        [("ja", "NameplateInstallDone")] = "名前表示の可読性改善が完了しました。",
        [("ja", "NameplateRemoveDone")] = "名前表示を既定値に戻しました。",
        [("ja", "OverwriteNameplate")] = "名前表示改善ファイルは既にインストールされています。上書きしますか？",
        [("ja", "ExistingPatchConfirm")] = "翻訳またはフォントパッチがすでに存在します。そのパッチに適用しますか？",
        [("ja", "Confirm")] = "確認",
        [("ja", "PaksNotFound")] = "Content\\Paksフォルダが見つかりません。",
        [("ja", "SelectPart")] = "少なくとも1つの部分を選択してください。",
        [("ja", "SelectFont")] = "フォントを選択してください。",
        [("ja", "NoBackup")] = "復元用バックアップが見つかりません。",
        [("ja", "OriginalRestored")] = "元に戻しました: {0}",
        [("ja", "RestoreDone")] = "元への復元が完了しました。",
        [("ja", "BackupCreated")] = "3ファイルのバックアップを作成しました: {0}",
        [("ja", "Ue58CompanionsReady")] = "UE 5.8用UTOC/UCASを確認しました。",
        [("ja", "PakBuildFailed")] = "生成されたPAKがないか、空です。",
        [("ja", "CompanionBuildFailed")] = "UE 5.8用UTOC/UCASの検証に失敗しました。",
        [("ja", "CloseGameFirst")] = "ARKが実行中です。ゲームを完全に終了してから再試行してください。",
        [("ja", "NameplateTemplateMissing")] = "名前表示Modのテンプレートファイルがありません。",
    };

    private sealed class AppSettings
    {
        public string InstallPath { get; set; } = "";
        public string CustomFontFolder { get; set; } = "";
        public int LanguageIndex { get; set; }
        public bool Backup { get; set; } = true;
        public bool OpenOutput { get; set; } = true;
        public List<PartSettings> Parts { get; set; } = new();
    }

    private sealed class PartSettings
    {
        public string LabelKey { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public string RegularFont { get; set; } = "";
        public string BoldFont { get; set; } = "";
    }

    private sealed class PartRow
    {
        public string LabelKey { get; }
        public string[] Targets { get; }
        public CheckBox EnabledCheck { get; } = new();
        public ComboBox RegularFont { get; } = new();
        public ComboBox BoldFont { get; } = new();
        public Label TitleLabel { get; } = new();
        private readonly bool _checkedByDefault;

        public PartRow(string labelKey, bool checkedByDefault, string[] targets)
        {
            LabelKey = labelKey;
            _checkedByDefault = checkedByDefault;
            Targets = targets;
        }

        public void Build(TableLayoutPanel table, int row)
        {
            table.RowCount = row + 1;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            EnabledCheck.Checked = _checkedByDefault;
            EnabledCheck.Dock = DockStyle.Fill;
            EnabledCheck.CheckAlign = ContentAlignment.MiddleCenter;
            EnabledCheck.TextAlign = ContentAlignment.MiddleCenter;
            table.Controls.Add(EnabledCheck, 0, row);

            TitleLabel.Dock = DockStyle.Fill;
            TitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            TitleLabel.Padding = new Padding(10, 0, 0, 0);
            TitleLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            table.Controls.Add(TitleLabel, 1, row);

            RegularFont.DropDownStyle = ComboBoxStyle.DropDownList;
            RegularFont.Dock = DockStyle.Fill;
            table.Controls.Add(RegularFont, 2, row);

            BoldFont.DropDownStyle = ComboBoxStyle.DropDownList;
            BoldFont.Dock = DockStyle.Fill;
            table.Controls.Add(BoldFont, 3, row);
        }

        public void SetFonts(string[] fontNames)
        {
            SetCombo(RegularFont, fontNames, new[] { "NotoSansCJKkr-Black", "NotoSansCJKkr-Bold", "Medium", "Regular" });
            SetCombo(BoldFont, fontNames, new[] { "NotoSansCJKkr-Bold", "NotoSansCJKkr-Black", "Bold", "Black" });
        }

        private static void SetCombo(ComboBox combo, string[] fontNames, string[] prefer)
        {
            var current = combo.Text;
            combo.Items.Clear();
            combo.Items.AddRange(fontNames.Cast<object>().ToArray());
            if (fontNames.Length == 0) return;
            var pick = fontNames.FirstOrDefault(x => x.Equals(current, StringComparison.OrdinalIgnoreCase));
            pick ??= prefer.Select(p => fontNames.FirstOrDefault(x => x.Contains(p, StringComparison.OrdinalIgnoreCase))).FirstOrDefault(x => x is not null);
            combo.SelectedItem = pick ?? fontNames[0];
        }
    }
}
