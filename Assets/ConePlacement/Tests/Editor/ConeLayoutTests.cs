using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OperaSimVR.ConePlacement.Tests
{
    public sealed class ConeLayoutTests
    {
        [Test] public void SharedConesKeepBothGroupMembershipsWithoutDuplicatingPhysicalCones()
        {
            var layout = new ConeLayout();
            layout.AddVertex(new Vector3(0, 0, 0));
            layout.AddVertex(new Vector3(4, 0, 0));
            layout.AddVertex(new Vector3(2, 0, 0));
            layout.AddVertex(new Vector3(6, 0, 0));
            Assert.That(layout.SetNeighbors(1, new[] { 2 }, out _), Is.True);
            Assert.That(layout.SetNeighbors(3, new[] { 4 }, out _), Is.True);
            Assert.That(layout.TryGeneratePositionGroups(100, out var groups, out var error), Is.True, error);
            Assert.That(groups.Select(g => g.Id), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(groups.Select(g => g.Key), Is.EqualTo(new[] { "1-2", "3-4" }));
            Assert.That(groups[0].Positions, Is.EqualTo(new[] { Vector3.zero, Vector3.right * 2, Vector3.right * 4 }));
            Assert.That(groups[1].Positions, Is.EqualTo(new[] { Vector3.right * 2, Vector3.right * 4, Vector3.right * 6 }));
            Assert.That(layout.TryGeneratePositions(100, out var physical, out error), Is.True, error);
            Assert.That(physical.Count, Is.EqualTo(4));
        }

        static int Point(ConeLayout layout, float x, float z = 0) => layout.AddVertex(new Vector3(x, 0, z));
        static void Link(ConeLayout layout, int id, params int[] others)
        { Assert.That(layout.SetNeighbors(id, others, out string error), Is.True, error); }
        static void Spacing(ConeLayout layout, int index, float value)
        { Assert.That(layout.SetGroupSpacing(layout.GetGroups()[index].Key, value, out string error), Is.True, error); }

        [Test] public void IsolatedVerticesHaveMonotonicIdsAndGroups()
        {
            var layout = new ConeLayout();
            int a = Point(layout, 0), b = Point(layout, 2);
            Assert.That(layout.GetGroups().Count, Is.EqualTo(2));
            Assert.That(layout.RemoveVertex(b, out _), Is.True);
            Assert.That(Point(layout, 3), Is.EqualTo(b + 1));
            Assert.That(layout.FindVertex(a).Neighbors.Count, Is.Zero);
        }

        [Test] public void OpenPathIsReciprocalAndOrderedFromSmallestEndpoint()
        {
            var layout = new ConeLayout();
            int middle = Point(layout, 5), end = Point(layout, 10), start = Point(layout, 0);
            Link(layout, middle, end, start);
            ConeGroup group = layout.GetGroups().Single();
            Assert.That(group.IsClosed, Is.False);
            Assert.That(group.OrderedVertexIds, Is.EqualTo(new[] { end, middle, start }));
            Assert.That(layout.FindVertex(end).Neighbors, Is.EqualTo(new[] { middle }));
        }

        [Test] public void ClosedRingAndSeparateInnerRingAreIndependentGroups()
        {
            var layout = new ConeLayout();
            for (int i = 0; i < 2; i++)
            {
                int a = Point(layout, i), b = Point(layout, i + 3), c = Point(layout, i + 2, 4);
                Link(layout, a, b, c); Link(layout, b, a, c);
            }
            Assert.That(layout.GetGroups().Count, Is.EqualTo(2));
            Assert.That(layout.GetGroups().All(g => g.IsClosed), Is.True);
            Spacing(layout, 0, 1); Spacing(layout, 1, 3);
            Assert.That(layout.GetGroups().Select(g => g.Spacing), Is.EqualTo(new[] { 1f, 3f }));
        }

        [TestCase(new[] { 1 }, "self")]
        [TestCase(new[] { 999 }, "missing")]
        [TestCase(new[] { 2, 2 }, "duplicate")]
        [TestCase(new[] { 2, 3, 4 }, "three")]
        public void InvalidNeighborEditsDoNotMutateGraph(int[] proposed, string reason)
        {
            var layout = new ConeLayout();
            for (int i = 0; i < 4; i++) Point(layout, i);
            Link(layout, 1, 2);
            string before = layout.ToJson();
            Assert.That(layout.SetNeighbors(1, proposed, out _), Is.False, reason);
            Assert.That(layout.ToJson(), Is.EqualTo(before));
        }

        [Test] public void TargetDegreeLimitIsTransactional()
        {
            var layout = new ConeLayout();
            for (int i = 0; i < 5; i++) Point(layout, i);
            Link(layout, 1, 2, 3); Link(layout, 4, 5);
            string before = layout.ToJson();
            Assert.That(layout.SetNeighbors(4, new[] { 1 }, out _), Is.False);
            Assert.That(layout.ToJson(), Is.EqualTo(before));
        }

        [Test] public void SplitAndMergeInheritSpacingAndRemoveReciprocalEdges()
        {
            var layout = new ConeLayout();
            for (int i = 0; i < 4; i++) Point(layout, i);
            Link(layout, 2, 1, 3); Link(layout, 3, 2, 4); Spacing(layout, 0, 1.3f);
            Link(layout, 2, 1);
            Assert.That(layout.GetGroups().Select(g => g.Spacing), Is.EqualTo(new[] { 1.3f, 1.3f }));
            Spacing(layout, 1, 4);
            Link(layout, 2, 1, 3);
            Assert.That(layout.GetGroups().Single().Spacing, Is.EqualTo(1.3f));
            Assert.That(layout.RemoveVertex(2, out _), Is.True);
            Assert.That(layout.FindVertex(1).Neighbors, Is.Empty);
            Assert.That(layout.FindVertex(3).Neighbors, Is.EqualTo(new[] { 4 }));
        }

        [TestCase(10, 3, new[] { 0f, 3, 6, 9, 10 })]
        [TestCase(10, 2, new[] { 0f, 2, 4, 6, 8, 10 })]
        [TestCase(1, 5, new[] { 0f, 1 })]
        public void EdgeSpacingKeepsEndpoints(float length, float spacing, float[] expected)
        {
            var layout = new ConeLayout(); Point(layout, 0); Point(layout, length); Link(layout, 1, 2); Spacing(layout, 0, spacing);
            Assert.That(layout.TryGeneratePositions(100, out var positions, out var error), Is.True, error);
            Assert.That(positions.Select(p => p.x), Is.EqualTo(expected).Within(.0001));
        }

        [Test] public void RingClosureAndCornersDoNotDuplicateCones()
        {
            var layout = new ConeLayout();
            Point(layout, 0); Point(layout, 4); Point(layout, 4, 4); Point(layout, 0, 4);
            Link(layout, 1, 2, 4); Link(layout, 2, 1, 3); Link(layout, 3, 2, 4);
            Assert.That(layout.TryGeneratePositions(8, out var positions, out _), Is.True);
            Assert.That(positions.Count, Is.EqualTo(8));
            Assert.That(positions.Distinct().Count(), Is.EqualTo(8));
        }

        [Test] public void PlacementLimitFailsWithoutPartialOutput()
        {
            var layout = new ConeLayout(); Point(layout, 0); Point(layout, 1000000); Link(layout, 1, 2); Spacing(layout, 0, .1f);
            Assert.That(layout.TryGeneratePositions(10000, out var positions, out _), Is.False);
            Assert.That(positions, Is.Empty);
        }

        [Test] public void CoincidentVerticesDoNotDuplicateConesOrDivideByZero()
        {
            var layout = new ConeLayout(); Point(layout, 0); Point(layout, 0); Link(layout, 1, 2);
            Assert.That(layout.TryGeneratePositions(1, out var positions, out _), Is.True);
            Assert.That(positions.Count, Is.EqualTo(1));
        }

        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(0f)] [TestCase(-1f)] [TestCase(.01f)]
        public void InvalidSpacingIsRejected(float spacing)
        {
            var layout = new ConeLayout(); Point(layout, 0);
            Assert.That(layout.SetGroupSpacing(layout.GetGroups()[0].Key, spacing, out _), Is.False);
            Assert.That(layout.GetGroups()[0].Spacing, Is.EqualTo(ConeLayout.DefaultSpacing));
        }

        [Test] public void NonFiniteCoordinatesAreRejected()
        {
            var layout = new ConeLayout();
            Assert.Throws<ArgumentException>(() => layout.AddVertex(new Vector3(float.NaN, 0, 0)));
            Point(layout, 0);
            Assert.That(layout.MoveVertex(1, new Vector3(0, float.PositiveInfinity, 0), out _), Is.False);
        }

        [Test] public void JsonRoundTripPreservesTopologySpacingAndNextId()
        {
            var layout = new ConeLayout(); Point(layout, 0); Point(layout, 5); Link(layout, 1, 2); Spacing(layout, 0, 1.5f);
            Point(layout, 8); layout.RemoveVertex(3, out _);
            Assert.That(ConeLayout.TryFromJson(layout.ToJson(), out var restored, out var error), Is.True, error);
            Assert.That(restored.ToJson(), Is.EqualTo(layout.ToJson()));
            Assert.That(Point(restored, 9), Is.EqualTo(4));
            Assert.That(ConeLayout.TryFromJson(layout.ToJson().Replace("\"nextId\": 4", "\"nextId\": 1"), out restored, out _), Is.True);
            Assert.That(Point(restored, 9), Is.EqualTo(3));
        }

        [TestCase("")] [TestCase("{}")] [TestCase("not json")]
        [TestCase("{\"version\":99,\"vertices\":[]}")]
        [TestCase("{\"version\":1,\"vertices\":[{\"id\":1,\"position\":{},\"neighbors\":[2]}],\"settings\":[]}")]
        [TestCase("{\"version\":1,\"vertices\":[{\"id\":1},{\"id\":1}],\"settings\":[]}")]
        public void InvalidJsonIsRejected(string json)
        { Assert.That(ConeLayout.TryFromJson(json, out var result, out _), Is.False); Assert.That(result, Is.Null); }
    }
}
