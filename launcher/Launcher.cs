using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

public sealed class LauncherSettings {
    public string GameDataRoot { get; set; }
    public int ResolutionScale { get; set; }
    public int FrameRate { get; set; }
    public bool Fullscreen { get; set; }
    public bool MotionBlur { get; set; }
    public LauncherSettings() {
        GameDataRoot = ""; ResolutionScale = 1; FrameRate = 60;
        Fullscreen = true; MotionBlur = true;
    }
}

internal static class Program {
    [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
    internal static string RuntimeDirectory;

    [STAThread]
    private static int Main(string[] args) {
        SetProcessDPIAware();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            RuntimeDirectory = File.Exists(Path.Combine(root, "launch-games.ps1")) ? root :
                File.Exists(Path.Combine(root, "runtime", "launch-games.ps1")) ? Path.Combine(root, "runtime") : Path.Combine(root, "source", "playtest");
            if (!File.Exists(Path.Combine(RuntimeDirectory, "launch-games.ps1")))
                throw new IOException("The game launcher files are missing. Keep the launcher with the game installation.");
            Application.Run(new LauncherForm());
            return 0;
        } catch (Exception error) {
            MessageBox.Show(error.Message, "MTR-PGR4", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}

internal sealed class LauncherForm : Form {
    private readonly string runtime = Program.RuntimeDirectory;
    private readonly string settingsFile;
    private readonly TextBox gameFolder = new TextBox();
    private readonly ComboBox resolution = new ComboBox();
    private readonly ComboBox frameRate = new ComboBox();
    private readonly CheckBox fullscreen = new CheckBox();
    private readonly CheckBox blur = new CheckBox();
    private readonly Label status = new Label();
    private readonly Button play = new Button();
    private readonly Button geometry = new Button();
    private readonly Button browse = new Button();
    private readonly Button save = new Button();
    private bool running;
    private bool loading = true;
    private Process gameProcess;

    internal LauncherForm() {
        settingsFile = Path.Combine(runtime, "launcher-settings.json");
        Text = "MTR-PGR4";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Font = new Font("Segoe UI", 10);
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(247, 248, 250);

        TableLayoutPanel layout = new TableLayoutPanel();
        layout.Dock = DockStyle.Fill; layout.Padding = new Padding(22);
        layout.AutoSize = true; layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        layout.MinimumSize = new Size(650, 0);
        layout.ColumnCount = 1; layout.RowCount = 8;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int row = 0; row < 8; row++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);
        Label title = new Label { Text = "MTR-PGR4", AutoSize = true,
            Font = new Font("Segoe UI", 19, FontStyle.Bold), Margin = new Padding(0, 0, 0, 3) };
        layout.Controls.Add(title);
        layout.Controls.Add(new Label { Text = "MissingTheRecompilation: Project Gotham Racing 4", AutoSize = true,
            Margin = new Padding(0, 0, 0, 16) });
        layout.Controls.Add(new Label { Text = "Extracted game folder", AutoSize = true, Margin = new Padding(0, 0, 0, 4) });

        TableLayoutPanel folderRow = new TableLayoutPanel();
        folderRow.AutoSize = true; folderRow.Dock = DockStyle.Top; folderRow.ColumnCount = 2;
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        gameFolder.Dock = DockStyle.Fill; gameFolder.ReadOnly = true;
        gameFolder.Margin = new Padding(0, 3, 10, 3);
        browse.Text = "Browse…"; browse.AutoSize = true; browse.Click += Browse;
        folderRow.Controls.Add(gameFolder, 0, 0); folderRow.Controls.Add(browse, 1, 0);
        layout.Controls.Add(folderRow);
        layout.Controls.Add(new Label { Text = "Choose your own extracted copy, containing default.xex and the Game and UI folders.",
            AutoSize = true, MaximumSize = new Size(590, 0), Margin = new Padding(0, 3, 0, 16) });

        TableLayoutPanel options = new TableLayoutPanel();
        options.AutoSize = true; options.Dock = DockStyle.Top; options.ColumnCount = 4;
        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        resolution.DropDownStyle = ComboBoxStyle.DropDownList;
        resolution.Items.AddRange(new object[] { "720p (original)", "1440p" }); resolution.Dock = DockStyle.Fill;
        frameRate.DropDownStyle = ComboBoxStyle.DropDownList;
        frameRate.Items.AddRange(new object[] { "30 FPS", "60 FPS" }); frameRate.Dock = DockStyle.Fill;
        options.Controls.Add(new Label { Text = "Resolution", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        options.Controls.Add(resolution, 1, 0);
        options.Controls.Add(new Label { Text = "Frame rate", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(14, 0, 3, 0) }, 2, 0);
        options.Controls.Add(frameRate, 3, 0);
        fullscreen.Text = "Fullscreen"; fullscreen.AutoSize = true; fullscreen.Margin = new Padding(3, 12, 3, 12);
        blur.Text = "Motion blur"; blur.AutoSize = true; blur.Margin = new Padding(3, 12, 3, 12);
        options.Controls.Add(fullscreen, 1, 1); options.Controls.Add(blur, 3, 1);
        layout.Controls.Add(options);

        FlowLayoutPanel buttons = new FlowLayoutPanel();
        buttons.AutoSize = true; buttons.Dock = DockStyle.Top; buttons.Margin = new Padding(0, 4, 0, 10);
        play.Text = "Play PGR4"; play.AutoSize = true; play.Padding = new Padding(12, 5, 12, 5);
        geometry.Text = "Geometry Wars"; geometry.AutoSize = true; geometry.Padding = new Padding(8, 5, 8, 5);
        save.Text = "Save settings"; save.AutoSize = true; save.Padding = new Padding(8, 5, 8, 5);
        play.Click += async delegate { await Launch(false); };
        geometry.Click += async delegate { await Launch(true); };
        save.Click += delegate { SaveSettings(); };
        buttons.Controls.Add(play); buttons.Controls.Add(geometry); buttons.Controls.Add(save);
        layout.Controls.Add(buttons);
        status.AutoSize = true; status.MaximumSize = new Size(590, 0); status.ForeColor = Color.FromArgb(65, 75, 90);
        layout.Controls.Add(status);

        LauncherSettings settings = new LauncherSettings();
        string readError = "";
        if (File.Exists(settingsFile)) {
            try { settings = new JavaScriptSerializer().Deserialize<LauncherSettings>(File.ReadAllText(settingsFile)) ?? settings; }
            catch { readError = "Saved settings could not be read. Please choose your folder again."; }
        } else {
            // Detect this development installation without embedding a user's path.
            string local = Path.GetFullPath(Path.Combine(runtime, "..", "..", "PGR4"));
            if (ValidFolder(local)) settings.GameDataRoot = local;
        }
        gameFolder.Text = settings.GameDataRoot ?? "";
        resolution.SelectedIndex = settings.ResolutionScale == 2 ? 1 : 0;
        frameRate.SelectedIndex = settings.FrameRate == 30 ? 0 : 1;
        fullscreen.Checked = settings.Fullscreen; blur.Checked = settings.MotionBlur;
        resolution.SelectedIndexChanged += Changed; frameRate.SelectedIndexChanged += Changed;
        fullscreen.CheckedChanged += Changed; blur.CheckedChanged += Changed;
        loading = false;
        UpdateButtons();
        if (readError.Length > 0) status.Text = readError;
        FormClosing += delegate(object sender, FormClosingEventArgs e) {
            if (running) { e.Cancel = true; status.Text = "Close the game first, then close this launcher."; }
        };
    }

    private static bool ValidFolder(string path) {
        return !string.IsNullOrWhiteSpace(path) && File.Exists(Path.Combine(path, "default.xex")) &&
            Directory.Exists(Path.Combine(path, "Game")) && Directory.Exists(Path.Combine(path, "UI"));
    }
    private void Changed(object sender, EventArgs e) {
        if (!loading) status.Text = "Settings will be saved when you play. You can also save them now.";
    }
    private void Browse(object sender, EventArgs e) {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog()) {
            dialog.Description = "Choose the extracted PGR4 folder containing default.xex, Game and UI.";
            dialog.ShowNewFolderButton = false;
            if (Directory.Exists(gameFolder.Text)) dialog.SelectedPath = gameFolder.Text;
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            gameFolder.Text = dialog.SelectedPath;
            UpdateButtons();
        }
    }
    private void UpdateButtons() {
        bool valid = ValidFolder(gameFolder.Text);
        play.Enabled = valid && !running;
        geometry.Enabled = valid && !running && File.Exists(Path.Combine(gameFolder.Text, "gw.xex")) &&
            Directory.Exists(Path.Combine(gameFolder.Text, "gw")) && File.Exists(Path.Combine(runtime, "geometry-wars", "gw_recompiled.exe"));
        browse.Enabled = !running; save.Enabled = valid && !running;
        resolution.Enabled = !running; frameRate.Enabled = !running; fullscreen.Enabled = !running; blur.Enabled = !running;
        status.Text = valid ? "Ready. Geometry Wars runs at 60 FPS and can return to PGR4." :
            "Choose a valid extracted PGR4 folder to enable Play. ISO files cannot be used directly.";
    }
    private bool SaveSettings() {
        try {
            LauncherSettings settings = new LauncherSettings { GameDataRoot = gameFolder.Text,
                ResolutionScale = resolution.SelectedIndex == 1 ? 2 : 1, FrameRate = frameRate.SelectedIndex == 1 ? 60 : 30,
                Fullscreen = fullscreen.Checked, MotionBlur = blur.Checked };
            string temporary = settingsFile + ".tmp";
            File.WriteAllText(temporary, new JavaScriptSerializer().Serialize(settings), new UTF8Encoding(false));
            if (File.Exists(settingsFile)) File.Replace(temporary, settingsFile, null); else File.Move(temporary, settingsFile);
            status.Text = "Settings saved.";
            return true;
        } catch (Exception error) { MessageBox.Show(this, "Could not save settings: " + error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
    }
    private static string Quote(string value) {
        // CRT command-line quoting, including quotes and trailing backslashes.
        StringBuilder result = new StringBuilder("\"");
        int slashes = 0;
        foreach (char c in value) {
            if (c == '\\') { slashes++; continue; }
            if (c == '"') result.Append('\\', slashes * 2 + 1);
            else result.Append('\\', slashes);
            result.Append(c); slashes = 0;
        }
        result.Append('\\', slashes * 2); result.Append('"'); return result.ToString();
    }
    private async Task Launch(bool startGeometry) {
        if (!ValidFolder(gameFolder.Text) || !SaveSettings()) return;
        string[] gameArgs = new string[0];
        if (frameRate.SelectedIndex == 1) gameArgs = new[] {
            "--pgr4_60fps", "--pgr4_garage_walk_fix", "--d3d12_texture_shared_heaps", "--gpu_vblank_deadline_timer",
            "--guest_frame_stats", "--gpu_wait_reg_mem_high_res", "--no-clear_memory_page_state", "--gpu_ring_wake_fast",
            "--gpu_command_stats", "--texture_resource_reuse", "--texture_reuse_pool_limit_mib=256", "--texture_cache_memory_limit_soft_lifetime=30" };
        if (resolution.SelectedIndex == 1) gameArgs = gameArgs.Concat(new[] { "--resolution_scale=2" }).ToArray();
        if (fullscreen.Checked) gameArgs = gameArgs.Concat(new[] { "--fullscreen" }).ToArray();
        if (!blur.Checked) gameArgs = gameArgs.Concat(new[] { "--pgr4_disable_motion_blur" }).ToArray();
        if (startGeometry) gameArgs = gameArgs.Concat(new[] { "--start-geometry-wars" }).ToArray();
        try {
            running = true; UpdateButtons(); status.Text = "Game running. Your selected settings also apply when returning from Geometry Wars.";
            string shell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            ProcessStartInfo info = new ProcessStartInfo(shell,
                "-NoProfile -ExecutionPolicy Bypass -File " + Quote(Path.Combine(runtime, "launch-games.ps1")) + " -- " + string.Join(" ", gameArgs.Select(Quote)));
            info.UseShellExecute = false; info.CreateNoWindow = true; info.WorkingDirectory = runtime;
            info.EnvironmentVariables["PGR4_GAME_ROOT"] = gameFolder.Text;
            info.RedirectStandardOutput = true; info.RedirectStandardError = true;
            gameProcess = Process.Start(info);
            Task<string> output = gameProcess.StandardOutput.ReadToEndAsync();
            Task<string> errorOutput = gameProcess.StandardError.ReadToEndAsync();
            await Task.Run(delegate { gameProcess.WaitForExit(); });
            string transcript = await output + "\r\n" + await errorOutput;
            Directory.CreateDirectory(Path.Combine(runtime, "logs"));
            File.WriteAllText(Path.Combine(runtime, "logs", "launcher.log"), transcript);
            int exitCode = gameProcess.ExitCode;
            gameProcess.Dispose(); gameProcess = null;
            running = false; UpdateButtons();
            if (exitCode != 0) MessageBox.Show(this, "The game could not finish normally (exit code " + exitCode + ").\r\n\r\n" + transcript.Trim() +
                "\r\n\r\nDetails are saved in logs\\launcher.log.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        } catch (Exception error) {
            running = false; UpdateButtons();
            MessageBox.Show(this, error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
