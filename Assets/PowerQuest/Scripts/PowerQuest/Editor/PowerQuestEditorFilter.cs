using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using PowerTools.Quest;
using PowerTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Assertions;


namespace PowerTools.Quest
{

// The list filtering code is by Arcnor. Thanks Arcnor!
public partial class PowerQuestEditor
{
	#region Variables: Static definitions

	private static readonly Color FilterHighlightColor = Color.yellow;

	enum FilterState 
	{
		All,
		Highlighted,
		Unparented,
		Parented
	}

	[Serializable]
	private class FilterContext 
	{
		public bool Show;

		public FilterState State;
		/**
		 * Only valid when FilterState == FilterState.Parented
		 */
		public string ParentPath;

		public FilterContext(bool showByDefault = true) {
			Show = showByDefault;
		}
	}	

	class PathsDropdown : AdvancedDropdown 
	{
		class PathItem : AdvancedDropdownItem 
		{
			public FilterState filterState { get; }
			[CanBeNull]
			public string filterParentPath { get; }

			public PathItem(string name, FilterState filterState, string filterParentPath = null) : base(name) 
			{
				this.filterState = filterState;
				this.filterParentPath = filterParentPath;
			}
		}

		readonly FilterContext _filterCtx;
		readonly Action _refreshFiltering;
		readonly bool _hasHighlighted;
		readonly bool _hasUnparented;

		readonly AdvancedDropdownItem _fakeRoot;

		public PathsDropdown(string gamePath, IEnumerable<MonoBehaviour> components, AdvancedDropdownState state, FilterContext filterCtx, Action refreshFiltering) : base(state) 
		{
			_filterCtx = filterCtx;
			_refreshFiltering = refreshFiltering;

			minimumSize = new Vector2(0, 200);
			_fakeRoot = new AdvancedDropdownItem("FakeRoot");

			foreach (var c in components) 
			{
				if (!_hasHighlighted && IsHighlighted(c)) 
				{
					_hasHighlighted = true;
				}

				var assetParentPath = GetAssetParentPath(gamePath, c);

				if (!string.IsNullOrEmpty(assetParentPath)) 
				{
					var parent = _fakeRoot;
					var parts = assetParentPath.Split('/');
					var currentPath = "";
					foreach (var part in parts) 
					{
						if (currentPath != "") 
						{
							currentPath += '/';
						}
						currentPath += part;

						var child = new PathItem(part, FilterState.Parented, currentPath);
						parent = GetOrAddNewChild(parent, child);
					}
				} 
				else 
				{
					_hasUnparented = true;
				}
			}
		}

		static AdvancedDropdownItem GetOrAddNewChild(AdvancedDropdownItem parent, PathItem newChild) 
		{
			foreach (var child in parent.children) 
			{
				if (child.name == newChild.name) 
					return child;				
			}

			parent.AddChild(newChild);
			return newChild;
		}

		protected override AdvancedDropdownItem BuildRoot() 
		{
			var root = new AdvancedDropdownItem("Filtering");

			root.AddChild(new PathItem("All", FilterState.All));

			if (_hasHighlighted) 
			{
				root.AddChild(new PathItem("Highlighted", FilterState.Highlighted));
			}

			if (_hasUnparented) 
			{
				root.AddChild(new PathItem("Unparented", FilterState.Unparented));
			}

			var childrenList = _fakeRoot.children.ToList();

			if (childrenList.Count > 0) 
			{
				root.AddSeparator();

				foreach (var child in childrenList) 
				{
					root.AddChild(child);
				}
			}

			return root;
		}

		protected override void ItemSelected(AdvancedDropdownItem item) 
		{
			if (item is PathItem pathItem) 
			{
				_filterCtx.State = pathItem.filterState;
				_filterCtx.ParentPath = pathItem.filterParentPath;
			} else 
			{
				_filterCtx.State = FilterState.All;
				_filterCtx.ParentPath = null;
			}

			_refreshFiltering.Invoke();
		}
	}	

	#endregion
	#region Variables: Serialized
	
	// Whether to show filtered list of prefabs, or the whole thing
	[SerializeField] FilterContext m_filterRooms = new FilterContext();
	[SerializeField] FilterContext m_filterCharacters = new FilterContext();
	[SerializeField] FilterContext m_filterInventory = new FilterContext();
	[SerializeField] FilterContext m_filterDialogTrees = new FilterContext();
	[SerializeField] FilterContext m_filterGuis = new FilterContext(false);
	
	#endregion
	#region Variables: Private
	
	//[CanBeNull] private string _lastDropdownName = null;

	#endregion
	#region Funcs

	static string GetAssetParentPath<T>(string gamePath, T component) where T : MonoBehaviour 
	{
		var assetPath = AssetDatabase.GetAssetPath(component);
		var assetDir = Path.GetDirectoryName(assetPath);

		if (string.IsNullOrEmpty(assetDir)) return "";

		var processedPath = assetDir.Substring(assetPath.IndexOf(gamePath, StringComparison.Ordinal) + gamePath.Length);
		#if UNITY_EDITOR_WIN
		var slashIdx = processedPath.IndexOf('\\');
		#else
		var slashIdx = processedPath.IndexOf('/');
		#endif
		
		processedPath = processedPath.Substring(slashIdx + 1);
		#if UNITY_EDITOR_WIN
		slashIdx = processedPath.LastIndexOf('\\');
		#else
		slashIdx = processedPath.LastIndexOf('/');
		#endif
		
		return slashIdx > 0
			? processedPath.Substring(0, slashIdx)
			: "";
	}

	void ApplyFilter<T>( List<T> prefabList, ref List<T> list, FilterContext filterCtx) where T : MonoBehaviour 
	{
		switch (filterCtx.State) {
			case FilterState.All:
				list = prefabList;
				break;

			case FilterState.Highlighted:
				list = prefabList.FindAll(IsHighlighted);
				break;

			case FilterState.Unparented:
				list = prefabList.FindAll(item => string.IsNullOrEmpty(GetAssetParentPath(m_gamePath, item)));
				break;

			case FilterState.Parented:
				list = prefabList.FindAll(item => GetAssetParentPath(m_gamePath, item) == filterCtx.ParentPath);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(filterCtx.State), filterCtx.State, null);
		}

		if (list.Count == 0) {
			list = prefabList;
			filterCtx.State = FilterState.All;
		}
	}

	static string GetFilterName(FilterContext filterCtx) => filterCtx.State == FilterState.Parented ? filterCtx.ParentPath : filterCtx.State.ToString();

	void LayoutListHeader<T>(List<T> allPrefabs, string name, FilterContext filterContext, Rect rect) where T : MonoBehaviour 
	{
		var foldoutWidth = 120;
		filterContext.Show = EditorGUI.Foldout(new Rect(rect) { width = foldoutWidth }, filterContext.Show, name, true);
		var buttonRect = new Rect(rect) { x = foldoutWidth, width = rect.width - foldoutWidth };

		var oldColor = GUI.contentColor;

		if (filterContext.State != FilterState.All) 
		{
			GUI.contentColor = FilterHighlightColor;
		}

		if (GUI.Button(buttonRect, GetFilterName(filterContext), new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight }))
		{
			GUI.contentColor = oldColor;

			if (allPrefabs.All(prefab => string.IsNullOrEmpty(GetAssetParentPath(m_gamePath, prefab)))) 
			{
				var oldState = filterContext.State;
				// Only toggle highlighting, no parents
				filterContext.State = filterContext.State != FilterState.All
					? FilterState.All
					: FilterState.Highlighted;

				if (filterContext.State == FilterState.Highlighted && !allPrefabs.Any(IsHighlighted)) 
				{
					filterContext.State = FilterState.All;
				}

				if (oldState != filterContext.State) 
				{
					CreateMainGuiLists();
				}
			} 
			else 
			{
				// TODO(arcnor): Avoid the dropdown from opening again if the button is pressed twice for the same `name`. Will need hackery because there is no "Close", thanks Unity!
				// TODO(arcnor): Maybe don't refresh all lists, but only the one that changed?
				// TODO(arcnor): We can keep the last element selected on the dropdown if we don't use a new AdvancedDropdownState() every time, but the current reset behavior might be better?
				var dropDown = new PathsDropdown(m_gamePath, allPrefabs, new AdvancedDropdownState(), filterContext, CreateMainGuiLists);
				dropDown.Show(rect);
			}
		}

		GUI.contentColor = oldColor;
	}

	ReorderableList FilterAndCreateReorderable<T>(
			string listName,
			List<T> allPrefabs, ref List<T> listPrefabs, FilterContext filterCtx,
			ReorderableList.ElementCallbackDelegate drawElementCallback,
			ReorderableList.SelectCallbackDelegate onSelectCallback,
			ReorderableList.AddCallbackDelegate onAddCallback,
			ReorderableList.RemoveCallbackDelegate onRemoveCallback
		) where T : MonoBehaviour 
	{
		ApplyFilter(allPrefabs, ref listPrefabs, filterCtx);

		// FIXME(arcnor): We should support adding and removing scenes when filtered (path to create stuff in is in `FilterContext.ParentPath` when state is `Parent`)
		var full = filterCtx.State == FilterState.All;
		return new ReorderableList(listPrefabs, typeof(T), full, true, full, full)
		{
			drawHeaderCallback = rect => LayoutListHeader(allPrefabs, listName, filterCtx, rect),
			drawElementCallback = drawElementCallback,
			onSelectCallback = onSelectCallback,
			onAddCallback = onAddCallback,
			onRemoveCallback = onRemoveCallback,
			onCanRemoveCallback = list => Application.isPlaying == false
		};
	}

	
	#endregion
}

}
