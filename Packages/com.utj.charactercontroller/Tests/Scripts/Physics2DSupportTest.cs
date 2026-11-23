using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.TinyCharacterController.Brain;
using Unity.TinyCharacterController.Check;
using Unity.TinyCharacterController.Control;
using Unity.TinyCharacterController.Effect;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.TinyCharacterController.Test
{
    /// <summary>
    /// Tests for CharacterBrain2D, GroundCheck2D and HeadContactCheck2D on a stage built with BoxCollider2D.
    /// The stage is built in code: the ground's top surface is at y = 0.
    /// </summary>
    public class Physics2DSupportTest
    {
        private const float Tolerance = 0.05f;

        private static readonly FieldInfo EnvironmentLayerField =
            typeof(CharacterSettings).GetField("_environmentLayer", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly List<GameObject> _objects = new();

        [SetUp]
        public void SetUp()
        {
            var camera = new GameObject("Main Camera") { tag = "MainCamera" };
            camera.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            _objects.Add(camera);

            CreateBox("Ground", new Vector2(0, -0.5f), new Vector2(40, 1));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objects)
                Object.Destroy(obj);
            _objects.Clear();
        }

        [Test]
        public void 環境レイヤーのフィールドを参照できる()
        {
            Assert.That(EnvironmentLayerField, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator 落下してBoxCollider2Dの地面に着地する()
        {
            var player = CreatePlayer(new Vector3(0, 2, 0));
            var groundCheck = player.GetComponent<GroundCheck2D>();

            yield return new WaitForSeconds(1.5f);

            Assert.That(player.transform.position.y, Is.InRange(0f, Tolerance));
            Assert.That(groundCheck.IsOnGround, Is.True);
            Assert.That(groundCheck.IsFirmlyOnGround, Is.True);
            Assert.That(groundCheck.GroundObject.name, Is.EqualTo("Ground"));
        }

        [UnityTest]
        public IEnumerator 着地した後は地面に留まる()
        {
            var player = CreatePlayer(new Vector3(0, 0.5f, 0));

            yield return new WaitForSeconds(1.0f);
            var landed = player.transform.position.y;
            yield return new WaitForSeconds(1.0f);

            Assert.That(landed, Is.InRange(0f, Tolerance));
            Assert.That(player.transform.position.y, Is.EqualTo(landed).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator 壁で止まる()
        {
            // The wall's left surface is at x = 2.
            CreateBox("Wall", new Vector2(2.5f, 2.5f), new Vector2(1, 5));
            var player = CreatePlayer(new Vector3(0, 0.05f, 0));
            var move = player.GetComponent<MoveControl>();
            var radius = player.GetComponent<CharacterSettings>().Radius;

            yield return new WaitForSeconds(0.3f);
            move.Move(Vector2.right);
            yield return new WaitForSeconds(2.0f);

            var x = player.transform.position.x;
            Assert.That(x, Is.LessThanOrEqualTo(2 - radius + 0.001f));
            Assert.That(x, Is.GreaterThan(2 - radius - Tolerance));
            Assert.That(player.GetComponent<GroundCheck2D>().IsOnGround, Is.True);
        }

        [UnityTest]
        public IEnumerator ジャンプして天井で止まる()
        {
            // The ceiling's bottom surface is at y = 2.5.
            CreateBox("Ceiling", new Vector2(0, 3f), new Vector2(4, 1));
            var player = CreatePlayer(new Vector3(0, 0.05f, 0));
            var jump = player.GetComponent<JumpControl>();
            var height = player.GetComponent<CharacterSettings>().Height;
            jump.JumpHeight = 4;

            yield return new WaitForSeconds(0.3f);
            jump.ForceJump();

            var maxY = player.transform.position.y;
            var touchedCeiling = false;
            for (var time = 0f; time < 1.5f; time += Time.deltaTime)
            {
                yield return null;
                maxY = Mathf.Max(maxY, player.transform.position.y);
                touchedCeiling |= player.GetComponent<HeadContactCheck2D>().IsHeadContact;
            }

            Assert.That(maxY, Is.LessThanOrEqualTo(2.5f - height + 0.001f));
            Assert.That(maxY, Is.GreaterThan(2.5f - height - 0.2f));
            Assert.That(touchedCeiling, Is.True);
            Assert.That(player.transform.position.y, Is.InRange(0f, Tolerance));
        }

        [UnityTest]
        public IEnumerator 向きが左右に切り替わる()
        {
            var player = CreatePlayer(new Vector3(0, 0.05f, 0));
            var brain = player.GetComponent<CharacterBrain2D>();
            var move = player.GetComponent<MoveControl>();

            yield return new WaitForSeconds(0.2f);
            // MoveControl requests the direction after the speed exceeds the stop threshold.
            move.Move(Vector2.left);
            yield return new WaitForSeconds(0.2f);

            Assert.That(brain.IsFacingRight, Is.False);
            Assert.That(Quaternion.Angle(player.transform.rotation, Quaternion.Euler(0, 180, 0)), Is.LessThan(0.01f));

            move.Move(Vector2.right);
            yield return new WaitForSeconds(0.2f);

            Assert.That(brain.IsFacingRight, Is.True);
            Assert.That(Quaternion.Angle(player.transform.rotation, Quaternion.identity), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator Z座標は変わらない()
        {
            var player = CreatePlayer(new Vector3(0, 1, 0.5f));
            var move = player.GetComponent<MoveControl>();

            yield return new WaitForSeconds(0.5f);
            move.Move(new Vector2(1, 1));
            yield return new WaitForSeconds(0.5f);

            Assert.That(player.transform.position.z, Is.EqualTo(0.5f));
        }

        private void CreateBox(string name, Vector2 center, Vector2 size)
        {
            var box = new GameObject(name);
            box.transform.position = center;
            box.AddComponent<BoxCollider2D>().size = size;
            _objects.Add(box);
        }

        private GameObject CreatePlayer(Vector3 position)
        {
            // Add all components before activating, so that the brain collects them in Awake.
            var player = new GameObject("Player");
            player.SetActive(false);
            player.transform.position = position;

            var settings = player.AddComponent<CharacterSettings>();
            EnvironmentLayerField.SetValue(settings, (LayerMask)(1 << LayerMask.NameToLayer("Default")));
            player.AddComponent<CharacterBrain2D>();
            player.AddComponent<GroundCheck2D>();
            player.AddComponent<HeadContactCheck2D>();
            player.AddComponent<Gravity>();
            player.AddComponent<MoveControl>();
            player.AddComponent<JumpControl>();

            _objects.Add(player);
            player.SetActive(true);
            return player;
        }
    }
}
