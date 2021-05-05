#if UNITY_EDITOR

using System;
using System.Collections.Generic;
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
        private List<ScopeDefinition> scopes;

        public NetworkScopesWindow()
        {
            InitializeScopes();
        }

        void InitializeScopes()
        {
            serializer = new SerializationProvider();
            scopes = NetworkScopeUtility.FindScopeDefinitions(serializer);
        }

        void OnGUI()
        {
            GUILayout.Label("Detected Scopes", EditorStyles.boldLabel);
            foreach (ScopeDefinition scopeDefinition in scopes)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(scopeDefinition.scopeDefinition.type.Name);
                if (GUILayout.Button("Generate", GUILayout.Width(86)))
                {
                    NetworkScopeProcessor.GenerateNetworkScopes(serializer, new List<ScopeDefinition>() { scopeDefinition }, false);
                }
                GUILayout.EndHorizontal();
                
                // GUILayout.BeginHorizontal();
                // GUILayout.EndHorizontal();
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
                GUILayout.Label("Serialization (failed)", EditorStyles.boldLabel);
            
                foreach (KeyValuePair<Type, SerializationFailureReason> kvp in serializer.failedTypes)
                {
                    GUILayout.Label(kvp.Key.Name);
                    GUILayout.Label(kvp.Value.ToString());
                    GUILayout.Space(4);
                }
            }
        }
    }
}

#endif