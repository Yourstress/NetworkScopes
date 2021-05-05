
using System.Linq;

namespace NetworkScopes
{
	using System;
	using System.IO;
	
    public static class FileUtility
    {
        public static string FindInterfacePath(string typeName) => FindEntityPath("interface", typeName);
        public static string FindClassPath(string typeName) => FindEntityPath("class", typeName);

        private static string FindEntityPath(string entityName, string typeName)
        {
            #if UNITY_EDITOR
			string[] guids = UnityEditor.AssetDatabase.FindAssets(string.Format("t:MonoScript {0}", typeName));

			if (guids.Length == 0)
				throw new Exception("Could not find the file containing the type {typeName}. Please make sure the filename matches the {entityName} name.");

			string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);

			return path;
			#else
			string projectDirectory = Path.GetFullPath(@"../../../");

			string fileName = $"{typeName}.cs";
			string[] files = Directory.GetFiles(projectDirectory, fileName, SearchOption.AllDirectories);

			if (files.Length != 1)
			{
				files = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories);

				string match = $"{entityName} {typeName}";
				
				files = files.Where(f => File.ReadAllText(f).Contains(match)).ToArray();

				if (files.Length == 1)
					return files[0];

				throw new Exception($"Could not find file containing the {entityName} {typeName}");
			}

			return files[0];
			#endif
        }
    }
}