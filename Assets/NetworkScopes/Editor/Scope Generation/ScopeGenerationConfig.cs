using UnityEditor;

namespace NetworkScopes.CodeGeneration
{
	public static class ScopeGenerationConfig
	{
		public static bool AutoGenerateScopeClasses
		{
			get { return EditorPrefs.GetBool("NetworkScopes_autoGenScopes", true); }
			set { EditorPrefs.SetBool("NetworkScopes_autoGenScopes", value); }
		}
	}
}