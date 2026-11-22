using System.Reflection;
using NUnit.Framework;
using Unity.TinyCharacterController.Control;
using Unity.TinyCharacterController.Utility;
using UnityEngine;

namespace Unity.TinyCharacterController.Test
{
    public class MoveInputRelayTest
    {
        private static readonly FieldInfo InputValueField =
            typeof(MoveControl).GetField("_inputValue", BindingFlags.NonPublic | BindingFlags.Instance);

        private GameObject _gameObject;
        private MoveControl _moveControl;
        private MoveInputRelay _relay;

        /// <summary>
        /// The input that MoveControl currently holds.
        /// </summary>
        private Vector2 ControlInput => (Vector2)InputValueField.GetValue(_moveControl);

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Player");
            _moveControl = _gameObject.AddComponent<MoveControl>();
            _relay = _gameObject.AddComponent<MoveInputRelay>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void MoveControlの入力値を参照できる()
        {
            Assert.That(InputValueField, Is.Not.Null);
        }

        [Test]
        public void MoveControlを設定しなくても同じGameObjectから取得する()
        {
            Assert.That(_relay.MoveControl, Is.SameAs(_moveControl));
        }

        [Test]
        public void SetInputの値がMoveControlに渡される()
        {
            _relay.SetInput(new Vector2(0.3f, -0.8f));

            Assert.That(_relay.RawInput, Is.EqualTo(new Vector2(0.3f, -0.8f)));
            Assert.That(_relay.AppliedInput, Is.EqualTo(new Vector2(0.3f, -0.8f)));
            Assert.That(ControlInput, Is.EqualTo(new Vector2(0.3f, -0.8f)));
        }

        [Test]
        public void 無効にするとMoveControlにゼロを渡す()
        {
            _relay.SetInput(Vector2.up);

            _relay.enabled = false;

            Assert.That(ControlInput, Is.EqualTo(Vector2.zero));
            Assert.That(_relay.AppliedInput, Is.EqualTo(Vector2.zero));
            Assert.That(_relay.RawInput, Is.EqualTo(Vector2.up));
        }

        [Test]
        public void 無効の間の入力は保持され有効にしたときに渡される()
        {
            _relay.enabled = false;

            _relay.SetInput(Vector2.right);
            Assert.That(ControlInput, Is.EqualTo(Vector2.zero));

            _relay.enabled = true;
            Assert.That(ControlInput, Is.EqualTo(Vector2.right));
        }

        [Test]
        public void HorizontalOnlyではYを0にする()
        {
            _relay.Axis = MoveInputRelay.AxisConstraint.HorizontalOnly;

            _relay.SetInput(new Vector2(0.5f, 0.7f));

            Assert.That(ControlInput, Is.EqualTo(new Vector2(0.5f, 0)));
        }

        [Test]
        public void VerticalOnlyではXを0にする()
        {
            _relay.Axis = MoveInputRelay.AxisConstraint.VerticalOnly;

            _relay.SetInput(new Vector2(0.5f, 0.7f));

            Assert.That(ControlInput, Is.EqualTo(new Vector2(0, 0.7f)));
        }

        [Test]
        public void 軸の制限を変えると現在の入力に反映される()
        {
            _relay.SetInput(new Vector2(0.5f, 0.7f));

            _relay.Axis = MoveInputRelay.AxisConstraint.HorizontalOnly;

            Assert.That(ControlInput, Is.EqualTo(new Vector2(0.5f, 0)));
        }

        [Test]
        public void 閾値より小さい入力はゼロとして扱う()
        {
            _relay.InputThreshold = 0.2f;

            _relay.SetInput(new Vector2(0.1f, 0.1f));
            Assert.That(ControlInput, Is.EqualTo(Vector2.zero));

            _relay.SetInput(new Vector2(0.3f, 0));
            Assert.That(ControlInput, Is.EqualTo(new Vector2(0.3f, 0)));
        }

        [Test]
        public void 閾値は軸の制限のあとに判定する()
        {
            _relay.InputThreshold = 0.2f;
            _relay.Axis = MoveInputRelay.AxisConstraint.HorizontalOnly;

            // The magnitude before the constraint is above the threshold, but the X component alone is below it.
            _relay.SetInput(new Vector2(0.1f, 0.9f));

            Assert.That(ControlInput, Is.EqualTo(Vector2.zero));
        }
    }
}
