using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vertx.Controls;
using Vertx.Extensions;
using Object = UnityEngine.Object;

namespace Vertx.Editor
{
	internal class AssetListWindow : EditorWindow, IHasCustomMenu
	{
		#region Variables

		[SerializeField] private AssetListConfiguration configuration;
		private Type type;
		private bool isComponent;

		private List<Object> objects;
		private List<ColumnContext> columnContexts;

		private MultiColumnTreeView treeView;
		[SerializeField] TreeViewState treeViewState;
		[SerializeField] MultiColumnHeaderState multiColumnHeaderState;

		private IMGUIContainer assetListContainer;

		internal Texture HoveredIcon;

		#endregion

		[MenuItem("Window/Vertx/Asset List")]
		public static void OpenWindow()
		{
			var listWindow = GetUnopenedWindow();
			listWindow.Show();
		}

		public static AssetListWindow GetUnopenedWindow()
		{
			var listWindow = CreateInstance<AssetListWindow>();
			listWindow.titleContent = new GUIContent("Asset List");
			listWindow.minSize = new Vector2(500, 200);
			return listWindow;
		}

		private void OnEnable()
		{
			if (configuration == null)
				InitialiseWithoutConfiguration();
			else
				InitialiseWithConfiguration(configuration);
		}

		private void InitialiseWithoutConfiguration()
		{
			rootVisualElement.Clear();

			var data = StyleExtensions.GetStyleSheetAndUXML("BlankAssetList");
			data.Item2.CloneTree(rootVisualElement);
			rootVisualElement.styleSheets.Add(data.Item1);
			rootVisualElement.Q<DragAndDropBox>("DropTarget").RegisterSingle(CreateNewWindow);
			var container = rootVisualElement.Q<VisualElement>("ListViewContainer");
			AssetListConfiguration[] configurations = EditorUtils.LoadAssetsOfType<AssetListConfiguration>();
			if (configurations.Length == 0)
				container.RemoveFromHierarchy();
			else
			{
				var listView = container.Q<ListView>("ListView");
				foreach (AssetListConfiguration configAsset in configurations)
				{
					var button = new Button(() => InitialiseWithConfiguration(configAsset))
					{
						text = configAsset.name
					};
					button.AddToClassList("listViewButton");
					listView.Add(button);
				}
			}

			void CreateNewWindow(Object o)
			{
				AssetListConfiguration listConfiguration = CreateInstance<AssetListConfiguration>();
				listConfiguration.Configure(o);
				string name = o.GetType().Name;
				string path = EditorUtility.SaveFilePanelInProject($"Create New {name} List Configuration", $"{name} List", "asset", $"Save a Configuration asset for {name} List");
				if (string.IsNullOrEmpty(path)) return;
				AssetDatabase.CreateAsset(listConfiguration, path);
				InitialiseWithConfiguration(listConfiguration);
			}
		}

		internal void InitialiseWithConfiguration(AssetListConfiguration configuration)
		{
			rootVisualElement.Clear();

			bool changedConfiguration = this.configuration != configuration;
			this.configuration = configuration;
			objects = AssetListUtility.LoadAssetsByTypeName(configuration.TypeString, out type, out isComponent, configuration.AssetContext);
			GUIContent objectContent = EditorGUIUtility.ObjectContent(null, type);
			titleContent = new GUIContent(configuration.name, objectContent.image);

			if (treeViewState == null)
				treeViewState = new TreeViewState();

			var headerState = new MultiColumnHeaderState(GetColumnsFromConfiguration(configuration));
			if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
				MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);

			if (changedConfiguration)
			{
				initialisedSizes = false;
				multiColumnHeaderState = headerState;
			}

			var multiColumnHeader = new MultiColumnHeader(headerState);

			treeView = new MultiColumnTreeView(treeViewState, multiColumnHeader, this);

			(StyleSheet styleSheet, VisualTreeAsset uxml) = StyleExtensions.GetStyleSheetAndUXML("AssetList");
			uxml.CloneTree(rootVisualElement);
			rootVisualElement.styleSheets.Add(styleSheet);

			var toolbarSearchField = rootVisualElement.Q<ToolbarSearchField>("SearchField");
			string searchString = treeView.searchString;
			if (searchString != null)
				toolbarSearchField.SetValueWithoutNotify(treeView.searchString);
			toolbarSearchField.RegisterValueChangedCallback(evt => treeView.searchString = evt.newValue);

			var toolbarMenu = rootVisualElement.Q<ToolbarMenu>("ExportMenu");
			toolbarMenu.menu.AppendAction("TSV", action => ExportToTSV());

			assetListContainer = rootVisualElement.Q<IMGUIContainer>("AssetList");
			assetListContainer.onGUIHandler = MultiColumnListGUI;
		}

		private void ExportToTSV()
		{
			StringBuilder sB = new StringBuilder();

			void Append(object value)
			{
				sB.Append(value);
				sB.Append('\t');
			}

			//Append all the headers
			var columns = GetColumnsFromConfiguration(configuration);
			foreach (var column in columns)
				Append(column.headerContent.text);
			sB.Append('\n');


			var orderedItems = objects.OrderBy(i => i.name);
			foreach (var t in orderedItems)
			{
				using (SerializedObject sO = new SerializedObject(t))
				{
					foreach (ColumnContext context in columnContexts)
					{
						switch (context.TryGetValue(sO, out SerializedProperty property, out object alternateValue))
						{
							case ColumnContext.PropertyOverride.None:
								if (property != null)
									sB.Append(AssetListUtility.GetString(
										property,
										configuration,
										configuration.Columns[context.ConfigColumnIndex]
									));
								break;
							case ColumnContext.PropertyOverride.Name:
								sB.Append(AssetListUtility.GetString((string) alternateValue, configuration.NameDisplay));
								break;
							case ColumnContext.PropertyOverride.Path:
								sB.Append((string)alternateValue);
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						sB.Append('\t');
					}
				}

				sB.Append('\n');
			}

			CodeUtility.SaveAndWriteFileDialog(configuration.name, sB.ToString(), "tsv");
		}

		private MultiColumnHeaderState.Column[] GetColumnsFromConfiguration(AssetListConfiguration configuration)
		{
			List<MultiColumnHeaderState.Column> columns = new List<MultiColumnHeaderState.Column>
			{
				new MultiColumnHeaderState.Column
				{
					headerContent = new GUIContent("Name"),
					allowToggleVisibility = false,
					autoResize = true,
					headerTextAlignment = TextAlignment.Center,
					sortingArrowAlignment = TextAlignment.Left,
					minWidth = 30
				}
			};

			List<ColumnContext> contexts = new List<ColumnContext>
			{
				new ColumnContext(configuration, configuration.NameDisplay, this)
			};

			if ((configuration.AdditionalColumns & AdditionalColumns.Path) != 0)
			{
				columns.Add(new MultiColumnHeaderState.Column
				{
					headerContent = new GUIContent("Path"),
					allowToggleVisibility = true,
					autoResize = true,
					headerTextAlignment = TextAlignment.Center,
					sortingArrowAlignment = TextAlignment.Left,
					minWidth = 30
				});
				contexts.Add(new ColumnContext(configuration, this));
			}

			if (configuration.Columns != null)
			{
				for (var i = 0; i < configuration.Columns.Length; i++)
				{
					AssetListConfiguration.ColumnConfiguration c = configuration.Columns[i];
					var columnTitleContent = new GUIContent(c.Title);
					contexts.Add(new ColumnContext(c, i));

					columns.Add(new MultiColumnHeaderState.Column
					{
						headerContent = new GUIContent(columnTitleContent),
						minWidth = AssetListUtility.GetMinWidth(c),
						autoResize = false
					});
				}
			}

			columnContexts = contexts;
			return columns.ToArray();
		}

		[SerializeField]
		private bool initialisedSizes;

		private void MultiColumnListGUI()
		{
			MultiColumnHeader multiColumnHeader = treeView.multiColumnHeader;
			if (!initialisedSizes)
			{
				//Debug.Log("Re-Init");
				GUIStyle style = "MultiColumnHeader";
				for (int i = 0; i < columnContexts.Count; i++)
				{
					var column = multiColumnHeader.GetColumn(i);
					column.width = Mathf.Max(column.minWidth, style.CalcSize(column.headerContent).x);
				}

				multiColumnHeader.ResizeToFit();
				initialisedSizes = true;
			}

			Rect rect = assetListContainer.contentRect;
			treeView.OnGUI(new Rect(0, 0, rect.width, rect.height));

			if (HoveredIcon != null)
			{
				float scale = Mathf.Min(position.width, position.height) / 2f;
				float scaleHalf = scale / 2f;
				AssetListUtility.DrawTextureInRect(new Rect(position.width / 2f - scaleHalf, position.height / 2f - scaleHalf, scale, scale), HoveredIcon);
				HoveredIcon = null;
				Repaint();
			}
			else if (Event.current.mousePosition.x < multiColumnHeader.GetColumnRect(0).xMax && focusedWindow == this)
				Repaint();
		}

		protected class MultiColumnTreeView : TreeView
		{
			private readonly AssetListWindow window;
			private readonly Queue<int> sortQueue = new Queue<int>();
			private List<Object> allObjects;
			private readonly Dictionary<Object, SerializedObject> serializedObjectLookup = new Dictionary<Object, SerializedObject>();

			private readonly GUIContent missingPropertyLabel = new GUIContent("Property was not found.", "The property listed for this column was not present on this Object.");

			private GUIStyle centeredMiniLabel;

			private GUIStyle CenteredMiniLabel => centeredMiniLabel ?? (centeredMiniLabel = new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleCenter
			});

			public MultiColumnTreeView(TreeViewState state,
				MultiColumnHeader multicolumnHeader, AssetListWindow window)
				: base(state, multicolumnHeader)
			{
				this.window = window;
				allObjects = window.objects;
				showAlternatingRowBackgrounds = true;
				rowHeight = 20;
				multicolumnHeader.sortingChanged += OnSortingChanged;

				Reload();
			}

			private SerializedObject GetSerializedObject(Object o)
			{
				if (serializedObjectLookup.TryGetValue(o, out var sO)) return sO;
				sO = new SerializedObject(o);
				serializedObjectLookup.Add(o, sO);
				return sO;
			}

			private void OnSortingChanged(MultiColumnHeader multicolumnheader)
			{
				int index = multicolumnheader.sortedColumnIndex;
				sortQueue.Enqueue(index);
				if (sortQueue.Count > 3)
					sortQueue.Dequeue();

				int count = 0;
				IOrderedEnumerable<Object> initialOrder = null;
				foreach (int i in sortQueue.Reverse())
				{
					bool ascending = multiColumnHeader.IsSortedAscending(i);
					ColumnContext columnContext = window.columnContexts[i];
					initialOrder = count++ == 0 ? OrderFirst() : OrderSubsequent(initialOrder);

					IOrderedEnumerable<TQuery> Order<TQuery, TKey>
						(IEnumerable<TQuery> source, Func<TQuery, TKey> selector) =>
						ascending ? source.OrderBy(selector) : source.OrderByDescending(selector);

					IOrderedEnumerable<TQuery> ThenBy<TQuery, TKey>
						(IOrderedEnumerable<TQuery> source, Func<TQuery, TKey> selector) =>
						ascending ? source.ThenBy(selector) : source.ThenByDescending(selector);

					IOrderedEnumerable<Object> OrderFirst() =>
						Order(allObjects, o => columnContext.GetSortableValue(GetSerializedObject(o)));

					IOrderedEnumerable<Object> OrderSubsequent(IOrderedEnumerable<Object> collection)
						=> ThenBy(collection, o => columnContext.GetSortableValue(GetSerializedObject(o)));
				}

				allObjects = initialOrder.ToList();
				for (int i = 0; i < rootItem.children.Count; i++)
				{
					TreeViewItem content = rootItem.children[i];
					content.displayName = allObjects[i].name;
				}
			}

			protected override void RowGUI(RowGUIArgs args)
			{
				TreeViewItem item = args.item;
				SerializedObject serializedObject = GetSerializedObject(allObjects[item.id]);
				for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
				{
					Rect cellRect = args.GetCellRect(i);
					CellGUI(cellRect, serializedObject, args.GetColumn(i));
				}

				GUI.color = Color.white;
			}

			private void CellGUI(Rect cellRect, SerializedObject serializedObject, int columnIndex)
			{
				ColumnContext columnContext = window.columnContexts[columnIndex];
				if (columnContext == null)
				{
					Reload();
					return;
				}

				if (columnContext.TryGetValue(serializedObject, out SerializedProperty property, out _) == ColumnContext.PropertyOverride.None)
				{
					if (property == null)
					{
						switch (window.configuration.MissingPropertyDisplay)
						{
							case MissingPropertyDisplay.RedWithWarning:
								EditorGUI.DrawRect(cellRect, new Color(1f, 0f, 0f, 0.15f));
								GUI.Label(cellRect, missingPropertyLabel, CenteredMiniLabel);
								break;
							case MissingPropertyDisplay.Blank:
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						return;
					}
				}

				columnContext.OnGUI(cellRect, serializedObject, property);
			}


			protected override TreeViewItem BuildRoot()
			{
				var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
				for (int i = 0; i < allObjects.Count; i++)
				{
					if (allObjects[i] == null) continue;
					root.AddChild(new TreeViewItem(i) {displayName = allObjects[i].name}); //Need to set display name for the search
				}

				SetupDepthsFromParentsAndChildren(root);

				return root;
			}
		}

		public void AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent("Refresh from Configuration asset"), false, () =>
			{
				initialisedSizes = false;
				OnEnable();
			});
			menu.AddItem(new GUIContent("Select Configuration asset"), false, () =>
			{
				EditorGUIUtility.PingObject(configuration);
				Selection.activeObject = configuration;
			});
		}
	}
}