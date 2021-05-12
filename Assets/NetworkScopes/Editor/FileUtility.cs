
using System.Linq;

namespace NetworkScopes
{
	using System;
	using System.IO;
	
    public static class FileUtility
    {
        public static string FindInterfacePath(string typeName, bool deepSearch) => FindEntityPath("interface", typeName, deepSearch);
        public static string FindClassPath(string typeName, bool deepSearch) => FindEntityPath("class", typeName, deepSearch);

        private static string FindEntityPath(string entityName, string typeName, bool deepSearch)
        {
            #if UNITY_EDITOR
			string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:MonoScript {typeName}");

			// return file if found by name
			if (guids.Length != 0)
			{
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
				
				if (Path.GetFileName(path) == $"{typeName}.cs")
					return path;
			}
			
			string projectDirectory = UnityEngine.Application.dataPath;
			
			#else
			string projectDirectory = Path.GetFullPath(@"../../../");

			string fileName = $"{typeName}.cs";
			string[] matchingFiles = Directory.GetFiles(projectDirectory, fileName, SearchOption.AllDirectories);

			// return file if found by name
			if (matchingFiles.Length == 1)
				return matchingFiles[0];
			#endif
	        
	        // otherwise, continue looking through the .cs files
	        if (deepSearch)
	        {
		        string[] files = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories);

		        string match = $"{entityName} {typeName}";
				
		        files = files.Where(f => File.ReadAllText(f).Contains(match)).ToArray();

		        if (files.Length == 1)
			        return files[0];
	        }

	        return null;
        }
    }
}