/*using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vertx.Extensions
{
	public class CodeGenerator : EditorWindow
	{
		[MenuItem("Window/Vertx/Code Generator")]
		private static void OpenWindow()
		{
			var window = GetWindow<CodeGenerator>();
			window.titleContent = new GUIContent("Code Generator", EditorGUIUtility.ObjectContent(null, typeof(MonoScript)).image);
			window.Show();
		}

		private TextField templatePathField;
		private const string templatePathFieldLabel = "Template Path";
		private const string templateDirectoryKey = "VertxTemplateDirectory";
		private string templateDirectory;
		private const string templatePathButtonLabel = "Locate Template";

		private Box remappingRoot;
		private VisualElement templateRemapGroup;
		private const string addMappingButtonLabel = "Add Mapping";
		private VisualElement templateContentsField;
		private const string removeItemButtonLabel = "Remove";

		private const string remapFromName = "RemapFrom";
		private const string remapToName = "RemapTo";

		private TextField createFromTemplateFileNameField;

		private Button createFromTemplateButton;
		private const string createFromTemplateButtonLabel = "Create from Template";

		private void OnEnable()
		{
			ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
			rootVisualElement.Add(scrollView);
			var root = scrollView;

			ResetTemplateDirectory();

			root.styleSheets.Add(StyleExtensions.GetStyleSheet("VertxShared"));
			root.styleSheets.Add(StyleExtensions.GetStyleSheet("VertxCodeGenerator"));
			root.contentContainer.AddToClassList("paddingBorder");

			//Template Path Field and Button
			templatePathField = new TextField(templatePathFieldLabel);
			templatePathField.SetEnabled(false);
			root.Add(templatePathField);
			Button templatePathButton = new Button(SetTemplatePath)
			{
				text = templatePathButtonLabel
			};
			root.Add(templatePathButton);

			//Remapping root
			remappingRoot = new Box();
			remappingRoot.SetEnabled(false);
			remappingRoot.AddToClassList("innerPadding");
			remappingRoot.AddToClassList("marginAboveAndBelow");
			root.Add(remappingRoot);
			templateRemapGroup = new VisualElement();
			remappingRoot.Add(templateRemapGroup);

			Button addMappingButton = new Button(AddMappingUI)
			{
				text = addMappingButtonLabel
			};
			remappingRoot.Add(addMappingButton);

			//Template Contents
			templateContentsField = new VisualElement();
			templateContentsField.styleSheets.Add(StyleExtensions.GetStyleSheet("CsharpHighlighting"));
//			templateContentsField.SetEnabled(false);
			root.Add(templateContentsField);

			//Create button
			createFromTemplateButton = new Button(CreateFromTemplate)
			{
				text = createFromTemplateButtonLabel
			};
			createFromTemplateButton.SetEnabled(false);
			root.Add(createFromTemplateButton);
		}

		void AddMappingUI() => AddMappingUI(null);

		void AddMappingUI(string fromDefault)
		{
			VisualElement horizontalGroup = new VisualElement();
			horizontalGroup.AddToClassList("horizontalGroup");
			templateRemapGroup.Add(horizontalGroup);

			TextField mapFromField = new TextField
			{
				name = remapFromName,
				isDelayed = true
			};
			mapFromField.AddToClassList("remappingField");
			mapFromField.RegisterCallback<ChangeEvent<string>>(HighlightCode);
			Label mappingLabel = new Label(":");
			mappingLabel.AddToClassList("mappingLabel");
			TextField mapToField = new TextField
			{
				name = remapToName,
				isDelayed = true
			};
			mapToField.AddToClassList("remappingField");

			Button removeItemButton = new Button(() =>
			{
				horizontalGroup.RemoveFromHierarchy();
				if (templateRemapGroup.childCount == 0)
					createFromTemplateButton.SetEnabled(false);
			})
			{
				text = removeItemButtonLabel
			};
			removeItemButton.AddToClassList("removeItem");

			horizontalGroup.Add(mapFromField);
			horizontalGroup.Add(mappingLabel);
			horizontalGroup.Add(mapToField);
			horizontalGroup.Add(removeItemButton);

			createFromTemplateButton.SetEnabled(true);

			if (fromDefault != null)
				mapFromField.value = fromDefault;
		}

		void HighlightCode(ChangeEvent<string> evt)
		{
			if (!string.IsNullOrEmpty(evt.previousValue))
				RemoveHighlightFromCode(evt.previousValue);
			string key = evt.newValue;
			if (string.IsNullOrEmpty(key))
				return;

			var q = templateContentsField.Query<Label>();
			q.ForEach(label =>
			{
				if (Regex.IsMatch(label.text, $@"(?<!\w){key}(?!\w)"))
					label.EnableInClassList("highlitForRemap", true);
			});
		}

		void RemoveHighlightFromCode(string key)
		{
			var q = templateContentsField.Query<Label>();
			q.ForEach(label =>
			{
				if (Regex.IsMatch(label.text, $@"(?<!\w){key}(?!\w)"))
					label.EnableInClassList("highlitForRemap", false);
			});
		}

		void ResetTemplateDirectory()
		{
			templateDirectory = EditorPrefs.HasKey(templateDirectoryKey) ? EditorPrefs.GetString(templateDirectoryKey) : Application.dataPath;
			if (templatePathField != null)
				templatePathField.value = string.Empty;
			templateRemapGroup?.Clear();
			remappingRoot?.SetEnabled(false);
		}

		void SetTemplatePath()
		{
			string templatePath = EditorUtility.OpenFilePanel("Select Template", templateDirectory, "cs");
			if (string.IsNullOrEmpty(templatePath))
			{
				ResetTemplateDirectory();
				return;
			}

			//Template path has been set.
			templatePathField.value = templatePath;

			AddCode(templateContentsField, File.ReadAllText(templatePath) /*.Replace("\t", "    ")#1#);

			templateDirectory = Path.GetDirectoryName(templatePath);
			EditorPrefs.SetString(templateDirectoryKey, templateDirectory);

			remappingRoot.SetEnabled(true);
			createFromTemplateButton.SetEnabled(false);
		}

		void AddCode(VisualElement root, string content)
		{
			root.Clear();

			//Scroll
			ScrollView codeScroll = new ScrollView(ScrollViewMode.Horizontal);
			VisualElement contentContainer = codeScroll.contentContainer;
			codeScroll.contentViewport.style.flexDirection = FlexDirection.Column;
			codeScroll.contentViewport.style.alignItems = Align.Stretch;
			codeScroll.AddToClassList("code-scroll");
			root.Add(codeScroll);

			contentContainer.ClearClassList();
			contentContainer.AddToClassList("code-container");
			VisualElement codeContainer = contentContainer;

			CSharpHighlighter highlighter = new CSharpHighlighter
			{
				AddStyleDefinition = false
			};
			// To add code, we first use the CSharpHighlighter to construct rich text for us.
			string highlit = highlighter.Highlight(content);
			// After constructing new rich text we pass the text back recursively through this function with the new parent.
			RichTextUtility.AddRichText(highlit, null, codeContainer, true); // only parse spans because this is all the CSharpHighlighter parses.
			//Finalise content container
			foreach (VisualElement child in codeContainer.Children())
			{
				if (child.ClassListContains(RichTextUtility.paragraphContainerClass))
				{
					child.AddToClassList("code");
					if (child.childCount == 1)
						RichTextUtility.AddInlineText("", child); //This seems to be required to get layout to function properly.
				}
			}

			//Begin Hack
			FieldInfo m_inheritedStyle = typeof(VisualElement).GetField("inheritedStyle", BindingFlags.NonPublic | BindingFlags.Instance);
			if (m_inheritedStyle == null)
				m_inheritedStyle = typeof(VisualElement).GetField("m_InheritedStylesData", BindingFlags.NonPublic | BindingFlags.Instance);
			Type inheritedStylesData = Type.GetType("UnityEngine.UIElements.StyleSheets.InheritedStylesData,UnityEngine");
			FieldInfo font = inheritedStylesData.GetField("font", BindingFlags.Public | BindingFlags.Instance);
			FieldInfo fontSize = inheritedStylesData.GetField("fontSize", BindingFlags.Public | BindingFlags.Instance);
			Font consola = (Font) EditorGUIUtility.Load("consola");

			contentContainer.Query<Label>().ForEach(l =>
			{
				l.AddToClassList("code");

				//Hack to regenerate the font size as Rich Text tags are removed from the original calculation.
				object value = m_inheritedStyle.GetValue(l);
				StyleFont fontVar = (StyleFont) font.GetValue(value);
				fontVar.value = consola;
				font.SetValue(value, fontVar);
				StyleLength fontSizeVar = 12; // = (StyleLength) fontSize.GetValue(value); //This doesn't seem to work properly, hard coded for now.
				fontSize.SetValue(value, fontSizeVar);
				m_inheritedStyle.SetValue(l, value);
				//it seems like whitespace is ignored in 2019.3+ so lets replace it
				l.text = l.text.Replace("\t", "    ");
				Vector2 measuredTextSize = l.MeasureTextSize(l.text.Replace('>', 'x').Replace(' ', 'x'), 0,
					VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
				l.style.width = measuredTextSize.x;
				l.style.height = measuredTextSize.y;
				l.AddToClassList("code-button-inline");

				l.RegisterCallback<MouseUpEvent>(evt => { AddMappingUI(l.text); });
			});

			//Button
			/*Button codeCopyButtonButtonContainer = new Button(() =>
			{
				EditorGUIUtility.systemCopyBuffer = content;
				Debug.Log("Copied Code to Clipboard");
			});
			codeCopyButtonButtonContainer.ClearClassList();
			codeCopyButtonButtonContainer.AddToClassList("code-button");
			codeCopyButtonButtonContainer.StretchToParentSize();
			codeContainer.Add(codeCopyButtonButtonContainer);#1#
		}

		void CreateFromTemplate()
		{
			string templatePath = templatePathField.value;
			Dictionary<string, string> mapping = new Dictionary<string, string>();
			foreach (var child in templateRemapGroup.Children())
			{
				var from = child.Q<TextField>(remapFromName);
				var to = child.Q<TextField>(remapToName);
				mapping.Add(from.value, to.value);
			}

			if (!CodeUtility.GetAndReplaceTextAtFilePath(templatePath, mapping, out string content))
				return;
				
			CodeUtility.SaveAndWriteFileDialog(Path.GetFileNameWithoutExtension(templatePath), content);
		}
	}
}*/