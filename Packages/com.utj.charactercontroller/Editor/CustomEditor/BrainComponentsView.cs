using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.TinyCharacterController;
using Unity.TinyCharacterController.Core;
using Unity.TinyCharacterController.Interfaces.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyCharacterControllerEditor
{
    /// <summary>
    /// Lists the TCC components on the Brain's GameObject, grouped by role (Core / Control / Effect / Check / Other).
    /// Control rows show the configured Move / Turn priorities as editable fields,
    /// and duplicated priorities are reported as warnings.
    /// </summary>
    public class BrainComponentsView : VisualElement
    {
        /// <summary>
        /// Role of a component as seen from the Brain.
        /// </summary>
        internal enum Category
        {
            Core,
            Control,
            Effect,
            Check,
            Other
        }

        /// <summary>
        /// A component shown in the list.
        /// </summary>
        internal class Entry
        {
            public Component Component;
            public Category Category;
            public SerializedObject SerializedObject;
            public SerializedProperty MovePriority;
            public SerializedProperty TurnPriority;
        }

        private static readonly Color RowLineColor = new(0f, 0f, 0f, 0.15f);
        private static readonly Assembly TccAssembly = typeof(BrainBase).Assembly;

        // Components whose embedded Inspector is open. Kept across rebuilds of the view.
        private static readonly HashSet<Component> ExpandedComponents = new();

        private static List<Type> _addableTypes;

        private readonly BrainBase _brain;
        private readonly List<Component> _componentBuffer = new();
        private readonly List<Entry> _entries = new();
        private readonly Label _title;
        private readonly VisualElement _groupRoot;
        private readonly VisualElement _warningRoot;
        private readonly BrainRuntimeView _runtimeView;
        private string _structureSignature;
        private string _warningText;

        public BrainComponentsView(BrainBase brain)
        {
            _brain = brain;
            style.marginTop = 8;

            // Toolbar
            var toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    alignItems = Align.Center,
                    marginBottom = 4
                }
            };
            _title = new Label { style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } };
            toolbar.Add(_title);
            toolbar.Add(new Button(ShowAddMenu) { text = "Add", tooltip = "Add a TCC component" });
            toolbar.Add(new Button(() => SetComponentInspectorsExpanded(false))
            {
                text = "Collapse All",
                tooltip = "Collapse the Inspectors of the listed components (only changes how the Inspector looks)"
            });
            toolbar.Add(new Button(() => SetComponentInspectorsExpanded(true))
            {
                text = "Expand All",
                tooltip = "Expand the Inspectors of the listed components"
            });
            Add(toolbar);

            _groupRoot = new VisualElement();
            Add(_groupRoot);

            _warningRoot = new VisualElement();
            Add(_warningRoot);

            _runtimeView = new BrainRuntimeView(brain);
            Add(_runtimeView);

            Rebuild();

            // Follow component additions, removals and reordering.
            schedule.Execute(CheckStructure).Every(500);
            schedule.Execute(UpdateWarnings).Every(300);
        }

        /// <summary>
        /// Determines the role of a component. Returns null if the component is not listed.
        /// Components outside the TCC assembly are listed only when they take part in the Brain's calculation
        /// (IMove / ITurn / IEffect).
        /// </summary>
        internal static Category? Classify(Component component)
        {
            if (component == null || component is BrainBase || component is Transform)
                return null;

            if (component is CharacterSettings)
                return Category.Core;
            if (component is IMove || component is ITurn)
                return Category.Control;
            if (component is IEffect)
                return Category.Effect;

            var type = component.GetType();
            if (type.Assembly != TccAssembly)
                return null;

            var ns = type.Namespace ?? string.Empty;
            return ns.EndsWith(".Check", StringComparison.Ordinal) ? Category.Check : Category.Other;
        }

        private void CheckStructure()
        {
            if (_brain == null)
                return;

            if (BuildSignature() != _structureSignature)
                Rebuild();
        }

        private string BuildSignature()
        {
            _brain.GetComponents(_componentBuffer);
            var builder = new StringBuilder();
            foreach (var component in _componentBuffer)
            {
                // Missing scripts are null; they still count for the order.
                builder.Append(component == null ? 0 : component.GetHashCode()).Append(',');
            }

            return builder.ToString();
        }

        private void Rebuild()
        {
            _groupRoot.Clear();
            _entries.Clear();
            ExpandedComponents.RemoveWhere(c => c == null);

            if (_brain == null)
                return;

            _structureSignature = BuildSignature();

            foreach (var component in _componentBuffer)
            {
                var category = Classify(component);
                if (category == null)
                    continue;

                var entry = new Entry
                {
                    Component = component,
                    Category = category.Value,
                    SerializedObject = new SerializedObject(component)
                };
                if (entry.Category == Category.Control)
                    FindPriorityProperties(entry);
                _entries.Add(entry);
            }

            _title.text = $"TCC Components ({_entries.Count})";

            foreach (Category category in Enum.GetValues(typeof(Category)))
            {
                var items = _entries.Where(e => e.Category == category).ToList();
                if (items.Count == 0)
                    continue;
                _groupRoot.Add(CreateGroup(category, items));
            }

            _warningText = null;
            UpdateWarnings();
            _runtimeView.SetEntries(_entries);
        }

        /// <summary>
        /// Finds the serialized fields that hold the configured Move / Turn priorities.
        /// The field names differ for each component, so integer fields whose name contains "Priority" are used:
        /// a name containing "Move" or "Turn" goes to that column, and any other name goes to the column of the
        /// interface the component implements.
        /// </summary>
        private static void FindPriorityProperties(Entry entry)
        {
            var candidates = new List<SerializedProperty>();
            var iterator = entry.SerializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType == SerializedPropertyType.Integer &&
                    iterator.name.IndexOf("priority", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    candidates.Add(iterator.Copy());
                }
            }

            foreach (var property in candidates)
            {
                var name = property.name;
                if (name.IndexOf("move", StringComparison.OrdinalIgnoreCase) >= 0)
                    entry.MovePriority ??= property;
                else if (name.IndexOf("turn", StringComparison.OrdinalIgnoreCase) >= 0)
                    entry.TurnPriority ??= property;
            }

            foreach (var property in candidates)
            {
                if (property.name.IndexOf("move", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    property.name.IndexOf("turn", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                if (entry.MovePriority == null && entry.Component is IMove)
                    entry.MovePriority = property;
                else if (entry.TurnPriority == null && entry.Component is ITurn)
                    entry.TurnPriority = property;
            }
        }

        private static VisualElement CreateGroup(Category category, List<Entry> items)
        {
            var foldout = new Foldout
            {
                text = $"{category}  ({items.Count})",
                value = true,
                viewDataKey = $"tcc-brain-group-{category}",
                style = { marginTop = 2 }
            };

            if (category == Category.Control)
            {
                var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                header.Add(new VisualElement { style = { flexGrow = 1 } });
                header.Add(CreateColumnLabel("Move"));
                header.Add(CreateColumnLabel("Turn"));
                foldout.Add(header);
            }

            foreach (var entry in items)
                foldout.Add(CreateRow(entry));

            return foldout;
        }

        private static Label CreateColumnLabel(string text)
        {
            return new Label(text)
            {
                style =
                {
                    width = 48,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    opacity = 0.7f
                }
            };
        }

        private static VisualElement CreateRow(Entry entry)
        {
            var container = new VisualElement();
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    minHeight = 20,
                    borderBottomWidth = 1,
                    borderBottomColor = RowLineColor
                }
            };
            container.Add(row);

            // Enabled flag (with Undo through the binding)
            var enabledProperty = entry.SerializedObject.FindProperty("m_Enabled");
            var enabledToggle = new Toggle { style = { marginRight = 2 } };
            if (entry.Component is Behaviour && enabledProperty != null)
                enabledToggle.BindProperty(enabledProperty);
            else
                enabledToggle.style.visibility = Visibility.Hidden;
            row.Add(enabledToggle);

            // Name: toggles the embedded Inspector
            var inspectorRoot = new VisualElement
            {
                style = { marginLeft = 18, marginBottom = 4 }
            };
            var nameButton = new Button
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    backgroundColor = Color.clear,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    borderTopWidth = 0,
                    borderBottomWidth = 0
                },
                tooltip = "Show this component's Inspector here"
            };
            row.Add(nameButton);

            var typeName = entry.Component.GetType().Name;
            var external = entry.Component.GetType().Assembly != TccAssembly;
            void UpdateExpanded()
            {
                var expanded = ExpandedComponents.Contains(entry.Component);
                nameButton.text = (expanded ? "▾ " : "▸ ") + typeName + (external ? "  (custom)" : string.Empty);
                inspectorRoot.Clear();
                if (expanded)
                    inspectorRoot.Add(new InspectorElement(entry.Component));
                inspectorRoot.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            }

            nameButton.clicked += () =>
            {
                if (!ExpandedComponents.Remove(entry.Component))
                    ExpandedComponents.Add(entry.Component);
                UpdateExpanded();
            };
            UpdateExpanded();

            if (entry.Category == Category.Core)
            {
                row.Add(new Label("Required") { style = { opacity = 0.7f, marginRight = 4 } });
            }

            if (entry.Category == Category.Control)
            {
                row.Add(CreatePriorityCell(entry.MovePriority, entry.Component is IMove));
                row.Add(CreatePriorityCell(entry.TurnPriority, entry.Component is ITurn));
            }

            container.Add(inspectorRoot);
            return container;
        }

        private static VisualElement CreatePriorityCell(SerializedProperty property, bool implemented)
        {
            if (property == null)
            {
                return new Label(implemented ? "?" : "—")
                {
                    tooltip = implemented
                        ? "No serialized priority field was found for this component"
                        : "This component does not implement this interface",
                    style = { width = 48, unityTextAlign = TextAnchor.MiddleCenter, opacity = 0.5f }
                };
            }

            var field = new IntegerField { style = { width = 48, marginLeft = 0, marginRight = 0 } };
            field.tooltip = property.displayName;
            field.BindProperty(property);
            return field;
        }

        /// <summary>
        /// Reports configured priorities that are shared by several components.
        /// When such components are active at the same time, the one listed first on the GameObject wins.
        /// </summary>
        private void UpdateWarnings()
        {
            var builder = new StringBuilder();
            AppendDuplicates(builder, "Move", e => e.MovePriority);
            AppendDuplicates(builder, "Turn", e => e.TurnPriority);

            var text = builder.ToString();
            if (text == _warningText)
                return;

            _warningText = text;
            _warningRoot.Clear();
            if (text.Length > 0)
                _warningRoot.Add(new HelpBox(text.TrimEnd(), HelpBoxMessageType.Warning));
        }

        private void AppendDuplicates(StringBuilder builder, string label, Func<Entry, SerializedProperty> selector)
        {
            var values = new List<(int Value, string Name)>();
            foreach (var entry in _entries)
            {
                var property = selector(entry);
                if (property == null || entry.Component == null)
                    continue;

                entry.SerializedObject.UpdateIfRequiredOrScript();
                values.Add((property.intValue, entry.Component.GetType().Name));
            }

            foreach (var group in values.Where(v => v.Value > 0).GroupBy(v => v.Value).Where(g => g.Count() > 1))
            {
                var names = string.Join(", ", group.Select(v => v.Name));
                builder.AppendLine(
                    $"{label} priority {group.Key} is shared by {names}. If they are active at the same time, the one listed first wins.");
            }
        }

        private void ShowAddMenu()
        {
            if (_brain == null)
                return;

            var menu = new GenericMenu();
            foreach (var type in GetAddableTypes())
            {
                var ns = type.Namespace ?? string.Empty;
                var group = ns.Substring(ns.LastIndexOf('.') + 1);
                var content = new GUIContent($"{group}/{type.Name}");
                var exists = _brain.GetComponent(type) != null;
                if (exists && type.IsDefined(typeof(DisallowMultipleComponent), true))
                {
                    menu.AddDisabledItem(content);
                    continue;
                }

                var targetType = type;
                menu.AddItem(content, false, () =>
                {
                    if (_brain == null)
                        return;
                    Undo.AddComponent(_brain.gameObject, targetType);
                    CheckStructure();
                });
            }

            menu.ShowAsContext();
        }

        private static List<Type> GetAddableTypes()
        {
            if (_addableTypes != null)
                return _addableTypes;

            string[] namespaces =
            {
                "Unity.TinyCharacterController.Control",
                "Unity.TinyCharacterController.Effect",
                "Unity.TinyCharacterController.Check"
            };

            _addableTypes = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                .Where(t => t.Assembly == TccAssembly && !t.IsAbstract && !t.IsGenericType &&
                            namespaces.Contains(t.Namespace) &&
                            !t.IsDefined(typeof(ObsoleteAttribute), false))
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToList();
            return _addableTypes;
        }

        /// <summary>
        /// Collapses or expands the regular Inspectors of the listed components.
        /// The state is stored per user by the editor and is not saved in the scene.
        /// </summary>
        private void SetComponentInspectorsExpanded(bool expanded)
        {
            foreach (var entry in _entries)
            {
                if (entry.Component != null)
                    InternalEditorUtility.SetIsInspectorExpanded(entry.Component, expanded);
            }

            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }
    }
}
