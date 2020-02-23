using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Vertx.Extensions;
using Object = UnityEngine.Object;

namespace Vertx.Controls
{
	public class DragAndDropBox : DragAndDropBox<Object>
	{
		private Type objectType = typeof(Object);

		public override Type ObjectType
		{
			get => objectType;
			set
			{
				if (value == null)
				{
					Debug.LogError($"Value passed to {nameof(ObjectType)} cannot be null.");
					return;
				}

				if (!value.IsSubclassOf(typeof(Object)))
				{
					Debug.LogError($"Value passed to {nameof(ObjectType)} cannot be a type not assignable to UnityEngine.Object.");
					return;
				}
				objectType = value;
			}
		}

		// ReSharper disable once UnusedType.Global
		public new class UxmlFactory : UxmlFactory<DragAndDropBox, UxmlTraits> { }

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			readonly UxmlStringAttributeDescription text = new UxmlStringAttributeDescription {name = "text"};
			readonly UxmlBoolAttributeDescription allowSceneObjectsDesc = new UxmlBoolAttributeDescription {name = "allowSceneObjects", defaultValue = true};

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get
				{
					yield break;
					/*yield return new UxmlChildElementDescription(typeof(Label));
					yield return new UxmlChildElementDescription(typeof(Image));*/
				}
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var dragAndDropBox = (DragAndDropBox) ve;
				dragAndDropBox.AllowSceneObjects = allowSceneObjectsDesc.GetValueFromBag(bag, cc);
				dragAndDropBox.label = text.GetValueFromBag(bag, cc);
			}
		}
	}

	public class DragAndDropBox<T> : BaseField<T[]> where T : Object
	{
		private readonly Label fieldLabel;
		protected bool AllowSceneObjects;
		public const string uSSClassName = "dragAndDrop";
		public const string acceptDropVariantUssClassName = "dragAndDropAccept";
		public const string centeredLabelUSSClassName = "dragAndDropCenteredLabel";

		/// <summary>
		///   <para>The type of the objects that can be assigned.</para>
		/// </summary>
		public virtual Type ObjectType
		{
			get => typeof(T);
			// ReSharper disable once ValueParameterNotUsed
			set => Debug.LogError($"Cannot assign {nameof(ObjectType)} to generic {nameof(DragAndDropBox<T>)}");
		}


		public DragAndDropBox() : base(string.Empty, null)
		{
			AllowSceneObjects = true;
			styleSheets.Add(StyleExtensions.GetStyleSheet("DragAndDropBox"));
			AddToClassList(uSSClassName);
			labelElement.pickingMode = PickingMode.Ignore;
			labelElement.AddToClassList(centeredLabelUSSClassName);
			focusable = false;
		}

		public DragAndDropBox(string labelText, Action<T[]> onObjectsDropped, bool allowSceneObjects = false) : base(labelText, null)
		{
			AllowSceneObjects = allowSceneObjects;
			styleSheets.Add(StyleExtensions.GetStyleSheet("DragAndDropBox"));
			AddToClassList(uSSClassName);
			labelElement.pickingMode = PickingMode.Ignore;
			labelElement.AddToClassList(centeredLabelUSSClassName);

			RegisterCallback<ChangeEvent<T[]>>(evt => onObjectsDropped.Invoke(evt.newValue));
			//TODO make this accessible by introducing a button-like behaviour
			focusable = false;
		}
		
		public void Register(Action<T[]> action) => RegisterCallback<ChangeEvent<T[]>>(evt => action.Invoke(evt.newValue));
		public void RegisterSingle(Action<T> action) => RegisterCallback<ChangeEvent<T[]>>(evt => action.Invoke(evt.newValue[0]));

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
				if (!AllowSceneObjects && !EditorUtility.IsPersistent(target))
					continue;
				if (target is T t && ObjectType.IsInstanceOfType(t))
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