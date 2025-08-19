using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Permaverse.AO
{
    /// <summary>
    /// Utility class for Node.js detection and execution in Unity Editor
    /// Handles nvm-managed installations and various fallback scenarios
    /// </summary>
    public static class NodeJsUtils
    {
        /// <summary>
        /// Find node executable using shell environment to handle nvm installations
        /// </summary>
        /// <returns>Path to node executable or null if not found</returns>
        public static string FindNodeExecutable()
        {
            try
            {
                // First try using shell with login environment to find node path (for nvm compatibility)
                string shellCommand = Application.platform == RuntimePlatform.WindowsEditor ? "cmd" : "/bin/zsh";
                string nodeCommand = Application.platform == RuntimePlatform.WindowsEditor ? "where node.exe" : "which node";
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = shellCommand,
                    Arguments = Application.platform == RuntimePlatform.WindowsEditor ? $"/c {nodeCommand}" : $"-l -c \"{nodeCommand}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processInfo })
                {
                    process.Start();
                    process.WaitForExit(5000); // 5 second timeout
                    
                    if (process.ExitCode == 0)
                    {
                        string nodePath = process.StandardOutput.ReadToEnd().Trim();
                        // On some systems, which might return multiple lines, take the first one
                        if (!string.IsNullOrEmpty(nodePath))
                        {
                            string firstPath = nodePath.Split('\n')[0].Trim();
                            if (File.Exists(firstPath))
                            {
                                UnityEngine.Debug.Log($"[NodeJsUtils] Found node at: {firstPath}");
                                return firstPath;
                            }
                        }
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        UnityEngine.Debug.LogWarning($"[NodeJsUtils] which node failed: {error}");
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[NodeJsUtils] Failed to find node using shell: {e.Message}");
            }

            // Fallback: try default node command
            try
            {
                string nodeCommand = Application.platform == RuntimePlatform.WindowsEditor ? "node.exe" : "node";
                var testProcess = new ProcessStartInfo
                {
                    FileName = nodeCommand,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = testProcess })
                {
                    process.Start();
                    process.WaitForExit(3000);
                    if (process.ExitCode == 0)
                    {
                        UnityEngine.Debug.Log($"[NodeJsUtils] Using default node command: {nodeCommand}");
                        return nodeCommand;
                    }
                }
            }
            catch
            {
                // Final fallback failed
            }

            // Last resort: try known nvm path directly
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                string nvmNodePath = "/Users/simo/.nvm/versions/node/v22.18.0/bin/node";
                if (File.Exists(nvmNodePath))
                {
                    UnityEngine.Debug.Log($"[NodeJsUtils] Using direct nvm path: {nvmNodePath}");
                    return nvmNodePath;
                }
            }

            return null;
        }

        /// <summary>
        /// Execute a Node.js script with the given arguments (synchronous)
        /// </summary>
        /// <param name="arguments">Command line arguments for node</param>
        /// <returns>Process output if successful</returns>
        public static string ExecuteNodeScript(string[] arguments)
        {
            string nodeCommand = FindNodeExecutable();
            if (string.IsNullOrEmpty(nodeCommand))
            {
                throw new Exception("Node.js not found. Please ensure Node.js is installed and accessible.");
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = nodeCommand,
                Arguments = string.Join(" ", arguments),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();
                
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    string errorDetails = !string.IsNullOrEmpty(error) ? error : "No error details available";
                    string outputDetails = !string.IsNullOrEmpty(output) ? $"\nOutput: {output}" : "";
                    throw new Exception($"Node.js script failed (exit code {process.ExitCode}):\nError: {errorDetails}{outputDetails}");
                }
                
                return output.Trim();
            }
        }

        /// <summary>
        /// Execute a Node.js script with the given arguments (asynchronous)
        /// </summary>
        /// <param name="arguments">Command line arguments for node</param>
        /// <returns>Process output if successful</returns>
        public static async Task<string> ExecuteNodeScriptAsync(string[] arguments)
        {
            string nodeCommand = FindNodeExecutable();
            if (string.IsNullOrEmpty(nodeCommand))
            {
                throw new Exception("Node.js not found. Please ensure Node.js is installed and accessible.");
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = nodeCommand,
                Arguments = string.Join(" ", arguments),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();
                
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                
                await Task.WhenAll(outputTask, errorTask);
                
                string output = await outputTask;
                string error = await errorTask;
                
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    string errorDetails = !string.IsNullOrEmpty(error) ? error : "No error details available";
                    string outputDetails = !string.IsNullOrEmpty(output) ? $"\nOutput: {output}" : "";
                    throw new Exception($"Node.js script failed (exit code {process.ExitCode}):\nError: {errorDetails}{outputDetails}");
                }
                
                return output.Trim();
            }
        }
    }
}
