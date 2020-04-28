using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Vertx.Editor.Extensions;
using Object = UnityEngine.Object;

namespace Vertx.Testing.Editor
{
	public class MissingBehaviourTests : ReferenceTests
	{
		#region CheckForUnassignedUnityEvents
		/*
		 * A lot of this code is stripped out from the Unity Event Visualiser from MephestoKhaan
		 * https://github.com/MephestoKhaan/UnityEventVisualizer
		 * Share the love!
		 */
		public class EventCall
		{
			public Object Sender;
			public Object Receiver;
			public string EventShortName;
			public string EventFullName;
			public UnityEventBase UnityEvent;
			public string Method;

			public EventCall(Object sender, Object receiver, string eventShortName, string eventFullName, string methodName, UnityEventBase unityEvent)
			{
				Sender = sender is Component sComp ? sComp.gameObject : sender;
				Receiver = receiver is Component rComp ? rComp.gameObject : receiver;
				EventShortName = eventShortName;
				EventFullName = eventFullName;
				Method = methodName;
				UnityEvent = unityEvent;
			}
		}
		
		[Test]
		public void CheckForUnassignedUnityEventsInBuildScenes()
		{
			HashSet<Type> componentsThatCanHaveUnityEvent = new HashSet<Type>();
			RefreshTypesThatCanHoldUnityEvents(componentsThatCanHaveUnityEvent);
			
			RunFunctionOnSceneRootGameObjects(g => CheckForUnassignedUnityEvents(g.transform, componentsThatCanHaveUnityEvent));
		}


		public static void CheckForUnassignedUnityEvents (Transform root, HashSet<Type> componentsThatCanHaveUnityEvent)
		{
			HashSet<EventCall> calls = new HashSet<EventCall>();
			foreach (var type in componentsThatCanHaveUnityEvent) {
				foreach (var o in root.GetComponentsInChildren(type, true)) {
					var caller = o;
					ExtractDefaultEventTriggers(calls, caller);
					ExtractEvents(calls, caller);
				}
			}

			foreach (EventCall call in calls)
			{
//				Debug.Log($"{call.sender} : {call.receiver} : {call.eventFullName}");
				Assert.NotNull(call.Receiver, $"Unity Event on \"{LogObjectWithPath(call.Sender)}\" has a null receiver. {call.EventFullName}");
				Assert.False(string.IsNullOrEmpty(call.Method), $"Unity Event on \"{LogObjectWithPath(call.Sender)}\" with receiver \"{call.Receiver}\" targets a null method. {call.EventFullName}");
			}
			
			string LogObjectWithPath (Object @object)
			{
				Transform transform;
				switch (@object)
				{
					case Component component:
						transform = component.transform;
						break;
					case GameObject gameObject:
						transform = gameObject.transform;
						break;
					default:
						return @object.ToString();
				}
				return AnimationUtility.CalculateTransformPath(transform, null);
			}
		}
		
		private static void ExtractEvents(HashSet<EventCall> calls, Component caller)
        {
            SerializedProperty iterator = new SerializedObject(caller).GetIterator();
            iterator.Next(true);
			RecursivelyExtractEvents(calls, caller, iterator, 0);
        }

		private static bool RecursivelyExtractEvents(HashSet<EventCall> calls, Component caller, SerializedProperty iterator, int level) {
			bool hasData = true;

			do {
				SerializedProperty persistentCalls = iterator.FindPropertyRelative("m_PersistentCalls.m_Calls");
				bool isUnityEvent = persistentCalls != null;
				if (isUnityEvent && persistentCalls.arraySize > 0) {
					UnityEventBase unityEvent = GetTargetObjectOfProperty(iterator) as UnityEventBase;
					AddEventCalls(calls, caller, unityEvent, iterator.displayName, iterator.propertyPath);
				}
				hasData = iterator.Next(!isUnityEvent);
				if (!hasData)
					continue;
				if (iterator.depth < level) return true;
				if (iterator.depth > level) hasData = RecursivelyExtractEvents(calls, caller, iterator, iterator.depth);
			}
			while (hasData);
			return false;
		}

		#region GetTargetObjectOfProperty

		/// <summary>
		///  Gets the object the property represents.
		/// </summary>
		private static object GetTargetObjectOfProperty(SerializedProperty prop) {
			var path = prop.propertyPath.Replace(".Array.data[", "[");
			object obj = prop.serializedObject.targetObject;
			var elements = path.Split('.');
			foreach (var element in elements) {
				if (element.Contains("[")) {
					var elementName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
					var index = Convert.ToInt32(element.Substring(element.IndexOf("[", StringComparison.Ordinal)).Replace("[", "").Replace("]", ""));
					obj = GetValue_Imp(obj, elementName, index);
				}
				else {
					obj = GetValue_Imp(obj, element);
				}
			}
			return obj;
		}
		
		private static object GetValue_Imp(object source, string name) {
			if (source == null)
				return null;
			var type = source.GetType();

			while (type != null) {
				var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (f != null)
					return f.GetValue(source);

				var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (p != null)
					return p.GetValue(source, null);

				type = type.BaseType;
			}
			return null;
		}

		private static object GetValue_Imp(object source, string name, int index) {
			var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
			if (enumerable == null) return null;
			var enm = enumerable.GetEnumerator();
			//while (index-- >= 0)
			//    enm.MoveNext();
			//return enm.Current;

			for (int i = 0; i <= index; i++) {
				if (!enm.MoveNext()) return null;
			}
			return enm.Current;
		}
		#endregion

        private static void ExtractDefaultEventTriggers(HashSet<EventCall> calls, Component caller)
        {
            EventTrigger eventTrigger = caller as EventTrigger;
            if (eventTrigger != null)
            {
                foreach (EventTrigger.Entry trigger in eventTrigger.triggers)
                {
					string name = trigger.eventID.ToString();
					AddEventCalls(calls, caller, trigger.callback, name, name);
                }
            }
        }

		private static void AddEventCalls(HashSet<EventCall> calls, Component caller, UnityEventBase unityEvent, string eventShortName, string eventFullName) {
			for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++) {
				string methodName = unityEvent.GetPersistentMethodName(i);
				Object receiver = unityEvent.GetPersistentTarget(i);
				
				calls.Add(new EventCall(caller, receiver, eventShortName, eventFullName, methodName, unityEvent));
			}
		}
		
		static void RefreshTypesThatCanHoldUnityEvents(HashSet<Type> componentsThatCanHaveUnityEvent) {
			
			Dictionary<Type, bool> tmpSearchedTypes = new Dictionary<Type, bool>();

			var objects = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic)
				.SelectMany(a =>
				{
					try
					{
						return a.GetTypes();
					}
					catch (ReflectionTypeLoadException)
					{
						//Catching this just to avoid exceptions --- change made by vertx
						return Array.Empty<Type>();
					}
				})
				.Where(t => typeof(Component).IsAssignableFrom(t));

			foreach (var obj in objects) {
				if (RecursivelySearchFields<UnityEventBase>(obj, tmpSearchedTypes)) {
					componentsThatCanHaveUnityEvent.Add(obj);
				}
			}
			tmpSearchedTypes.Clear();
		}

		/// <summary>
		/// Search for types that have a field or property of type <typeparamref name="T"/> or can hold an object that can.
		/// </summary>
		/// <typeparam name="T">Needle</typeparam>
		/// <param name="type">Haystack</param>
		/// <param name="tmpSearchedTypes"></param>
		/// <returns>Can contain some object <typeparamref name="T"/></returns>
		static bool RecursivelySearchFields<T>(Type type, Dictionary<Type, bool> tmpSearchedTypes) {
			if (tmpSearchedTypes.TryGetValue(type, out bool wanted)) return wanted;
			tmpSearchedTypes.Add(type, false);

			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			foreach (var fType in type.GetFields(flags).Where(f => !f.FieldType.IsPrimitive).Select(f => f.FieldType).Concat(type.GetProperties(flags).Select(p => p.PropertyType))) {
				if (typeof(T).IsAssignableFrom(fType)) {
					return tmpSearchedTypes[type] = true;
				}
				if (typeof(Object).IsAssignableFrom(fType)) {
					continue;
				}
				if (!tmpSearchedTypes.TryGetValue(fType, out wanted)) {
					if (RecursivelySearchFields<T>(fType, tmpSearchedTypes)) {
						return tmpSearchedTypes[type] = true;
					}
				}
				else if (wanted) {
					return tmpSearchedTypes[type] = true;
				}
			}

			if (type.IsArray) {
				if (RecursivelySearchFields<T>(type.GetElementType(), tmpSearchedTypes)) {
					return tmpSearchedTypes[type] = true;
				}
			}

			return false;
		}
		#endregion

		#region Missing Component Script

		[Test]
		public void CheckForMissingScriptsInBuildScenes()
			=> RunFunctionOnSceneRootGameObjects(CheckForMissingScriptReferencesFromRoot);

		private void CheckForMissingScriptReferencesFromRoot(GameObject root)
		{
			RecursivelyFindMissingComponents(root.transform);

			void RecursivelyFindMissingComponents(Transform transform)
			{
				Assert.Zero(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), $"{EditorUtils.GetPathForObject(transform.gameObject)} has a missing script Component.");
				foreach (Transform child in transform)
					RecursivelyFindMissingComponents(child);
			}
		}

		[Test]
		public void CheckForMissingScriptsInAssets()
			=> RunFunctionOnAssets(CheckForMissingScriptReferencesFromRoot, null);

		#endregion
	}
}