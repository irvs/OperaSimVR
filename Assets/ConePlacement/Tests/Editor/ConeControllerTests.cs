using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OperaSimVR.ConePlacement.Tests
{
    public sealed class ConeControllerTests
    {
        GameObject field, owner, cameraObject;
        ConePlacementController controller;

        [SetUp] public void SetUp()
        {
            field = GameObject.CreatePrimitive(PrimitiveType.Cube);
            field.name = "Cone test field";
            field.transform.SetPositionAndRotation(new Vector3(2000, 5, 2000), Quaternion.Euler(0, 0, 10));
            field.transform.localScale = new Vector3(100, 1, 100);
            field.AddComponent<ConePlacementSurface>();
            owner = new GameObject("Cone test controller");
            controller = owner.AddComponent<ConePlacementController>();
            Physics.SyncTransforms();
        }

        [TearDown] public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(owner);
            UnityEngine.Object.DestroyImmediate(field);
            if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        [Test] public void PickAcceptsMarkedSlopeAndRejectsUnmarkedObjects()
        {
            Ray ray = new Ray(new Vector3(2000, 100, 2000), Vector3.down);
            Assert.That(controller.TryPickSurface(ray, out var point), Is.True);
            Assert.That(point.y, Is.GreaterThan(5));
            UnityEngine.Object.DestroyImmediate(field.GetComponent<ConePlacementSurface>());
            Assert.That(controller.TryPickSurface(ray, out _), Is.False);
        }

        [Test] public void SendRejectsUnappliedSpacingInput()
        {
            controller.AddVertex(new Vector3(1995, 5, 2000));
            controller.AddVertex(new Vector3(2005, 5, 2000));
            controller.SetNeighbors(1, new[] { 2 });
            Assert.That(controller.PlaceCones(), Is.True);
            var inputs = (System.Collections.Generic.Dictionary<string, string>)typeof(ConePlacementController)
                .GetField("spacingText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            inputs[controller.Layout.GetGroups()[0].Key] = "3";
            Assert.That(controller.SendToRos(), Is.False);
            Assert.That(controller.IsSendingToRos, Is.False);
            Assert.That(controller.RosStatus, Does.Contain("Place / Update Cones"));
        }

        [Test] public void GeneratedConesRestOnSlopeAndDirtyEditsHideStaleBoundary()
        {
            controller.AddVertex(new Vector3(1995, 5, 2000));
            controller.AddVertex(new Vector3(2005, 5, 2000));
            Assert.That(controller.SetNeighbors(1, new[] { 2 }), Is.True);
            Assert.That(controller.PlaceCones(), Is.True, controller.Status);
            Assert.That(controller.PlacedConeCount, Is.EqualTo(6));
            Assert.That(controller.PlacedPositions.Last().y - controller.PlacedPositions.First().y, Is.EqualTo(10 * Mathf.Tan(10 * Mathf.Deg2Rad)).Within(.01));
            foreach (Vector3 position in controller.PlacedPositions)
            {
                Assert.That(controller.TryProjectToSurface(position, out var ground), Is.True);
                Assert.That(Vector3.Distance(position, ground), Is.LessThan(.001));
            }
            Assert.That(controller.transform.Find("Placed Cones").GetComponentsInChildren<MeshRenderer>().Length, Is.EqualTo(6));
            Assert.That(controller.SetSpacing(controller.Layout.GetGroups()[0].Key, 1), Is.True);
            Assert.That(controller.NeedsPlacement, Is.True);
            Assert.That(controller.transform.Find("Placed Cones").gameObject.activeSelf, Is.False);
            Assert.That(controller.PlaceCones(), Is.True);
            Assert.That(controller.PlacedConeCount, Is.EqualTo(11));
        }

        [Test] public void MissingGroundFailsBeforeReplacingValidCones()
        {
            controller.AddVertex(new Vector3(2000, 5, 2000));
            Assert.That(controller.PlaceCones(), Is.True);
            controller.AddVertex(new Vector3(3000, 5, 3000));
            Assert.That(controller.PlaceCones(), Is.False);
            Assert.That(controller.NeedsPlacement, Is.True);
            Assert.That(controller.PlacedConeCount, Is.Zero);
        }

        [Test] public void SaveLoadAndInvalidLoadPreserveCurrentLayout()
        {
            string path = Path.Combine(Path.GetTempPath(), "operasimvr-cone-test-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                controller.FilePath = path;
                controller.AddVertex(new Vector3(2000, 5, 2000));
                Assert.That(controller.SaveLayout(), Is.True, controller.Status);
                string original = controller.Layout.ToJson();
                controller.AddVertex(new Vector3(2005, 5, 2000));
                Assert.That(controller.LoadLayout(), Is.True, controller.Status);
                Assert.That(controller.Layout.ToJson(), Is.EqualTo(original));
                File.WriteAllText(path, "{invalid");
                Assert.That(controller.LoadLayout(), Is.False);
                Assert.That(controller.Layout.ToJson(), Is.EqualTo(original));
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        [Test] public void ScreenSpaceVertexSelectionResolvesIds()
        {
            cameraObject = new GameObject("Cone test camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.SetPositionAndRotation(new Vector3(2000, 40, 2000), Quaternion.Euler(90, 0, 0));
            controller.AddVertex(new Vector3(2000, 5, 2000));
            Vector3 screen = camera.WorldToScreenPoint(new Vector3(2000, 5.2f, 2000));
            Assert.That(controller.PickVertex(camera, screen), Is.EqualTo(1));
            Assert.That(controller.PickVertex(camera, (Vector2)screen + new Vector2(80, 80)), Is.EqualTo(-1));
        }

        [Test] public void GroupExportUsesPlacedGroundPositionsAndInvalidatesAfterEdits()
        {
            controller.AddVertex(new Vector3(1995, 5, 2000));
            controller.AddVertex(new Vector3(2005, 5, 2000));
            controller.AddVertex(new Vector3(2010, 5, 2000));
            Assert.That(controller.SetNeighbors(1, new[] { 2 }), Is.True);
            Assert.That(controller.TryGetPlacedGroups(out _, out _), Is.False);
            Assert.That(controller.PlaceCones(), Is.True, controller.Status);
            Assert.That(controller.TryGetPlacedGroups(out var groups, out var error), Is.True, error);
            Assert.That(groups.Select(g => g.Id), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(groups[0].Positions.Count, Is.EqualTo(6));
            Assert.That(groups[1].Positions.Count, Is.EqualTo(1));
            Assert.That(groups.SelectMany(g => g.Positions), Is.EqualTo(controller.PlacedPositions));
            foreach (Vector3 position in groups.SelectMany(g => g.Positions))
            {
                Assert.That(controller.TryProjectToSurface(position, out var ground), Is.True);
                Assert.That(Vector3.Distance(position, ground), Is.LessThan(.001));
            }
            Assert.That(controller.SetSpacing("1-2", 1), Is.True);
            Assert.That(controller.TryGetPlacedGroups(out _, out _), Is.False);
            Assert.That(groups[0].Positions.Count, Is.EqualTo(6), "Previously exported snapshots stay immutable.");
            Assert.That(controller.PlaceCones(), Is.True);
            controller.ClearCones();
            Assert.That(controller.TryGetPlacedGroups(out _, out _), Is.False);
        }

        [Test] public void GroupExportRejectsManualConeMovesAndDirectLayoutEdits()
        {
            controller.AddVertex(new Vector3(2000, 5, 2000));
            Assert.That(controller.PlaceCones(), Is.True);
            controller.transform.Find("Placed Cones").GetChild(0).position += Vector3.up;
            Assert.That(controller.TryGetPlacedGroups(out _, out _), Is.False);
            Assert.That(controller.PlaceCones(), Is.True);
            controller.Layout.AddVertex(new Vector3(2005, 5, 2000));
            Assert.That(controller.TryGetPlacedGroups(out _, out _), Is.False);
        }

        [Test] public void GroupExportRejectsAPlacementThatFailedAfterGroundWasRemoved()
        {
            controller.AddVertex(new Vector3(2000, 5, 2000));
            Assert.That(controller.PlaceCones(), Is.True);
            UnityEngine.Object.DestroyImmediate(field.GetComponent<ConePlacementSurface>());
            Assert.That(controller.PlaceCones(), Is.False);
            Assert.That(controller.TryGetPlacedGroups(out _, out _), Is.False);
        }

        [Test] public void EditingCameraDoesNotInheritVrProjectionAndRestoresOriginalCamera()
        {
            Camera[] existing = Camera.allCameras;
            foreach (Camera other in existing) other.enabled = false;
            try
            {
                cameraObject = new GameObject("Cone test original camera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.transform.SetPositionAndRotation(new Vector3(2000, 40, 2000), Quaternion.identity);
                camera.projectionMatrix = Matrix4x4.Perspective(30, 1, .1f, 100);
                camera.worldToCameraMatrix = Matrix4x4.identity;
                controller.SetEditing(true);
                typeof(ConePlacementController).GetMethod("OpenEditingCamera", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
                Camera editing = controller.EditingCamera;
                Assert.That(editing, Is.Not.Null);
                Assert.That(editing.orthographic, Is.True);
                Assert.That(Vector3.Dot(editing.transform.forward, Vector3.down), Is.GreaterThan(.999f));
                Assert.That(controller.TryPickSurface(editing.ViewportPointToRay(new Vector3(.5f, .5f)), out var ground), Is.True);
                Assert.That(ground.x, Is.EqualTo(2000).Within(.01));
                Assert.That(ground.z, Is.EqualTo(2000).Within(.01));
                Assert.That(camera.enabled, Is.False);
                controller.SetEditing(false);
                Assert.That(controller.EditingCamera, Is.Null);
                Assert.That(camera.enabled, Is.True);
            }
            finally
            {
                controller.SetEditing(false);
                foreach (Camera other in existing) if (other != null) other.enabled = true;
            }
        }
    }
}
