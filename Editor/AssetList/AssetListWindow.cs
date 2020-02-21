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
		private List<Context> contexts;
		public List<Context> Contexts => contexts;
		
		private MultiColumnTreeView treeView;
		[SerializeField] TreeViewState treeViewState;
		[SerializeField] MultiColumnHeaderState multiColumnHeaderState;
		
		#endregion

		public class Context
		{
			public readonly string Name;
			private Action onGUI;
			
			public Context(string name)
			{
				Name = name;
			}

			public void OnGUI(Rect cellRect) => onGUI?.Invoke();

			public static object GetValue(Context context)
			{
				
				//TODO
				throw new NotImplementedException();
			}

			public void DoTint()
			{
				/*EditorGUI.DrawRect(args.rowRect, new Color(1f, 0f, 0f, 0.15f));
				EditorGUI.DrawRect(args.rowRect, new Color(0f, 0.5f, 1f, 0.15f));
				Color color = GUI.color;
				color.a *= 0.3f;
				GUI.color = color;*/
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
			rootVisualElement.Clear();
			if(configuration == null)
				InitialiseWithoutConfiguration();
			else
				InitialiseWithConfiguration(configuration);
		}

		private void InitialiseWithoutConfiguration()
		{
			VisualTreeAsset uxml = StyleExtensions.GetUXML("BlankAssetList");
			uxml.CloneTree(rootVisualElement);
			rootVisualElement.Q<DragAndDropBox>("DropTarget").RegisterSingle(CreateNewWindow);

			void CreateNewWindow(Object o)
			{
				AssetListConfiguration listConfiguration = CreateInstance<AssetListConfiguration>();
				listConfiguration.Configure(o);
				string name = o.GetType().Name;
				string path = EditorUtility.SaveFilePanelInProject($"Create New {name} List Configuration", $"{name} List", "asset", $"Save a Configuration asset for {name} List");
				if (string.IsNullOrEmpty(path)) return;
				AssetDatabase.CreateAsset(listConfiguration, path);
			}
		}
		
		internal void InitialiseWithConfiguration(AssetListConfiguration configuration)
		{
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
			
			Toolbar toolbar = new Toolbar();
			toolbar.Add(new ToolbarSearchField());
			rootVisualElement.Add(toolbar);
			
			IMGUIContainer imguiContainer = new IMGUIContainer(MultiColumnListGUI);
			rootVisualElement.Add(imguiContainer);
			
		}

		private MultiColumnHeaderState.Column[] GetColumnsFromConfiguration(AssetListConfiguration configuration)
		{
			List<MultiColumnHeaderState.Column> columns = new List<MultiColumnHeaderState.Column>();
			List<Context> contexts = new List<Context>();
			foreach (AssetListConfiguration.ColumnConfiguration c in configuration.Columns)
			{
				columns.Add(new MultiColumnHeaderState.Column
				{
					headerContent = new GUIContent(c.Title)
				});
				
				contexts.Add(new Context(
						c.Title
					));
			}

			this.contexts = contexts;
			return columns.ToArray();
		}

		private void MultiColumnListGUI()
		{
			
		}
		
		protected class MultiColumnTreeView : TreeView
		{
			private readonly AssetListWindow window;
			private readonly Queue<int> sortQueue = new Queue<int>();
			private List<Context> allContexts;

			public MultiColumnTreeView(TreeViewState state,
				MultiColumnHeader multicolumnHeader, AssetListWindow window)
				: base(state, multicolumnHeader)
			{
				this.window = window;
				allContexts = window.Contexts;
				showAlternatingRowBackgrounds = true;
				rowHeight = 20;
				multicolumnHeader.sortingChanged += OnSortingChanged;

				Reload();
			}

			private void OnSortingChanged(MultiColumnHeader multicolumnheader)
			{
				int index = multicolumnheader.sortedColumnIndex;
				sortQueue.Enqueue(index);
				if (sortQueue.Count > 3)
					sortQueue.Dequeue();

				int count = 0;
				IOrderedEnumerable<Context> initialOrder = null;
				foreach (int i in sortQueue.Reverse())
				{
					bool ascending = multiColumnHeader.IsSortedAscending(i);
					initialOrder = count++ == 0 ? OrderFirst(ascending) : OrderSubsequent(initialOrder, ascending);
				}

				allContexts = initialOrder.ToList();
				for (int i = 0; i < rootItem.children.Count; i++)
				{
					TreeViewItem content = rootItem.children[i];
					content.displayName = allContexts[i].Name;
				}

				IOrderedEnumerable<TQuery> Order<TQuery, TKey>
					(IEnumerable<TQuery> source, Func<TQuery, TKey> selector, bool ascending) =>
					ascending ? source.OrderBy(selector) : source.OrderByDescending(selector);

				IOrderedEnumerable<TQuery> ThenBy<TQuery, TKey>
					(IOrderedEnumerable<TQuery> source, Func<TQuery, TKey> selector, bool ascending) =>
					ascending ? source.ThenBy(selector) : source.ThenByDescending(selector);

				IOrderedEnumerable<Context> OrderFirst(bool ascending) =>
					Order(allContexts, Context.GetValue, ascending);

				IOrderedEnumerable<Context> OrderSubsequent(IOrderedEnumerable<Context> collection, bool ascending)
					=> ThenBy(collection, Context.GetValue, ascending);
			}

			protected override void RowGUI(RowGUIArgs args)
			{
				TreeViewItem item = args.item;
				Context context = allContexts[item.id];

				for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
				{
					Rect cellRect = args.GetCellRect(i);
					CellGUI(cellRect, item, args.GetColumn(i), ref args);
				}

				GUI.color = Color.white;
			}
			
			private void CellGUI(Rect cellRect, TreeViewItem treeContentValue, int columnIndex, ref RowGUIArgs args)
			{
				Context context = allContexts[treeContentValue.id];
				if (context == null)
				{
					Reload();
					return;
				}

				context.OnGUI(cellRect);
			}


			protected override TreeViewItem BuildRoot()
			{
				var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
				for (int i = 0; i < allContexts.Count; i++)
				{
					if (allContexts[i] == null) continue;
					root.AddChild(new TreeViewItem(i) {displayName = window.Contexts[i].Name}); //Need to set display name for the search
				}

				SetupDepthsFromParentsAndChildren(root);

				return root;
			}
		}
	}
}