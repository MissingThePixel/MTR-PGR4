using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Requests identify one of the two bundled games, never an executable path
// or shell command. The game code and its existing launch-data format stay intact.
internal sealed class GameSession {
    private readonly string runtime, gameRoot;
    private readonly string[] gameArguments;
    private readonly bool startGeometry;
    private readonly Queue<string> log = new Queue<string>();
    private readonly object logLock = new object();

    internal GameSession(string directory, string gameDirectory, string[] arguments, bool geometry) {
        runtime = Path.GetFullPath(directory); gameRoot = Path.GetFullPath(gameDirectory);
        gameArguments = (string[])arguments.Clone(); startGeometry = geometry;
    }

    internal static string QuoteArgument(string value) {
        StringBuilder result = new StringBuilder("\"");
        int slashes = 0;
        foreach (char c in value) {
            if (c == '\\') { slashes++; continue; }
            if (c == '"') result.Append('\\', slashes * 2 + 1);
            else result.Append('\\', slashes);
            result.Append(c); slashes = 0;
        }
        result.Append('\\', slashes * 2); result.Append('"');
        return result.ToString();
    }

    internal static uint ValidateHandoff(byte[] bytes, uint currentTitle) {
        if (bytes.Length < 20 || bytes.Length > 3092 || BitConverter.ToUInt32(bytes, 0) != 0x48475250)
            throw new InvalidDataException("Invalid title handoff header.");
        uint next = BitConverter.ToUInt32(bytes, 4);
        uint present = BitConverter.ToUInt32(bytes, 12);
        uint length = BitConverter.ToUInt32(bytes, 16);
        if ((next != 1 && next != 2) || next == currentTitle || present > 1 || length > 3072 || bytes.Length != 20 + length)
            throw new InvalidDataException("Invalid title handoff data.");
        return next;
    }

    private void Record(string line) {
        if (line == null) return;
        lock (logLock) {
            if (line.Length > 4096) line = line.Substring(0, 4096);
            log.Enqueue(line);
            while (log.Count > 200) log.Dequeue(); // Bounded troubleshooting tail.
        }
    }

    internal async Task<int> RunAsync(Action<string> progress) {
        string session = Path.Combine(runtime, "logs", "geometry-wars", Guid.NewGuid().ToString("N"));
        string outgoing = Path.Combine(session, "request.bin"), incoming = Path.Combine(session, "incoming.bin");
        uint title = startGeometry ? 2u : 1u;
        bool hasInput = false;
        int sequence = 0;
        Directory.CreateDirectory(session);
        try {
            while (true) {
                sequence++;
                string workingDirectory = title == 1 ? runtime : Path.Combine(runtime, "geometry-wars");
                string executable = Path.Combine(workingDirectory, title == 1 ? "pgr4_recompiled.exe" : "gw_recompiled.exe");
                if (!File.Exists(executable)) throw new FileNotFoundException("Game executable missing.", executable);
                List<string> arguments = new List<string> {
                    "--game_data_root", gameRoot, "--user_data_root", Path.Combine(workingDirectory, "userdata"),
                    "--execute_unclipped_draw_vs_on_cpu", "--gpu_plugin=xenos", "--d3d12_present_vsync"
                };
                if (title == 1) {
                    arguments.AddRange(new[] { "--xmp_music_gain", "0.35" });
                    arguments.AddRange(gameArguments);
                } else {
                    arguments.AddRange(new[] { "--log_file", Path.Combine(session, "geometry-wars-" + sequence + ".log") });
                    // GW already runs at 60 FPS. Carry only shared display options.
                    arguments.AddRange(gameArguments.Where(a => a == "--fullscreen" || a == "--resolution_scale=2"));
                }
                ProcessStartInfo info = new ProcessStartInfo(executable, string.Join(" ", arguments.Select(QuoteArgument))) {
                    UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                info.EnvironmentVariables["PGR4_GAME_ROOT"] = gameRoot;
                info.EnvironmentVariables["PGR4_HANDOFF_OUTPUT"] = outgoing;
                info.EnvironmentVariables.Remove("PGR4_HANDOFF_INPUT");
                if (hasInput) info.EnvironmentVariables["PGR4_HANDOFF_INPUT"] = incoming;
                int exitCode;
                using (Process process = new Process { StartInfo = info }) {
                    process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) { Record(e.Data); };
                    process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) { Record(e.Data); };
                    Record(title == 1 ? "Launching PGR4." : "Launching Geometry Wars.");
                    if (!process.Start()) throw new IOException("The game could not be started.");
                    process.BeginOutputReadLine(); process.BeginErrorReadLine();
                    await Task.Run(delegate { process.WaitForExit(); });
                    exitCode = process.ExitCode;
                }
                Record("Game exit code: " + exitCode);
                if (exitCode != 0) return exitCode;
                if (!File.Exists(outgoing)) return 0; // A normal close ends the session.
                long size = new FileInfo(outgoing).Length;
                if (size < 20 || size > 3092) throw new InvalidDataException("Invalid title handoff size.");
                title = ValidateHandoff(File.ReadAllBytes(outgoing), title);
                File.Delete(incoming); File.Move(outgoing, incoming);
                hasInput = true;
                progress(title == 2 ? "Launching Geometry Wars…" : "Returning to PGR4…");
            }
        } catch (Exception error) { Record(error.Message); throw; }
        finally {
            // Delete only this session's messages, never user saves.
            foreach (string file in new[] { incoming, outgoing }) {
                try { File.Delete(file); } catch (IOException) {} catch (UnauthorizedAccessException) {}
            }
            try {
                if (!Directory.EnumerateFileSystemEntries(session).Any()) Directory.Delete(session);
                lock (logLock) File.WriteAllLines(Path.Combine(runtime, "logs", "launcher.log"), log.ToArray());
            } catch (IOException) {} catch (UnauthorizedAccessException) {}
        }
    }
}