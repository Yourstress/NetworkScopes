#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening.Plugins.Core;
using NetworkScopes.CodeGeneration;
using UnityEditor;
using UnityEngine;

namespace NetworkScopes
{
    public class NetworkScopesWindow : EditorWindow
    {
        [MenuItem("Tools/Network Scopes/Show Window %#n")]
        static void ShowWindow()
        {
            GetWindow<NetworkScopesWindow>("Network Scopes");
        }

        SerializationProvider serializer = new SerializationProvider();
        // private List<ScopeDefinition> scopes;
        // private string[] scopePaths;
        // private bool[] scopeGenerated;
        // private bool[] scopeChanged;

        class ScopeData
        {
            public readonly ScopeDefinition scope;
            public readonly ScopeFile abstractClass;
            public readonly ScopeFile concreteClass;
            public bool wasGenerated = false;

            public ScopeData(ScopeDefinition scope)
            {
                this.scope = scope;
                abstractClass = new ScopeFile(scope.GetAbstractScriptPath());
                concreteClass = new ScopeFile(scope.GetConcreteScriptPath());
                UpdateFileStates();
            }

            void UpdateFileStates()
            {
                abstractClass.state = GetFileState(abstractClass.path);
                concreteClass.state = GetFileState(concreteClass.path);
            }
            
            FileState GetFileState(string path)
            {
                if (File.Exists(path))
                    return scope.scopeDefinition.ToScriptWriter().ToString() == File.ReadAllText(path) ? 
                        FileState.NoChange : FileState.Changed;
                else
                    return FileState.Added;
            }
        }

        class ScopeFile
        {
            public string path;
            public FileState state;

            public ScopeFile(string scopePath)
            {
                path = scopePath.Replace(Application.dataPath, "Assets");
            }
        }

        enum FileState
        {
            Added,
            Changed,
            NoChange,
        }

        private ScopeData[] scopes;
        private Vector2 scrollPos;

        private void OnEnable()
        {
            InitializeScopes();
        }
        
        void InitializeScopes()
        {
            serializer = new SerializationProvider();
            scopes = NetworkScopeUtility.FindScopeDefinitions(serializer)
                .Select(s => new ScopeData(s))
                .ToArray();
        }

        void OnGUI()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.Label("Detected Scopes", EditorStyles.boldLabel);
            for (var scopeIndex = 0; scopeIndex < scopes.Length; scopeIndex++)
            {
                ScopeData scopeData = scopes[scopeIndex];
                ScopeDefinition scopeDefinition = scopeData.scope;

                GUILayout.BeginHorizontal();
                GUILayout.Label(scopeDefinition.scopeDefinition.type.Name.Replace("_Abstract", ""));
                GUI.enabled = !scopeData.wasGenerated;
                if (GUILayout.Button("Generate", GUILayout.Width(86)))
                {
                    scopeData.wasGenerated = true;
                    NetworkScopeProcessor.GenerateNetworkScopes(serializer, new List<ScopeDefinition>() {scopeDefinition}, false);
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();

                // path and other stuff
                GUILayout.BeginHorizontal();
                GUILayout.Space(12);
                GUILayout.BeginVertical();

                
                DrawScopeFile("Abstract Class Path", scopeData.abstractClass, false);
                DrawScopeFile("Concrete Class Path", scopeData.concreteClass, true);

                void DrawScopeFile(string text, ScopeFile scopeFile, bool ignoreChangedState)
                {
                    FileState state = scopeFile.state;

                    if (ignoreChangedState && state == FileState.Changed)
                        state = FileState.NoChange;
                    
                    GUILayout.BeginHorizontal();
                    GUI.color = state == FileState.Added ? new Color(0.22f, 0.76f, 0.41f) :
                        state == FileState.Changed ? new Color(1f, 0.63f, 0.2f) : new Color(.72f,.72f,.72f);
                    EditorGUILayout.TextField(text, scopeFile.path);
                    GUI.color = Color.white;
                    GUI.enabled = File.Exists(scopeFile.path);
                    if (GUILayout.Button("Ping", GUILayout.ExpandWidth(false)))
                    {
                        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scopeFile.path);
                        EditorGUIUtility.PingObject(script);
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(12);
            
            if (GUILayout.Button("Refresh"))
            {
                InitializeScopes();
            }
            
            
            // Serialization
            GUILayout.Label("Serialization", EditorStyles.boldLabel);

            foreach (Type type in serializer.serializableTypes)
            {
                GUILayout.Label(type.Name);
            }

            if (serializer.failedTypes.Count > 0)
            {
                GUI.color = Color.red;
                GUILayout.Label("Serialization (failed)", EditorStyles.boldLabel);
                GUI.color = Color.white;
            
                foreach (KeyValuePair<Type, SerializationFailureReason> kvp in serializer.failedTypes)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(kvp.Key.Name);
                    GUILayout.Label(kvp.Value.ToString());
                    GUILayout.Space(4);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }
    }
}

#endif