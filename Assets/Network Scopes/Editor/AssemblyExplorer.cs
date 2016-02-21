
using Mono;
using Mono.Cecil;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEngine;

public class AssemblyExplorer : IDisposable
{
	private string[] assemblyPaths = null;
	private WriterParameters[] assemblyWriterParams = null;
	private AssemblyDefinition[] assemblies = null;

	private HashSet<AssemblyDefinition> modifiedAssemblies = null;

	private bool areAssembliesLocked = false;

	public static AssemblyExplorer FromDirectory(string applicationPath, string assemblyDirectory)
	{
		AssemblyExplorer exp = new AssemblyExplorer(false);

		List<string> loadedAssemblyPaths = new List<string>();
		List<AssemblyDefinition> loadedAssemblies = new List<AssemblyDefinition>();
		List<WriterParameters> loadedAssemblyWriterParams = new List<WriterParameters>();

		// This will hold the paths to all the assemblies that will be processed
		HashSet<string> assemblyPaths = new HashSet<string>(Directory.GetFiles(assemblyDirectory, "*.dll", SearchOption.TopDirectoryOnly));
		HashSet<string> assemblySearchDirectories = new HashSet<string>();

		assemblyPaths.RemoveWhere(s => !s.Contains("Assembly-CSharp") || s.Contains("-firstpass"));

		assemblySearchDirectories.Add(Path.Combine(applicationPath, "Contents/UnityExtensions/Unity/Networking"));
//		assemblySearchDirectories.Add(Path.Combine(Application.dataPath, "../Library/ScriptAssemblies"));
		assemblySearchDirectories.Add(assemblyDirectory);

		exp.LoadAssemblies(assemblyPaths, assemblySearchDirectories, loadedAssemblyPaths, loadedAssemblies, loadedAssemblyWriterParams, applicationPath);

		return exp;
	}
	
	public static AssemblyExplorer FromLoadedAssemblies()
	{
		AssemblyExplorer exp = new AssemblyExplorer(true);

		List<string> loadedAssemblyPaths = new List<string>();
		List<AssemblyDefinition> loadedAssemblies = new List<AssemblyDefinition>();
		List<WriterParameters> loadedAssemblyWriterParams = new List<WriterParameters>();

		// This will hold the paths to all the assemblies that will be processed
		HashSet<string> assemblyPaths = new HashSet<string>();
		// This will hold the search directories for the resolver
		HashSet<string> assemblySearchDirectories = new HashSet<string>();

		// Add all assemblies in the project to be processed, and add their directory to
		// the resolver search directories.
		foreach( Assembly assembly in AppDomain.CurrentDomain.GetAssemblies() )
		{
			string assemName = assembly.GetName().Name;
			if (!assemName.StartsWith("Assembly-CSharp") &&
				assemName != "UnityEngine.Networking" ||
				assemName.Contains("Editor") ||
				assemName.Contains("-firstpass"))
			{
				continue;
			}
			
			// Only process assemblies which are in the project
			if( assembly.Location.Replace( '\\', '/' ).StartsWith( Application.dataPath.Substring( 0, Application.dataPath.Length - 7 ) ) )
					assemblyPaths.Add( assembly.Location );

			// But always add the assembly folder to the search directories
			assemblySearchDirectories.Add( Path.GetDirectoryName( assembly.Location ) );
		}

		exp.LoadAssemblies(assemblyPaths, assemblySearchDirectories, loadedAssemblyPaths, loadedAssemblies, loadedAssemblyWriterParams, EditorApplication.applicationPath);

		return exp;
	}
		
	void LoadAssemblies(HashSet<string> assemblyPaths, HashSet<string> assemblySearchDirectories, List<string> loadedAssemblyPaths, List<AssemblyDefinition> loadedAssemblies, List<WriterParameters> loadedAssemblyWriterParams, string applicationPath)
	{
		// Create resolver
		DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();
		// Add all directories found in the project folder
		foreach( String searchDirectory in assemblySearchDirectories )
		{
			assemblyResolver.AddSearchDirectory( searchDirectory );
		}
		// Add path to the Unity managed dlls
		assemblyResolver.AddSearchDirectory( Path.GetDirectoryName( applicationPath ) + "/Data/Managed" );

		// Create reader parameters with resolver
		ReaderParameters readerParameters = new ReaderParameters();
		readerParameters.AssemblyResolver = assemblyResolver;

		// Create writer parameters
		WriterParameters writerParameters = new WriterParameters();

		// Process any assemblies which need it
		foreach( String assemblyPath in assemblyPaths )
		{
			// mdbs have the naming convention myDll.dll.mdb whereas pdbs have myDll.pdb
			String mdbPath = assemblyPath + ".mdb";
			String pdbPath = assemblyPath.Substring( 0, assemblyPath.Length - 3 ) + "pdb";

			// Figure out if there's an pdb/mdb to go with it
			if( File.Exists( pdbPath ) )
			{
				readerParameters.ReadSymbols = true;
				readerParameters.SymbolReaderProvider = new Mono.Cecil.Pdb.PdbReaderProvider();
				writerParameters.WriteSymbols = true;
				writerParameters.SymbolWriterProvider = new Mono.Cecil.Mdb.MdbWriterProvider(); // pdb written out as mdb, as mono can't work with pdbs
			}
			else if( File.Exists( mdbPath ) )
			{
				readerParameters.ReadSymbols = true;
				readerParameters.SymbolReaderProvider = new Mono.Cecil.Mdb.MdbReaderProvider();
				writerParameters.WriteSymbols = true;
				writerParameters.SymbolWriterProvider = new Mono.Cecil.Mdb.MdbWriterProvider();
			}
			else
			{
				readerParameters.ReadSymbols = false;
				readerParameters.SymbolReaderProvider = null;
				writerParameters.WriteSymbols = false;
				writerParameters.SymbolWriterProvider = null;
			}

			// Read assembly
			AssemblyDefinition assemblyDefinition;

			try
			{
				assemblyDefinition = AssemblyDefinition.ReadAssembly( assemblyPath, readerParameters );
			}
			catch
			{
				assemblyDefinition = AssemblyDefinition.ReadAssembly( assemblyPath );
			}

//			Debug.Log("<color=yellow>Loaded assembly " + assemblyDefinition.FullName + " </color> from " + assemblyPath);
			//			assemblyDefinition.Name == "Assembly-CSharp

			// Process it if it hasn't already
			loadedAssemblies.Add(assemblyDefinition);
			loadedAssemblyPaths.Add(assemblyPath);
			loadedAssemblyWriterParams.Add(writerParameters);
		}

		this.assemblies = loadedAssemblies.ToArray();
		this.assemblyPaths = loadedAssemblyPaths.ToArray();
		this.assemblyWriterParams = loadedAssemblyWriterParams.ToArray();
	}
		
	public AssemblyExplorer(bool lockReloadAssemblies)
	{
		if (lockReloadAssemblies)
		{
			// Lock assemblies while they may be altered
			EditorApplication.LockReloadAssemblies();

			areAssembliesLocked = true;
		}
	}
	
	public void MarkAssemblyChanged(AssemblyDefinition assembly)
	{
		if (modifiedAssemblies == null)
			modifiedAssemblies = new HashSet<AssemblyDefinition>();
	
		if (!modifiedAssemblies.Contains(assembly))
			modifiedAssemblies.Add(assembly);
	}
	
	public void SaveChangedAssemblies()
	{
		if (modifiedAssemblies != null)
		{
			foreach (AssemblyDefinition def in modifiedAssemblies)
			{
				int assemLoc = Array.IndexOf(assemblies, def);
				
				def.Write(assemblyPaths[assemLoc], assemblyWriterParams[assemLoc]);
			}
		}
	}

	public TypeReference FindRelatedType<T>(ModuleDefinition module)
	{
		string fullTypeName = typeof(T).FullName;

		for (int x = 0; x < module.AssemblyReferences.Count; x++)
		{
			foreach (TypeReference relType in module.AssemblyResolver.Resolve(module.AssemblyReferences[x]).MainModule.Types)
			{
				if (relType.FullName == fullTypeName)
				{
					return relType;
				}
			}
		}

		return null;
	}

	public TypeDefinition FindTypeDefinition<T>()
	{
		Type t = typeof(T);
		
		foreach (AssemblyDefinition assembly in assemblies)
		foreach (ModuleDefinition module in assembly.Modules)
		foreach (TypeDefinition type in module.Types)
		{
			if (t.FullName == type.FullName)
				return type;
		}
		
		return null;
	}

	public List<TypeDefinition> FindSubclassTypes(Type baseClass)
	{
		string endpointBaseScopeName = baseClass.Name;

		List<TypeDefinition> foundTypes = new List<TypeDefinition>();

		foreach (AssemblyDefinition assembly in assemblies)
		{
			foreach (ModuleDefinition module in assembly.Modules)
			{
				foreach (TypeDefinition type in module.Types)
				{
					if (type.BaseType != null)
					{
						// make sure it's the right one
						if (type.BaseType.Name == endpointBaseScopeName &&
							type.BaseType.Namespace == baseClass.Namespace)
						{
							foundTypes.Add(type);
						}
					}
				}
			}
		}

		return foundTypes;
	}
	
	public List<KeyValuePair<TypeDefinition,CustomAttribute>> FindTypesWithAttribute(Type attributeType)
	{
		List<KeyValuePair<TypeDefinition,CustomAttribute>> foundTypes = new List<KeyValuePair<TypeDefinition,CustomAttribute>>(4);

		foreach (AssemblyDefinition assembly in assemblies)
		foreach (ModuleDefinition module in assembly.Modules)
		foreach (TypeDefinition type in module.Types)
		{
			CustomAttribute attr = FindAttributeInType(type, attributeType);
			
			if (attr != null)
				foundTypes.Add(new KeyValuePair<TypeDefinition,CustomAttribute>(type, attr));
		}
		
		return foundTypes;
	}

	public List<KeyValuePair<MethodDefinition,CustomAttribute>> FindMethodsWithAttribute(Type attributeType)
	{
		List<KeyValuePair<MethodDefinition,CustomAttribute>> foundMethods = new List<KeyValuePair<MethodDefinition,CustomAttribute>>(4);

		foreach (AssemblyDefinition assembly in assemblies)
			foreach (ModuleDefinition module in assembly.Modules)
				foreach (TypeDefinition type in module.Types)
					foreach (MethodDefinition method in type.Methods)
					{
						if (!method.HasBody)
							continue;
						
						CustomAttribute attr = FindAttributeInMethod(method, attributeType);

						if (attr != null)
							foundMethods.Add(new KeyValuePair<MethodDefinition,CustomAttribute>(method, attr));
					}

		return foundMethods;
	}
	
	public CustomAttribute FindAttributeInType(TypeDefinition typeDefinition, Type attributeType)
	{
		string attributeFullTypeName = attributeType.FullName;
		
		foreach (CustomAttribute attribute in typeDefinition.CustomAttributes)
		{
			if (attribute.AttributeType.FullName == attributeFullTypeName)
				return attribute;
		}
		
		return null;
	}

	public CustomAttribute FindAttributeInMethod(MethodDefinition methodDefinition, Type attributeType)
	{
		string attributeFullTypeName = attributeType.FullName;

		foreach (CustomAttribute attribute in methodDefinition.CustomAttributes)
		{
			if (attribute.AttributeType.FullName == attributeFullTypeName)
				return attribute;
		}

		return null;
	}

	public static int RemoveCallVirt(MethodDefinition method, out MethodDefinition operand)
	{ 
		for (int x = 0; x < method.Body.Instructions.Count; x++)
		{
			if (method.Body.Instructions[x].OpCode.Code == Code.Callvirt)
			{
				operand = ((MethodReference)method.Body.Instructions[x].Operand).Resolve();

				return RemoveCallVirtAtIndex(method, x);
			}
		}

		operand = null;

		return -1;
	}

	public static int RemoveCallVirtAtIndex(MethodDefinition method, int instIndex)
	{
		int lastRemovedIndex = -1;

		while (instIndex != -1)
		{
			Code code = method.Body.Instructions[instIndex].OpCode.Code;

			if (code != Code.Ldarg_0 &&
				code != Code.Ldfld &&
				code != Code.Callvirt)
				break;

//			UnityEngine.Debug.Log("REM " + code + " offfset " + method.Body.Instructions[instIndex].Offset);
			method.Body.Instructions.RemoveAt(instIndex);

			lastRemovedIndex = instIndex;

			instIndex--;
		}

		return lastRemovedIndex;
	}

	#region IDisposable implementation

	public void Dispose ()
	{
		// Unlock now that we're done
		if (areAssembliesLocked)
			EditorApplication.UnlockReloadAssemblies();
	}

	#endregion
}