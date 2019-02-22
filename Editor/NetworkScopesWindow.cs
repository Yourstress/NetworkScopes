
using System;
using System.IO;
using System.Linq;

namespace NetworkScopes.Editor
{
	using UnityEngine;
	using UnityEditor;
	using System.Collections.Generic;

	public class NetworkScopesWindow : EditorWindow
	{
		private Vector2 scrollPos;

		private List<NetworkScopeProcessor> networkScopeTypes;
		private NetworkScopeProcessor _selectedProcessor;

		private List<string> currentFileLines = new List<string>();
		private List<string> generatedFileLines = new List<string>();

		private List<int> matchingCurrentFileLines = new List<int>();
		private List<int> matchingGeneratedFileLines = new List<int>();

		private GUIStyle codeStyle = null;

		[MenuItem("Network Scopes/Network Scopes Window")]
		static void ShowWindow()
		{
			GetWindow<NetworkScopesWindow>("Network Scopes");
		}

		private const float sidebarWidth = 200;
		private const float lineColumnWidth = 60;

		void OnGUI()
		{
			if (codeStyle == null)
			{
				codeStyle = EditorStyles.whiteLabel;
				codeStyle.normal.textColor = new Color(.8f,.8f,.8f);
				codeStyle.richText = true;
				string fontAssetGuid = AssetDatabase.FindAssets("t:Font CONSOLA")[0];
				codeStyle.font = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(fontAssetGuid));
				codeStyle.fontSize = 12;
			}

			HandleInput();

			GUILayout.BeginHorizontal();

			GUILayout.BeginHorizontal("PreBackground", GUILayout.Width(sidebarWidth), GUILayout.ExpandHeight(true));
			GUILayout.BeginVertical();
			DrawSidebar();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
			GUILayout.BeginVertical();
			DrawContent();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			GUILayout.EndHorizontal();
		}

		void HandleInput()
		{
			if (Event.current.type == EventType.KeyDown)
			{
				if (Event.current.keyCode == KeyCode.DownArrow)
				{
					SelectRelative(1);
					Event.current.Use();
				}
				else if (Event.current.keyCode == KeyCode.UpArrow)
				{
					SelectRelative(-1);
					Event.current.Use();
				}
			}
		}

		void SelectRelative(int indexOffset)
		{
			if (networkScopeTypes.Count == 0)
				return;

			int selIndex = networkScopeTypes.IndexOf(_selectedProcessor);

			selIndex = (selIndex + indexOffset + networkScopeTypes.Count) % networkScopeTypes.Count;

			SelectType(networkScopeTypes[selIndex]);
		}

		void OnEnable()
		{
			codeStyle = null;

			networkScopeTypes = NetworkScopeUtility.GetNetworkScopeTypes();

			string lastSelectedTypeName = EditorPrefs.GetString("NetworkScopesWindow.LastTypeName");
			NetworkScopeProcessor selectedProcessor = networkScopeTypes.FirstOrDefault(t => t.Name == lastSelectedTypeName);

			if (selectedProcessor != null)
				SelectType(selectedProcessor);
		}

		void OnDisable()
		{
			if (_selectedProcessor != null)
				EditorPrefs.SetString("NetworkScopesWindow.LastTypeName", _selectedProcessor.Name);
		}

		void SelectType(NetworkScopeProcessor newSelection)
		{
			_selectedProcessor = newSelection;

			currentFileLines.Clear();
			generatedFileLines.Clear();

			// load current file
			string currentFilePath = NetworkScopePostProcessor.GetGeneratedClassPath(_selectedProcessor.type.Namespace, _selectedProcessor.type.Name, true, false);
			if (File.Exists(currentFilePath))
			{
				currentFileLines.AddRange(_selectedProcessor.preGeneratedContents.ConvertTabs().Split('\n'));
			}
			else
				Debug.Log("File doesn't exist at " + currentFilePath);

			generatedFileLines.AddRange(_selectedProcessor.postGeneratedContents.ConvertTabs().Split('\n'));

			List<string> lowCountList = (generatedFileLines.Count < currentFileLines.Count) ? generatedFileLines : currentFileLines;
			List<string> highCountList = (generatedFileLines.Count > currentFileLines.Count) ? generatedFileLines : currentFileLines;
			if (lowCountList.Count != highCountList.Count)
			{
				int num = highCountList.Count - lowCountList.Count;
				for (int x = 0; x < num; x++)
					lowCountList.Add("");
			}

			// figure out matching strings
			UpdateMatchingLines();
		}

		void GetMatchingLines(List<int> matchingLineIndexes, List<string> lines, List<string> otherLines)
		{
			matchingLineIndexes.Clear();

			// from start
			for (int x = 0; x < lines.Count; x++)
			{
				if (lines[x] == otherLines[x])
					matchingLineIndexes.Add(x);
				else
					break;
			}

			// from finish
			int mainListIndex = lines.Count - 1;
			int otherListIndex = otherLines.Count - 1;

			// skip whitespace
			while (mainListIndex >= 0 && lines[mainListIndex].Length == 0)
				mainListIndex--;
			while (otherListIndex >= 0 && otherLines[otherListIndex].Length == 0)
				otherListIndex--;

			while (otherListIndex >= 0 && mainListIndex >= 0)
			{
				if (lines[mainListIndex] == otherLines[otherListIndex])
				{
					matchingLineIndexes.Add(mainListIndex);
				}
				else
					break;

				mainListIndex--;
				otherListIndex--;
			}
		}

		void UpdateMatchingLines()
		{
			GetMatchingLines(matchingCurrentFileLines, currentFileLines, generatedFileLines);
			GetMatchingLines(matchingGeneratedFileLines, generatedFileLines, currentFileLines);
		}

		void DrawSidebar()
		{
			GUILayout.Label("Scopes", EditorStyles.boldLabel);
			GUILayout.Space(12);

			if (networkScopeTypes == null)
				return;

			for (var x = 0; x < networkScopeTypes.Count; x++)
			{
				NetworkScopeProcessor netScopeProcessor = networkScopeTypes[x];

				bool isSelected = (_selectedProcessor == netScopeProcessor);

				Rect itemRect = GUILayoutUtility.GetRect(sidebarWidth, 24);

				if (isSelected)
				{
					GUI.color = new Color(.1f, .1f, .1f);
					GUI.DrawTexture(itemRect, EditorGUIUtility.whiteTexture);
				}

				itemRect.y += 3;
				itemRect.xMin += 18;

				GUI.color = Color.white;
				if (GUI.Button(itemRect, netScopeProcessor.Name, EditorStyles.label))
					SelectType(netScopeProcessor);

				itemRect = new Rect(6, itemRect.y, 13, 13);
				GUI.Label(itemRect, GUIContent.none, netScopeProcessor.areFilesMatching ? "WinBtnMaxMac" : "WinBtnCloseMac");
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Generate All"))
			{
				int numSaved = 0;
				foreach (NetworkScopeProcessor scopeType in networkScopeTypes)
				{
					if (scopeType.WriteToFile())
						numSaved++;
				}

				if (numSaved > 0)
				{
					AssetDatabase.Refresh();

					Debug.Log($"NetworkScopes generated {numSaved} classes.");
				}
			}

			GUILayout.Space(12);
		}

		void DrawContent()
		{
			float maxWidth = position.width - sidebarWidth - lineColumnWidth;
			float columnWidth = maxWidth * 0.5f;

			DrawToolbar();

			GUILayout.BeginHorizontal();
			GUILayout.Label(GUIContent.none, "TE BoxBackground", GUILayout.Width(lineColumnWidth));
			GUILayout.Label("Current", "TE BoxBackground", GUILayout.Width(columnWidth));
			GUILayout.Label("Generated", "TE BoxBackground", GUILayout.Width(columnWidth));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.BeginScrollView(scrollPos, "PreBackground", GUILayout.Width(lineColumnWidth));
			int maxLines = Mathf.Min(generatedFileLines.Count, currentFileLines.Count);
			for (int x = 0; x < maxLines; x++)
			{
				GUILayout.Label((x+1).ToString(), codeStyle);
			}
			GUILayout.EndScrollView();

			Vector2 scrollPos1 = GUILayout.BeginScrollView(scrollPos, "PreBackground", GUILayout.Width(columnWidth));
			DrawList(currentFileLines, matchingCurrentFileLines, Color.white);
			GUILayout.EndScrollView();

			Vector2 scrollPos2 = GUILayout.BeginScrollView(scrollPos, "PreBackground", GUILayout.Width(columnWidth));
			DrawList(generatedFileLines, matchingGeneratedFileLines, Color.white);
			GUILayout.EndScrollView();

			if (Mathf.Abs(scrollPos.y - scrollPos1.y) >= .1f || Mathf.Abs(scrollPos.x - scrollPos1.x) >= .1f)
			{
				scrollPos = scrollPos1;
			}
			else if (Mathf.Abs(scrollPos.y - scrollPos2.y) >= .1f || Mathf.Abs(scrollPos.x - scrollPos2.x) >= .1f)
			{
				scrollPos = scrollPos2;
			}

			GUILayout.EndHorizontal();
		}

		[NonSerialized] private Color matchingTextColor = new Color(.5f, .5f, .5f);

		void DrawList(List<string> list, List<int> matchingLineIndexes, Color c)
		{
			for (int x = 0; x < list.Count; x++)
			{
				GUI.color = matchingLineIndexes.Contains(x) ? matchingTextColor : c;

				GUIContent content = new GUIContent(list[x]);
				Rect itemRect = GUILayoutUtility.GetRect(content, codeStyle);
				GUI.Label(itemRect, list[x], codeStyle);
			}
			GUI.color = Color.white;
		}

		private void DrawToolbar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			GUILayout.FlexibleSpace();

			if (_selectedProcessor != null)
			{
				if (_selectedProcessor.areFilesMatching)
				{
					GUI.color = Color.green;
					GUILayout.Label("Ready", EditorStyles.miniLabel);
				}
				else
				{
					GUI.color = Color.red;
					GUILayout.Label("Requires Generation", EditorStyles.miniLabel);
				}

				GUI.color = Color.white;
				if (GUILayout.Button("Generate", EditorStyles.toolbarButton))
				{
					_selectedProcessor.WriteToFile();
					AssetDatabase.Refresh();

					// refresh
					SelectType(_selectedProcessor);
				}
			}

			GUILayout.Space(12);
			GUILayout.EndHorizontal();
		}
	}
}