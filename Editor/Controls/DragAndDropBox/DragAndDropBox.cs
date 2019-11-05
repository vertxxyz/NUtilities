using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;
using Vertx.Extensions;
using Object = UnityEngine.Object;

namespace Vertx.Controls
{
	public class DragAndDropBox<T> : BaseField<T[]> where T : Object
	{
		private readonly Label fieldLabel;
		private readonly bool allowSceneObjects;
		public const string uSSClassName = "dragAndDrop";
		public const string acceptDropVariantUssClassName = "dragAndDropAccept";
		public const string centeredLabelUSSClassName = "dragAndDropCenteredLabel";

		public DragAndDropBox(string labelText, Action<T[]> onObjectsDropped, bool allowSceneObjects = false) : base(labelText, null)
		{
			this.allowSceneObjects = allowSceneObjects;
			styleSheets.Add(StyleExtensions.GetStyleSheet("DragAndDropBox"));
			AddToClassList(uSSClassName);
			labelElement.pickingMode = PickingMode.Ignore;
			labelElement.AddToClassList(centeredLabelUSSClassName);

			RegisterCallback<ChangeEvent<T[]>>(evt => onObjectsDropped.Invoke(evt.newValue));
			//TODO make this accessible by introducing a button-like behaviour
			focusable = false;
		}

		protected override void ExecuteDefaultActionAtTarget(EventBase evt)
		{
			if (evt.eventTypeId == EventBase<DragUpdatedEvent>.TypeId())
				OnDragUpdated(evt);
			else if (evt.eventTypeId == EventBase<DragPerformEvent>.TypeId())
				OnDragPerform(evt);
			else
			{
				if (evt.eventTypeId != EventBase<DragLeaveEvent>.TypeId())
					return;
				OnDragLeave();
			}
		}

		private T[] ValidateObjects()
		{
			List<T> validObjects = new List<T>();
			foreach (Object target in DragAndDrop.objectReferences)
			{
				if (target == null)
					continue;
				if (!allowSceneObjects && !EditorUtility.IsPersistent(target))
					continue;
				if (target is T t)
					validObjects.Add(t);
			}

			return validObjects.Count == 0 ? null : validObjects.ToArray();
		}

		private static MethodInfo _ValidateObjectFieldAssignmentMI;
		private static MethodInfo ValidateObjectFieldAssignment => typeof(EditorGUI).GetMethod("ValidateObjectFieldAssignment", BindingFlags.NonPublic | BindingFlags.Static);

		private void OnDragUpdated(EventBase evt)
		{
			if (ValidateObjects() == null)
				return;
			DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			AddToClassList(acceptDropVariantUssClassName);
			evt.StopPropagation();
		}

		private void OnDragPerform(EventBase evt)
		{
			T[] objects = ValidateObjects();
			if (objects == null)
				return;
			DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			value = objects;
			DragAndDrop.AcceptDrag();
			RemoveFromClassList(acceptDropVariantUssClassName);
			evt.StopPropagation();
		}

		private void OnDragLeave() => RemoveFromClassList(acceptDropVariantUssClassName);
	}
}