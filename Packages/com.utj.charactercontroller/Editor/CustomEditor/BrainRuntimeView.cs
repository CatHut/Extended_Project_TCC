using System;
using System.Collections.Generic;
using Unity.TinyCharacterController.Core;
using Unity.TinyCharacterController.Interfaces.Components;
using Unity.TinyCharacterController.Interfaces.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyCharacterControllerEditor
{
    /// <summary>
    /// Runtime information of a Brain, shown only while playing:
    /// the current Move / Turn, the effective priority of each candidate, the velocity breakdown,
    /// the state of ground / overhead checks, and a log of priority changes.
    /// </summary>
    public class BrainRuntimeView : VisualElement
    {
        private const int LogCapacity = 8;
        private const int UpdateIntervalMs = 100;

        private static readonly Color HighlightColor = new(0.24f, 0.48f, 0.75f, 0.35f);
        private static readonly Color BarBackgroundColor = new(0f, 0f, 0f, 0.25f);
        private static readonly Color BarColor = new(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color BarHighlightColor = new(0.42f, 0.66f, 0.93f, 1f);

        /// <summary>
        /// A row that shows one value which is updated every frame.
        /// </summary>
        private class Row
        {
            public VisualElement Root;
            public Label Value;
            public VisualElement Bar;
            public Func<int> Priority;
            public object Source;
        }

        private readonly BrainBase _brain;
        private readonly Label _currentMove;
        private readonly Label _currentTurn;
        private readonly VisualElement _moveRoot;
        private readonly VisualElement _turnRoot;
        private readonly VisualElement _velocityRoot;
        private readonly VisualElement _checkRoot;
        private readonly VisualElement _logRoot;

        private readonly List<Row> _moveRows = new();
        private readonly List<Row> _turnRows = new();
        private readonly List<(Label Label, Func<string> Getter)> _valueRows = new();
        private readonly List<string> _log = new();
        private IMove _lastMove;
        private ITurn _lastTurn;
        private bool _hasLastState;

        public BrainRuntimeView(BrainBase brain)
        {
            _brain = brain;
            style.marginTop = 8;

            var header = new Label("Runtime")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 }
            };
            Add(header);

            var current = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            _currentMove = CreateCurrentLabel(current, "Current Move");
            _currentTurn = CreateCurrentLabel(current, "Current Turn");
            Add(current);

            _moveRoot = CreateSection("Move candidates (effective priority)");
            _turnRoot = CreateSection("Turn candidates (effective priority)");
            _velocityRoot = CreateSection("Velocity");
            _checkRoot = CreateSection("Check");
            _logRoot = CreateSection("Priority changes");

            schedule.Execute(UpdateView).Every(UpdateIntervalMs);
            UpdateView();
        }

        /// <summary>
        /// Rebuilds the rows from the listed components.
        /// </summary>
        internal void SetEntries(List<BrainComponentsView.Entry> entries)
        {
            _moveRoot.Clear();
            _turnRoot.Clear();
            _velocityRoot.Clear();
            _checkRoot.Clear();
            _moveRows.Clear();
            _turnRows.Clear();
            _valueRows.Clear();

            foreach (var entry in entries)
            {
                var component = entry.Component;
                var name = component.GetType().Name;

                if (component is IMove move)
                    _moveRows.Add(CreatePriorityRow(_moveRoot, name, move, () => move.Priority));
                if (component is ITurn turn)
                    _turnRows.Add(CreatePriorityRow(_turnRoot, name, turn, () => turn.Priority));
            }

            // Velocity breakdown
            AddValueRow(_velocityRoot, "Control", () => FormatVelocity(_brain.ControlVelocity), true);
            AddValueRow(_velocityRoot, "Effect", () => FormatVelocity(_brain.EffectVelocity), true);
            foreach (var entry in entries)
            {
                if (entry.Component is IEffect effect)
                {
                    var component = entry.Component;
                    AddValueRow(_velocityRoot, "    " + component.GetType().Name,
                        () => component == null ? "-" : FormatVelocity(effect.Velocity), false);
                }
            }
            AddValueRow(_velocityRoot, "Total", () => FormatVelocity(_brain.TotalVelocity), true);

            // Check states
            foreach (var entry in entries)
            {
                var component = entry.Component;
                var name = component.GetType().Name;
                if (component is IGroundContact ground)
                {
                    AddValueRow(_checkRoot, name,
                        () => component == null
                            ? "-"
                            : $"IsOnGround: {ground.IsOnGround}  Distance: {ground.DistanceFromGround:F2}", false);
                }

                if (component is IOverheadDetection overhead)
                {
                    AddValueRow(_checkRoot, name,
                        () => component == null
                            ? "-"
                            : $"IsHeadContact: {overhead.IsHeadContact}  IsObjectOverhead: {overhead.IsObjectOverhead}",
                        false);
                }
            }

            _checkRoot.parent.style.display = _checkRoot.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateView();
        }

        private static Label CreateCurrentLabel(VisualElement parent, string title)
        {
            var box = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexBasis = 0,
                    marginRight = 4,
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4,
                    backgroundColor = BarBackgroundColor,
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3
                }
            };
            box.Add(new Label(title) { style = { opacity = 0.7f } });
            var value = new Label("-") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            box.Add(value);
            parent.Add(box);
            return value;
        }

        private VisualElement CreateSection(string title)
        {
            var section = new VisualElement { style = { marginTop = 6 } };
            section.Add(new Label(title) { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            var body = new VisualElement();
            section.Add(body);
            Add(section);
            return body;
        }

        private static Row CreatePriorityRow(VisualElement parent, string name, object source, Func<int> priority)
        {
            var root = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    minHeight = 18,
                    paddingLeft = 4,
                    paddingRight = 4,
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };
            root.Add(new Label(name) { style = { flexGrow = 1, flexShrink = 1 } });

            var barBackground = new VisualElement
            {
                style = { width = 80, height = 6, backgroundColor = BarBackgroundColor, marginRight = 6 }
            };
            var bar = new VisualElement { style = { height = 6, backgroundColor = BarColor } };
            barBackground.Add(bar);
            root.Add(barBackground);

            var value = new Label("0") { style = { width = 28, unityTextAlign = TextAnchor.MiddleRight } };
            root.Add(value);
            parent.Add(root);

            return new Row { Root = root, Value = value, Bar = bar, Priority = priority, Source = source };
        }

        private void AddValueRow(VisualElement parent, string title, Func<string> getter, bool bold)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, minHeight = 16 } };
            var titleLabel = new Label(title) { style = { flexGrow = 1, whiteSpace = WhiteSpace.Pre } };
            var value = new Label("-");
            if (bold)
            {
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                value.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                titleLabel.style.opacity = 0.8f;
            }

            row.Add(titleLabel);
            row.Add(value);
            parent.Add(row);
            _valueRows.Add((value, getter));
        }

        private void UpdateView()
        {
            var playing = EditorApplication.isPlaying && _brain != null;
            style.display = playing ? DisplayStyle.Flex : DisplayStyle.None;
            if (!playing)
            {
                _hasLastState = false;
                return;
            }

            IBrain brain = _brain;
            var currentMove = brain.CurrentMove;
            var currentTurn = brain.CurrentTurn;

            _currentMove.text = NameOf(currentMove);
            _currentTurn.text = NameOf(currentTurn);

            UpdatePriorityRows(_moveRows, currentMove);
            UpdatePriorityRows(_turnRows, currentTurn);

            foreach (var (label, getter) in _valueRows)
                label.text = getter();

            UpdateLog(currentMove, currentTurn);
        }

        private static void UpdatePriorityRows(List<Row> rows, object current)
        {
            var max = 10;
            foreach (var row in rows)
                max = Mathf.Max(max, SafePriority(row));

            foreach (var row in rows)
            {
                var priority = SafePriority(row);
                var isCurrent = current != null && ReferenceEquals(row.Source, current);
                row.Value.text = priority.ToString();
                row.Root.style.backgroundColor = isCurrent ? HighlightColor : Color.clear;
                row.Root.style.opacity = priority > 0 ? 1f : 0.5f;
                row.Bar.style.width = Length.Percent(Mathf.Clamp01((float)priority / max) * 100f);
                row.Bar.style.backgroundColor = isCurrent ? BarHighlightColor : BarColor;
            }
        }

        private static int SafePriority(Row row)
        {
            // A destroyed component throws when it is accessed through the interface.
            return row.Source is UnityEngine.Object obj && obj == null ? 0 : row.Priority();
        }

        /// <summary>
        /// Records changes of the highest-priority Move / Turn.
        /// The view is polled, so a change that is undone within one update interval may be missed.
        /// </summary>
        private void UpdateLog(IMove currentMove, ITurn currentTurn)
        {
            if (_hasLastState)
            {
                if (!ReferenceEquals(currentMove, _lastMove))
                    AddLog($"{Time.time,7:F2}  Move  {NameOf(_lastMove)} → {NameOf(currentMove)}");
                if (!ReferenceEquals(currentTurn, _lastTurn))
                    AddLog($"{Time.time,7:F2}  Turn  {NameOf(_lastTurn)} → {NameOf(currentTurn)}");
            }

            _lastMove = currentMove;
            _lastTurn = currentTurn;
            _hasLastState = true;
        }

        private void AddLog(string text)
        {
            _log.Insert(0, text);
            if (_log.Count > LogCapacity)
                _log.RemoveAt(_log.Count - 1);

            _logRoot.Clear();
            for (var i = 0; i < _log.Count; i++)
            {
                _logRoot.Add(new Label(_log[i]) { style = { opacity = i == 0 ? 1f : 0.7f } });
            }
        }

        private static string NameOf(object component)
        {
            if (component == null)
                return "(none)";
            if (component is UnityEngine.Object obj && obj == null)
                return "(destroyed)";
            return component.GetType().Name;
        }

        private static string FormatVelocity(Vector3 velocity)
        {
            return $"{velocity.magnitude,6:F2}  {velocity.ToString("F2")}";
        }
    }
}
