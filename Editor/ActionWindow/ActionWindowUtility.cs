using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vertx.Editor
{
	internal static class ActionWindowUtility
	{
		private enum ReturnType
		{
			Invalid,
			Single,
			Multiple
		}

		public static HashSet<ActionOperation> GetActions()
		{
			HashSet<ActionOperation> allOperations = new HashSet<ActionOperation>();

			TypeCache.MethodCollection methods = TypeCache.GetMethodsWithAttribute<ActionProviderAttribute>();
			Type returnType = typeof(ActionOperation);
			Type alternateReturnType = typeof(IEnumerable<ActionOperation>);
			foreach (MethodInfo methodInfo in methods)
			{
				if (!methodInfo.IsStatic)
				{
					LogWarning("is not static.");
					continue;
				}


				ReturnType type;
				if (methodInfo.ReturnType == returnType)
				{
					type = ReturnType.Single;
				}
				else if (methodInfo.ReturnType == alternateReturnType)
				{
					type = ReturnType.Multiple;
				}
				else
				{
					LogWarning($"does not return a {nameof(ActionOperation)} or {nameof(IEnumerable<ActionOperation>)}");
					continue;
				}


				switch (type)
				{
					case ReturnType.Invalid:
						continue;
					case ReturnType.Single:
					{
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

						AppendOperation(operation);
						break;
					}
					case ReturnType.Multiple:
					{
						IEnumerable<ActionOperation> operations;
						try
						{
							operations = (IEnumerable<ActionOperation>) methodInfo.Invoke(null, null);
						}
						catch (Exception e)
						{
							Debug.LogException(e);
							continue;
						}

						foreach (ActionOperation operation in operations)
						{
							AppendOperation(operation);
						}
						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}

				void AppendOperation(ActionOperation operation)
				{
					if (operation == null)
					{
						//LogWarning($"returned a null {nameof(ActionOperation)}.");
						return;
					}

					allOperations.Add(operation);
				}

				void LogWarning(string message) => Debug.LogWarning($"{methodInfo.DeclaringType}.{methodInfo.Name} with {nameof(ActionProviderAttribute)} {message}");
			}

			return allOperations;
		}
	}
}