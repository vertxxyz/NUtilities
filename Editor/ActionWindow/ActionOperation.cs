using System;
using Core;

namespace Vertx.Editor
{
	public sealed class ActionOperation : IPropertyDropdownItem
	{
		private readonly Func<bool> isValidForSelection;
		private readonly Action actionWhenSelected;

		private enum ActionTarget
		{
			None,
			Objects
		}


		public string Name { get; }
		public string Path { get; }
		private ActionTarget Target { get; }

		public ActionOperation(string name, string path, Func<bool> isValidForSelection, Action actionWhenSelected)
		{
			this.isValidForSelection = isValidForSelection;
			this.actionWhenSelected = actionWhenSelected;
			Name = name;
			Path = path;
			Target = ActionTarget.Objects;
		}

		public ActionOperation(string name, string path, Action actionWhenSelected)
		{
			this.actionWhenSelected = actionWhenSelected;
			Name = name;
			Path = path;
			Target = ActionTarget.None;
		}


		public bool Validate() => Target == ActionTarget.None || isValidForSelection();
		public void RunAction() => actionWhenSelected();
	}
}