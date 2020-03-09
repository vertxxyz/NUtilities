using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vertx.Editor
{
	internal static class ActionWindowUtility
	{
		public static HashSet<ActionOperation> GetActions()
		{
			HashSet<ActionOperation> operations = new HashSet<ActionOperation>();
			
			TypeCache.MethodCollection methods = TypeCache.GetMethodsWithAttribute<ActionProviderAttribute>();
			Type returnType = typeof(ActionOperation);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.ReturnType != returnType)
				{
					LogWarning($"does not return a {nameof(ActionOperation)}");
					continue;
				}

				if (!methodInfo.IsStatic)
				{
					LogWarning("is not static.");
					continue;
				}

				ActionOperation operation;
				try
				{
					operation = (ActionOperation) methodInfo.Invoke(null, null);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					continue;
				}

				if (operation == null)
				{
					LogWarning($"returned a null {nameof(ActionOperation)}.");
					continue;
				}
				
				operations.Add(operation);

				void LogWarning(string message) => Debug.LogWarning($"{methodInfo.DeclaringType}.{methodInfo.Name} with {nameof(ActionProviderAttribute)} {message}");
			}

			return operations;
		}
	}
}