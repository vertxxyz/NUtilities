using System;
using UnityEngine.UIElements;
using Vertx.Extensions;

namespace Vertx.Controls
{
	public class HelpBox : VisualElement
	{
		public const string uSSClassName = "helpBox";
		public const string infoUssClassName = "consoleInfo";
		public const string warningUssClassName = "consoleWarning";
		public const string errorUssClassName = "consoleError";

		public enum MessageType
		{
			None,
			Info,
			Warning,
			Error
		}

		public HelpBox(string labelText, MessageType messageType = MessageType.None)
		{
			styleSheets.Add(StyleExtensions.GetStyleSheet("HelpBox"));
			AddToClassList(uSSClassName);

			switch (messageType)
			{
				case MessageType.None:
					break;
				case MessageType.Info:
					AddIconWithClass(infoUssClassName);
					break;
				case MessageType.Warning:
					AddIconWithClass(warningUssClassName);
					break;
				case MessageType.Error:
					AddIconWithClass(errorUssClassName);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
			}

			if (!string.IsNullOrEmpty(labelText))
			{
				Label l = new Label(labelText);
				Add(l);
			}

			void AddIconWithClass(string ussClass)
			{
				VisualElement image = new VisualElement();
				image.AddToClassList(ussClass);
				Add(image);
			}
		}
	}
}