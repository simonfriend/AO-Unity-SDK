using System.Collections.Generic;
using UnityEngine;

namespace Permaverse.AO
{
	public class IMGUIInteractiveShell : MonoBehaviour
	{
		private InteractiveCMDShell shell;
		private Vector2 scrollPos;
		public string cmd = "";
		public List<string> lineBuffer = new List<string>();
		int startIndex, endIndex;
		GUIStyle textStyleNoWrap = null;

		public void OnGUI()
		{
			if (shell == null)
			{
				if (GUILayout.Button("Create shell process"))
				{
					if (shell == null)
						shell = new InteractiveCMDShell();
				}
				return;
			}
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("stop shell"))
			{
				shell.Stop();
				shell = null;
				return;
			}
			if (GUILayout.Button("clear output"))
			{
				lineBuffer.Clear();
				return;
			}
			GUILayout.EndHorizontal();
			Event e = Event.current;
			if (cmd != "" && e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
			{
				shell.RunCommand(cmd);
				cmd = "";
			}
			if (textStyleNoWrap == null)
			{
				textStyleNoWrap = new GUIStyle("label");
				textStyleNoWrap.wordWrap = false;
				textStyleNoWrap.font = Font.CreateDynamicFontFromOSFont("Courier New", 12);
			}
			shell.GetRecentLines(lineBuffer);
			if (e.type == EventType.Layout)
			{
				startIndex = (int)(scrollPos.y / 20);
				endIndex = Mathf.Min(startIndex + 30, lineBuffer.Count - 1);
			}
			scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(1000));
			GUILayout.Space(startIndex * 20);
			for (int i = startIndex; i <= endIndex; i++)
				GUILayout.Label(lineBuffer[i], textStyleNoWrap, GUILayout.Height(20));
			GUILayout.Space((lineBuffer.Count - endIndex - 1) * 20);
			GUILayout.EndScrollView();
			GUILayout.BeginHorizontal();
			GUILayout.Label(shell.GetCurrentLine(), GUILayout.ExpandWidth(false));
			cmd = GUILayout.TextField(cmd, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal();
		}
	}
}