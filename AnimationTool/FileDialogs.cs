using System;
using System.Diagnostics;

namespace AnimationTool
{
    /// <summary>
    /// Native macOS Open/Save dialogs, shown via osascript's "choose file" /
    /// "choose file name" commands (backed by NSOpenPanel/NSSavePanel).
    /// Mac-only: this tool currently only runs on macOS.
    /// </summary>
    public static class FileDialogs
    {
        /// <summary>
        /// Shows a native Open dialog filtered to the given extension (no dot, e.g. "xml").
        /// Returns the chosen POSIX path, or null if the user cancelled.
        /// </summary>
        public static string OpenFile(string extension, string prompt)
        {
            var script = $"POSIX path of (choose file with prompt \"{EscapeForAppleScript(prompt)}\" of type {{\"{EscapeForAppleScript(extension)}\"}})";
            return RunOsaScript(script);
        }

        /// <summary>
        /// Shows a native Save dialog with the given default filename. The returned
        /// path always ends in "." + extension, even if the user typed a different one.
        /// Returns null if the user cancelled.
        /// </summary>
        public static string SaveFile(string extension, string defaultFileName, string prompt)
        {
            var script = $"POSIX path of (choose file name with prompt \"{EscapeForAppleScript(prompt)}\" default name \"{EscapeForAppleScript(defaultFileName)}\")";
            var path = RunOsaScript(script);
            if (null == path)
            {
                return null;
            }

            var suffix = "." + extension;
            if (!path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                path += suffix;
            }

            return path;
        }

        private static string RunOsaScript(string script)
        {
            var startInfo = new ProcessStartInfo("osascript")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add(script);

            using var process = Process.Start(startInfo);
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                // Non-zero exit means the user cancelled (AppleScript error -128)
                // or something else went wrong; either way there's no path to use.
                return null;
            }

            return output.Trim();
        }

        private static string EscapeForAppleScript(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
