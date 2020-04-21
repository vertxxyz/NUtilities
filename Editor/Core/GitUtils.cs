using System.Text;
using UnityEngine;

namespace Vertx.Editor.Extensions
{
	/* Copyright 2019 mob-sakai																					*
	 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and		*
	 * associated documentation files (the "Software"), to deal in the Software without restriction,			*
	 * including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,	*
	 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,	*
	 * subject to the following conditions: The above copyright notice and this permission notice shall be		*
	 * included in all copies or substantial portions of the Software.											*
	 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT		*
	 * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.	*
	 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,	*
	 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH			*
	 * THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.												*/

	public static class GitUtils
	{
		static readonly StringBuilder s_sbError = new StringBuilder();
		static readonly StringBuilder s_sbOutput = new StringBuilder();

		public static bool IsGitRunning { get; private set; }

		public delegate void GitCommandCallback(bool success, string output);

		public static WaitWhile ExecuteGitCommand(string args, GitCommandCallback callback, bool waitForExit = false)
		{
			var startInfo = new System.Diagnostics.ProcessStartInfo
			{
				Arguments = args,
				CreateNoWindow = true,
				FileName = "git",
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
			};

			var launchProcess = System.Diagnostics.Process.Start(startInfo);
			if (launchProcess == null || launchProcess.HasExited || launchProcess.Id == 0)
			{
				Debug.LogError("No 'git' executable was found. Please install Git on your system and restart Unity");
				callback(false, string.Empty);
			}
			else
			{
				//Add process callback.
				IsGitRunning = true;
				s_sbError.Length = 0;
				s_sbOutput.Length = 0;
				launchProcess.OutputDataReceived += (sender, e) => s_sbOutput.AppendLine(e.Data ?? string.Empty);
				launchProcess.ErrorDataReceived += (sender, e) => s_sbError.AppendLine(e.Data ?? string.Empty);
				launchProcess.Exited += (sender, e) =>
				{
					IsGitRunning = false;
					bool success = 0 == launchProcess.ExitCode;
					if (!success)
						Debug.LogError($"Error: git {args}\n\n{s_sbError}");

					callback(success, s_sbOutput.ToString());
				};

				launchProcess.BeginOutputReadLine();
				launchProcess.BeginErrorReadLine();
				launchProcess.EnableRaisingEvents = true;

				if (waitForExit)
					launchProcess.WaitForExit();
			}

			return new WaitWhile(() => IsGitRunning);
		}
	}
}