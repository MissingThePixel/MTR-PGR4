using System;
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
            RuntimeDirectory = File.Exists(Path.Combine(root, "pgr4_recompiled.exe")) ? root :
                Path.Combine(root, "runtime");
            if (!File.Exists(Path.Combine(RuntimeDirectory, "pgr4_recompiled.exe")))
                throw new IOException("The game launcher files are missing. Keep the launcher with the game installation.");
            Application.Run(new LauncherForm());
            return 0;
        } catch (Exception error) {
            MessageBox.Show(error.Message, "MTR-PGR4", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}

// Paint flat buttons explicitly so disabled captions remain readable on pale
// backgrounds. Button still supplies keyboard activation and accessibility.
internal sealed class LauncherButton : Button {
    private bool hovered, pressed;
    protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hovered = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { pressed = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) { pressed = true; Invalidate(); } base.OnKeyDown(e); }
    protected override void OnKeyUp(KeyEventArgs e) { pressed = false; Invalidate(); base.OnKeyUp(e); }
    protected override void OnPaint(PaintEventArgs e) {
        Color fill = !Enabled ? Color.FromArgb(241, 242, 245) : pressed ? FlatAppearance.MouseDownBackColor :
            hovered ? FlatAppearance.MouseOverBackColor : BackColor;
        e.Graphics.Clear(fill);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle,
            Enabled ? ForeColor : Color.FromArgb(145, 149, 157),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues) {
            Rectangle focus = ClientRectangle; focus.Inflate(-4, -4);
            ControlPaint.DrawFocusRectangle(e.Graphics, focus, ForeColor, fill);
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
    private readonly Button play = new LauncherButton();
    private readonly Button geometry = new LauncherButton();
    private readonly Button browse = new LauncherButton();
    private readonly Button save = new LauncherButton();
    private bool running;
    private bool loading = true;

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
        BackColor = Color.White;
        ForeColor = Color.FromArgb(27, 29, 34);

        TableLayoutPanel shell = new TableLayoutPanel {
            Dock = DockStyle.Fill, Padding = new Padding(28), AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(shell);
        PictureBox artwork = new PictureBox {
            Size = new Size(300, 300), SizeMode = PictureBoxSizeMode.Zoom,
            Anchor = AnchorStyles.None, Margin = new Padding(0, 0, 20, 0)
        };
        using (Stream stream = typeof(LauncherForm).Assembly.GetManifestResourceStream("LauncherArtwork")) {
            if (stream != null) using (Image image = Image.FromStream(stream)) artwork.Image = new Bitmap(image);
        }
        FormClosed += delegate { if (artwork.Image != null) artwork.Image.Dispose(); };
        shell.Controls.Add(artwork, 0, 0);

        TableLayoutPanel layout = new TableLayoutPanel {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(520, 0), ColumnCount = 1, RowCount = 9,
            Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int row = 0; row < 9; row++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.Controls.Add(layout, 1, 0);
        layout.Controls.Add(new Label {
            Text = "Project Gotham Racing 4", AutoSize = true,
            Font = new Font("Segoe UI", 21, FontStyle.Bold), Margin = new Padding(0, 0, 0, 26)
        });
        layout.Controls.Add(new Label { Text = "Game folder", AutoSize = true, Margin = new Padding(0, 0, 0, 8) });

        TableLayoutPanel folderRow = new TableLayoutPanel {
            AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, Margin = new Padding(0)
        };
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        gameFolder.Dock = DockStyle.Fill; gameFolder.ReadOnly = true;
        gameFolder.BackColor = Color.White; gameFolder.ForeColor = ForeColor;
        gameFolder.BorderStyle = BorderStyle.FixedSingle;
        gameFolder.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        gameFolder.Margin = new Padding(0, 8, 12, 8);
        browse.Text = "Browse"; StyleButton(browse, false); browse.MinimumSize = new Size(104, 38);
        browse.Click += Browse;
        folderRow.Controls.Add(gameFolder, 0, 0); folderRow.Controls.Add(browse, 1, 0);
        layout.Controls.Add(folderRow);
        layout.Controls.Add(new Label {
            Text = "Select your extracted PGR4 folder.", AutoSize = true,
            ForeColor = Color.FromArgb(95, 101, 113), Margin = new Padding(0, 6, 0, 20)
        });

        TableLayoutPanel options = new TableLayoutPanel {
            AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, RowCount = 3, Margin = new Padding(0)
        };
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int row = 0; row < 3; row++) options.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resolution.DropDownStyle = ComboBoxStyle.DropDownList; resolution.FlatStyle = FlatStyle.Flat;
        resolution.Items.AddRange(new object[] { "720p (1280 × 720)", "1440p (2560 × 1440)" });
        resolution.Dock = DockStyle.Fill; resolution.Margin = new Padding(0, 0, 12, 0);
        frameRate.DropDownStyle = ComboBoxStyle.DropDownList; frameRate.FlatStyle = FlatStyle.Flat;
        frameRate.Items.AddRange(new object[] { "30 FPS", "60 FPS" });
        frameRate.Dock = DockStyle.Fill; frameRate.Margin = new Padding(0);
        options.Controls.Add(new Label { Text = "Resolution", AutoSize = true, Margin = new Padding(0, 0, 0, 8) }, 0, 0);
        options.Controls.Add(new Label { Text = "Frame rate", AutoSize = true, Margin = new Padding(0, 0, 0, 8) }, 1, 0);
        options.Controls.Add(resolution, 0, 1); options.Controls.Add(frameRate, 1, 1);
        fullscreen.Text = "Fullscreen"; fullscreen.AutoSize = true; fullscreen.Margin = new Padding(0, 14, 0, 20);
        blur.Text = "Motion blur"; blur.AutoSize = true; blur.Margin = new Padding(0, 14, 0, 20);
        options.Controls.Add(fullscreen, 0, 2); options.Controls.Add(blur, 1, 2);
        layout.Controls.Add(options);

        play.Text = "Play"; StyleButton(play, true); play.Dock = DockStyle.Top;
        play.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        play.MinimumSize = new Size(0, 48); play.Margin = new Padding(0, 2, 0, 10);
        play.Click += async delegate { await Launch(false); };
        layout.Controls.Add(play);
        TableLayoutPanel secondary = new TableLayoutPanel {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 18)
        };
        secondary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        secondary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        secondary.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        geometry.Text = "Geometry Wars"; StyleButton(geometry, false); geometry.Dock = DockStyle.Top;
        geometry.Margin = new Padding(0, 0, 12, 0);
        save.Text = "Save settings"; StyleButton(save, false); save.Dock = DockStyle.Top; save.Margin = new Padding(0);
        geometry.Click += async delegate { await Launch(true); };
        save.Click += delegate { SaveSettings(); };
        secondary.Controls.Add(geometry, 0, 0); secondary.Controls.Add(save, 1, 0);
        layout.Controls.Add(secondary);
        status.AutoSize = true; status.MaximumSize = new Size(510, 0);
        status.ForeColor = Color.FromArgb(95, 101, 113); status.Margin = new Padding(0);
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

    private static void StyleButton(Button button, bool primary) {
        button.AutoSize = true; button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = primary ? Color.FromArgb(196, 38, 50) : Color.FromArgb(235, 237, 241);
        button.ForeColor = primary ? Color.White : Color.FromArgb(27, 29, 34);
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(216, 49, 62) : Color.FromArgb(222, 225, 231);
        button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(163, 28, 40) : Color.FromArgb(209, 213, 221);
        button.Padding = new Padding(12, 7, 12, 7); button.Margin = new Padding(0);
        button.Cursor = Cursors.Hand;
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
    private async Task Launch(bool startGeometry) {
        if (!ValidFolder(gameFolder.Text) || !SaveSettings()) return;
        string[] gameArgs = new string[0];
        if (frameRate.SelectedIndex == 1) gameArgs = new[] {
            "--pgr4_60fps", "--pgr4_garage_walk_fix", "--d3d12_texture_shared_heaps", "--gpu_vblank_deadline_timer",
            "--gpu_wait_reg_mem_high_res", "--no-clear_memory_page_state", "--gpu_ring_wake_fast",
            "--texture_resource_reuse", "--texture_reuse_pool_limit_mib=256", "--texture_cache_memory_limit_soft_lifetime=30" };
        if (resolution.SelectedIndex == 1) gameArgs = gameArgs.Concat(new[] { "--resolution_scale=2" }).ToArray();
        if (fullscreen.Checked) gameArgs = gameArgs.Concat(new[] { "--fullscreen" }).ToArray();
        if (!blur.Checked) gameArgs = gameArgs.Concat(new[] { "--pgr4_disable_motion_blur" }).ToArray();
        try {
            running = true; UpdateButtons(); status.Text = "Game running. Your selected settings also apply when returning from Geometry Wars.";
            GameSession session = new GameSession(runtime, gameFolder.Text, gameArgs, startGeometry);
            int exitCode = await session.RunAsync(delegate(string message) { status.Text = message; });
            running = false; UpdateButtons();
            if (exitCode != 0) MessageBox.Show(this,
                "The game could not finish normally (exit code " + exitCode + ").\r\n\r\nDetails are saved in logs\\launcher.log.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        } catch (Exception error) {
            running = false; UpdateButtons();
            MessageBox.Show(this, error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
