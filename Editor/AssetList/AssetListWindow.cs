using System;
using System.Collections.Generic;
using System.Linq;
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
	internal class AssetListWindow : EditorWindow
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

		private Texture hoveredIcon;
		
		#endregion

		public class ColumnContext
		{
			private readonly string propertyName;
			private readonly string iconPropertyName;
			private readonly Action<Rect, SerializedProperty> onGUI;

			public enum GUIType
			{
				Property
			}

			public ColumnContext(string propertyName, string iconPropertyName, AssetListWindow window)
			{
				this.propertyName = propertyName;
				this.iconPropertyName = iconPropertyName;
				onGUI = (rect, property) => LargeObjectLabelWithPing(rect, property, iconPropertyName, window);
			}
			
			public ColumnContext(string propertyName, GUIType guiType)
			{
				this.propertyName = propertyName;
				switch (guiType)
				{
					case GUIType.Property:
						onGUI = Property;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(guiType), guiType, null);
				}
			}

			public void OnGUI(Rect cellRect, SerializedProperty @object) => onGUI?.Invoke(cellRect, @object);

			public SerializedProperty GetValue(SerializedObject context) => context.FindProperty(propertyName);

			public object GetSortableValue(SerializedObject context)
			{
				SerializedProperty property = GetValue(context);
				return AssetListUtility.GetSortableValue(property);
			}

			public void DoTint()
			{
				/*EditorGUI.DrawRect(args.rowRect, new Color(1f, 0f, 0f, 0.15f));
				EditorGUI.DrawRect(args.rowRect, new Color(0f, 0.5f, 1f, 0.15f));
				Color color = GUI.color;
				color.a *= 0.3f;
				GUI.color = color;*/
			}

			private static void Property(Rect r, SerializedProperty p) => EditorGUI.PropertyField(r, p, true);

			private static void LargeObjectLabelWithPing(Rect r, SerializedProperty p, string iconPropertyName, AssetListWindow window)
			{
				Object target = p.serializedObject.targetObject;
				if (!(target is Texture texture))
				{
					if (string.IsNullOrEmpty(iconPropertyName))
						texture = null;
					else
					{
						SerializedProperty iconProperty = p.serializedObject.FindProperty(iconPropertyName);
						texture = iconProperty.objectReferenceValue as Texture;
					}
				}
				Event e = Event.current;
				
				if (texture != null)
				{
					float h = r.height - 2;
					AssetListUtility.DrawTextureInRect(new Rect(r.x + 10, r.y + 1, h, h), texture);
					if (r.Contains(e.mousePosition) && focusedWindow == window)
						window.hoveredIcon = texture;
				}
				
				GUI.Label(
					new Rect(r.x + 10 + r.height, r.y, r.width - 10 - r.height, r.height),
					target.name);
				if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
					EditorGUIUtility.PingObject(target);
			}
		}

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
			if(configuration == null)
				InitialiseWithoutConfiguration();
			else
				InitialiseWithConfiguration(configuration);
		}

		private void InitialiseWithoutConfiguration()
		{
			rootVisualElement.Clear();
			
			VisualTreeAsset uxml = StyleExtensions.GetUXML("BlankAssetList");
			uxml.CloneTree(rootVisualElement);
			rootVisualElement.Q<DragAndDropBox>("DropTarget").RegisterSingle(CreateNewWindow);
			var container = rootVisualElement.Q<VisualElement>("ListViewContainer");
			AssetListConfiguration[] configurations = EditorUtils.LoadAssetsOfType<AssetListConfiguration>();
			if(configurations.Length == 0)
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
			
			this.configuration = configuration;
			objects = AssetListUtility.LoadAssetsByTypeName(configuration.TypeString, out type, out isComponent);
			
			if (treeViewState == null)
				treeViewState = new TreeViewState();
			
			bool firstInit = multiColumnHeaderState == null;
			var headerState = new MultiColumnHeaderState(GetColumnsFromConfiguration(configuration));
			if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
				MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);

			multiColumnHeaderState = headerState;

			var multiColumnHeader = new MultiColumnHeader(headerState);
			if (firstInit)
				multiColumnHeader.ResizeToFit();
			
			treeView = new MultiColumnTreeView(treeViewState, multiColumnHeader, this);

			(StyleSheet styleSheet, VisualTreeAsset uxml) = StyleExtensions.GetStyleSheetAndUXML("AssetList");
			uxml.CloneTree(rootVisualElement);
			rootVisualElement.styleSheets.Add(styleSheet);

			var toolbarSearchField = rootVisualElement.Q<ToolbarSearchField>("SearchField");
			string searchString = treeView.searchString;
			if(searchString != null)
				toolbarSearchField.SetValueWithoutNotify(treeView.searchString);
			toolbarSearchField.RegisterValueChangedCallback(evt => treeView.searchString = evt.newValue);

			assetListContainer = rootVisualElement.Q<IMGUIContainer>("AssetList");
			assetListContainer.onGUIHandler = MultiColumnListGUI;

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
					sortingArrowAlignment = TextAlignment.Left
				}
			};


			List<ColumnContext> contexts = new List<ColumnContext>
			{
				new ColumnContext("m_Name", configuration.IconPropertyPath, this)
			};
			
			foreach (AssetListConfiguration.ColumnConfiguration c in configuration.Columns)
			{
				columns.Add(new MultiColumnHeaderState.Column
				{
					headerContent = new GUIContent(c.Title)
				});
				
				contexts.Add(new ColumnContext(
					c.Title,
					ColumnContext.GUIType.Property
				));

			}

			columnContexts = contexts;
			return columns.ToArray();
		}

		private void MultiColumnListGUI()
		{
			Rect rect = assetListContainer.contentRect;
			treeView.OnGUI(new Rect(0, 0, rect.width, rect.height));
			
			if (hoveredIcon != null)
			{
				float scale = Mathf.Min(position.width, position.height) / 2f;
				float scaleHalf = scale / 2f;
				AssetListUtility.DrawTextureInRect(new Rect(position.width / 2f - scaleHalf, position.height / 2f - scaleHalf, scale, scale), hoveredIcon);
				hoveredIcon = null;
				Repaint();
			}
			else if (Event.current.mousePosition.x < treeView.multiColumnHeader.GetColumnRect(0).xMax && focusedWindow == this)
				Repaint();
		}
		
		protected class MultiColumnTreeView : TreeView
		{
			private readonly AssetListWindow window;
			private readonly Queue<int> sortQueue = new Queue<int>();
			private List<Object> allObjects;
			private readonly Dictionary<Object, SerializedObject> serializedObjectLookup = new Dictionary<Object, SerializedObject>();

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
						Order(allObjects, o=> columnContext.GetSortableValue(GetSerializedObject(o)));

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
				for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
				{
					Rect cellRect = args.GetCellRect(i);
					CellGUI(cellRect, item, args.GetColumn(i), ref args);
				}

				GUI.color = Color.white;
			}
			
			private void CellGUI(Rect cellRect, TreeViewItem treeContentValue, int columnIndex, ref RowGUIArgs args)
			{
				ColumnContext columnContext = window.columnContexts[columnIndex];
				if (columnContext == null)
				{
					Reload();
					return;
				}
				
				columnContext.OnGUI(cellRect, columnContext.GetValue(GetSerializedObject(allObjects[treeContentValue.id])));
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
	}
}