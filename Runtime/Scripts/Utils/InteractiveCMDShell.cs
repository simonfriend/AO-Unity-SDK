using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Permaverse.AO
{
    public class InteractiveCMDShell
    {
        private System.Diagnostics.ProcessStartInfo startInfo;
        private System.Diagnostics.Process process;
        private System.Threading.Thread thread;
        private System.IO.StreamReader output;
        // private bool stopThread = false;

        private string lineBuffer = "";
        private List<string> lines = new List<string>();
        private bool m_Running = false;

        public InteractiveCMDShell()
        {
            string shell;

            // Determine the shell based on the operating system
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shell = "cmd.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                shell = "/bin/zsh";  // macOS default shell
            }
            else
            {
                shell = "/bin/bash"; // Assume bash for Linux
            }

            Debug.Log("Opening shell: " + shell);
            startInfo = new System.Diagnostics.ProcessStartInfo(shell)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true, // Capture any errors
                CreateNoWindow = true,
                WorkingDirectory = Application.dataPath + "/StreamingAssets" // Set appropriate working directory
            };

            // Set environment variables to ensure commands like 'aos' are recognized
            startInfo.EnvironmentVariables["PATH"] += ":/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin";

            try
            {
                process = new System.Diagnostics.Process
                {
                    StartInfo = startInfo
                };

                if (!process.Start())
                {
                    Debug.LogError("Failed to start the shell process.");
                }

                output = process.StandardOutput;
                Debug.Log("Shell started successfully.");

                thread = new System.Threading.Thread(Thread);
                thread.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to start shell: " + ex.Message);
            }
        }

        ~InteractiveCMDShell()
        {
            Stop(); // Ensure the shell is stopped when this class is destroyed
        }

        public void RunCommand(string command)
        {
            if (m_Running)
            {
                process.StandardInput.WriteLine(command);
                process.StandardInput.Flush();
            }
        }

        public void Stop()
        {
            // stopThread = true;  // Signal the thread to stop
            if (process != null)
            {
                process.Kill();
                if (thread.IsAlive)
                {
                    thread.Join(200);  // Wait for the thread to terminate gracefully
                }
                process.Dispose();
                process = null;
                thread = null;
                m_Running = false;
            }
        }

        public string GetCurrentLine()
        {
            if (!m_Running)
                return "";
            return lineBuffer;
        }

        public void GetRecentLines(List<string> aLines)
        {
            if (!m_Running || aLines == null)
                return;
            if (lines.Count == 0)
                return;
            lock (lines)
            {
                if (lines.Count > 0)
                {
                    aLines.AddRange(lines);
                    lines.Clear();
                }
            }
        }

        private void Thread()
        {
            m_Running = true;
            try
            {
                while (true)
                {
                    int c = output.Read(); // Read character by character
                    if (c <= 0)
                        break;
                    else if (c == '\n')
                    {
                        lock (lines)
                        {
                            lines.Add(lineBuffer); // Add complete line to the output list
                            lineBuffer = ""; // Clear the line buffer
                        }
                    }
                    else if (c != '\r') // Ignore carriage return
                    {
                        lineBuffer += (char)c; // Append character to the buffer
                    }
                }
                Debug.Log("CMDProcess Thread finished");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            m_Running = false;
        }
    }
}