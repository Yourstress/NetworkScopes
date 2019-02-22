using System;
using System.Reflection;
using System.Threading.Tasks;
using CodeGeneration;


namespace NetworkScopes
{
	public struct SignalResponseData
	{
		public readonly Type responseType;
		public readonly bool isTask;

		public string ResponseTypeName => TypeUtility.GetTypeName(responseType);

		public SignalResponseData(MethodInfo method)
		{
			isTask = false;

			responseType = method.ReturnType;

			if (responseType.IsGenericType)
			{
				Type genericReturnType = responseType.GetGenericTypeDefinition();

				if (genericReturnType == typeof(Task<>))
				{
					responseType = responseType.GetGenericArguments()[0];
					isTask = true;
				}
			}
		}

		public void ImportTypesInto(IImporter importer)
		{
			importer.Import(responseType);

			if (responseType.IsGenericType)
			{
				foreach (Type genericArgument in responseType.GetGenericArguments())
				{
					importer.Import(genericArgument);
				}
			}
		}
	}
}