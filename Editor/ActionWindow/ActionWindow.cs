using System;
using System.Collections.Generic;
using Core;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using static Vertx.Editor.ActionWindowUtility;

namespace Vertx.Editor
{
	[AttributeUsage(AttributeTargets.Method)]
	// ReSharper disable once ClassNeverInstantiated.Global
	public class ActionProviderAttribute : Attribute { }

	public class ActionWindow : AdvancedDropdown
	{
		private static AdvancedDropdownState state;
		private static AdvancedDropdownState State => state ?? (state = new AdvancedDropdownState());

		private Dictionary<int, ActionOperation> lookup;

		[Shortcut("Window/Vertx/Action Window", KeyCode.Quote, ShortcutModifiers.Alt | ShortcutModifiers.Action)]
		private static void OpenWindow()
		{
			ActionWindow dropdown = new ActionWindow(State);
			int screenWidth = Screen.currentResolution.width;
			int screenHeight = Screen.currentResolution.height;

			float width = Mathf.Min(screenWidth / 2f, 640);

			dropdown.Show(new Rect(screenWidth / 2f - width / 2f, screenHeight - screenHeight / 2f, width, 0));
		}

		public ActionWindow(AdvancedDropdownState state) : base(state) { }

		protected override AdvancedDropdownItem BuildRoot()
		{
			HashSet<ActionOperation> actionOperations = GetActions();
			string title = Selection.objects.Length > 0 ? "Operate on Selection" : "Operate on All";

			(Dictionary<int, ActionOperation> lookup, AdvancedDropdownItem root) = PropertyDropdownUtils.GetStructure(actionOperations, title);
			this.lookup = lookup;
			return root;
		}

		protected override void ItemSelected(AdvancedDropdownItem item)
		{
			if (!lookup.TryGetValue(item.id, out var operation))
			{
				Debug.LogError($"{item.name} was not found. {nameof(PropertyDropdownUtils.GetStructure)} must have failed.");
				return;
			}
			operation.RunAction();
		}
	}
}