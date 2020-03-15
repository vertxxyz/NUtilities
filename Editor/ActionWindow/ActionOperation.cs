using System;
using Core;

namespace Vertx.Editor
{
	public sealed class ActionOperation : IPropertyDropdownItem
	{
		private readonly Action actionWhenSelected;

		public enum ActionTarget
		{
			None,
			Selection,
			Scene,
			All
		}


		public string Name { get; }
		public string Path { get; }
		private ActionTarget Target { get; }

		public ActionOperation(ActionTarget target, string name, string path, Action actionWhenSelected)
		{
			switch (target)
			{
				case ActionTarget.None:
					break;
				case ActionTarget.Selection:
					name = $"{name} (Selection)";
					break;
				case ActionTarget.Scene:
					name = $"{name} (In Scene)";
					break;
				case ActionTarget.All:
					name = $"{name} (All)";
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(target), target, null);
			}
			
			this.actionWhenSelected = actionWhenSelected;
			Name = name;
			Path = path;
			Target = ActionTarget.None;
		}
		
		public void RunAction() => actionWhenSelected();
	}
}