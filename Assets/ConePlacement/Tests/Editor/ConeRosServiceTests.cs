using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OperaSimVR.ConePlacement.Ros;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using UnityEngine;

namespace OperaSimVR.ConePlacement.Tests
{
    public sealed class ConeRosServiceTests
    {
        [Serializable] sealed class ReplyEnvelope { public long srv_id; }
        static SetConeLayoutRequest Request(int count)
        {
            var layout = new ConeLayout();
            for (int i = 0; i < count; i++) layout.AddVertex(new Vector3(i + 1, 2, 3));
            Assert.That(layout.TryGeneratePositionGroups(100, out var groups, out var error), Is.True, error);
            Assert.That(ConeRosRequestBuilder.TryBuild(groups.AsEnumerable().Reverse().ToArray(), "unity_world_flu",
                Vector3.zero, Quaternion.identity, out var result, out error), Is.True, error);
            return result;
        }

        [TestCase(1)] [TestCase(3)] [TestCase(20)]
        public void PacksSortedGroupsIntoExactlyTwentySlotsAndSerializesFixedArray(int count)
        {
            var request = Request(count);
            Assert.That(request.groups.Length, Is.EqualTo(20));
            Assert.That(request.group_count, Is.EqualTo(count));
            for (int i = 0; i < 20; i++)
            {
                Assert.That(request.groups[i].group_id, Is.EqualTo(i < count ? i + 1 : 0));
                Assert.That(request.groups[i].positions.Length, Is.EqualTo(i < count ? 1 : 0));
            }
            byte[] bytes = Serialize(request);
            Assert.That(bytes.Take(4), Is.EqualTo(new byte[] { 0, 1, 0, 0 }));
            var reader = new MessageDeserializer(); reader.InitWithBuffer(bytes);
            var decoded = SetConeLayoutRequest.Deserialize(reader);
            Assert.That(decoded.groups[0].positions[0].x, Is.EqualTo(3));
            Assert.That(decoded.groups[0].positions[0].y, Is.EqualTo(-1));
            Assert.That(decoded.groups[0].positions[0].z, Is.EqualTo(2));
            Directory.CreateDirectory("Logs/cone-ros-service");
            File.WriteAllBytes("Logs/cone-ros-service/wire-" + count + ".cdr", bytes);
        }

        [Test] public void SparseGroupIdsArePackedWithoutEmptyHoles()
        {
            var layout = new ConeLayout();
            for (int i = 0; i < 6; i++) layout.AddVertex(new Vector3(i, 0, 0));
            foreach (int id in new[] { 2, 3, 4, 5 }) Assert.That(layout.RemoveVertex(id, out _), Is.True);
            layout.TryGeneratePositionGroups(100, out var groups, out _);
            Assert.That(ConeRosRequestBuilder.TryBuild(groups, "world", Vector3.zero, Quaternion.identity,
                out var request, out var error), Is.True, error);
            Assert.That(request.groups.Take(3).Select(g => g.group_id), Is.EqualTo(new uint[] { 1, 6, 0 }));
        }

        [Test] public void RejectsTwentyOneGroupsWithoutTruncation()
        {
            var layout = new ConeLayout();
            for (int i = 0; i < 21; i++) layout.AddVertex(new Vector3(i, 0, 0));
            layout.TryGeneratePositionGroups(100, out var groups, out _);
            Assert.That(ConeRosRequestBuilder.TryBuild(groups, "world", Vector3.zero, Quaternion.identity,
                out var request, out var error), Is.False);
            Assert.That(request, Is.Null);
            Assert.That(error, Does.Contain("20 groups"));
        }

        [Test] public void ConvertsRelativeFramePoseBeforeFluAxes()
        {
            Vector3 origin = new Vector3(100, 20, 300);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            var layout = new ConeLayout();
            layout.AddVertex(origin + rotation * new Vector3(1, 2, 3));
            layout.TryGeneratePositionGroups(100, out var groups, out _);
            Assert.That(ConeRosRequestBuilder.TryBuild(groups, "site", origin, rotation,
                out var request, out var error), Is.True, error);
            var point = request.groups[0].positions[0];
            Assert.That(point.x, Is.EqualTo(3).Within(.0001));
            Assert.That(point.y, Is.EqualTo(-1).Within(.0001));
            Assert.That(point.z, Is.EqualTo(2).Within(.0001));
        }

        [TestCase(true)] [TestCase(false)]
        public void FirstMatchingResponseEndsConnectionEvenWhenRejected(bool accepted)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var server = Task.Run(() =>
            {
                using (TcpClient peer = listener.AcceptTcpClient())
                using (NetworkStream stream = peer.GetStream())
                {
                    stream.ReadTimeout = 5000;
                    Handshake(stream);
                    var command = ReadPacket(stream);
                    Assert.That(command.Item1, Is.EqualTo("__request"));
                    long id = JsonUtility.FromJson<ReplyEnvelope>(Encoding.UTF8.GetString(command.Item2).TrimEnd('\0')).srv_id;
                    Assert.That(id, Is.GreaterThan(0));
                    var packet = ReadPacket(stream);
                    Assert.That(packet.Item1, Is.EqualTo("/cone_placement/set_layout"));
                    var response = new SetConeLayoutResponse { success = accepted, message = "first", group_count = 1, cone_count = 1 };
                    WritePacket(stream, "__log", Encoding.UTF8.GetBytes("{\"text\":\"before response\"}"));
                    // A response left over from the preceding connection must not finish this send.
                    WritePacket(stream, "__response", Encoding.UTF8.GetBytes("{\"srv_id\":" + (id - 1) + "}"));
                    WritePacket(stream, packet.Item1, Serialize(new SetConeLayoutResponse { success = !accepted, message = "late" }));
                    WritePacket(stream, "__response", Encoding.UTF8.GetBytes("{\"srv_id\":" + id + "}"));
                    WritePacket(stream, packet.Item1, Serialize(response), true);
                    Assert.That(stream.ReadByte(), Is.EqualTo(-1), "The client must close after one response without retrying.");
                    Directory.CreateDirectory("Logs/cone-ros-service");
                    File.WriteAllBytes("Logs/cone-ros-service/wire-response.cdr", Serialize(response));
                }
            });
            try
            {
                var result = ConeRosServiceClient.SendAsync("127.0.0.1", port, "/cone_placement/set_layout", Request(1), 5).GetAwaiter().GetResult();
                Assert.That(result.success, Is.EqualTo(accepted));
                Assert.That(result.message, Is.EqualTo("first"));
                server.GetAwaiter().GetResult();
            }
            finally { listener.Stop(); }
        }

        [Test] public void TimeoutClosesConnectionWithoutSendingAgain()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var server = Task.Run(() =>
            {
                using (TcpClient peer = listener.AcceptTcpClient())
                using (NetworkStream stream = peer.GetStream())
                {
                    stream.ReadTimeout = 5000;
                    Handshake(stream); ReadPacket(stream); ReadPacket(stream);
                    Assert.That(stream.ReadByte(), Is.EqualTo(-1), "Timeout must close, not retry.");
                }
            });
            try
            {
                Assert.Throws<TimeoutException>(() => ConeRosServiceClient.SendAsync("127.0.0.1", port,
                    "/cone_placement/set_layout", Request(1), 1).GetAwaiter().GetResult());
                server.GetAwaiter().GetResult();
            }
            finally { listener.Stop(); }
        }

        [Test] public void CancellationClosesPendingConnection()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            using (var cancel = new CancellationTokenSource())
            {
                var server = Task.Run(() =>
                {
                    using (TcpClient peer = listener.AcceptTcpClient())
                    using (NetworkStream stream = peer.GetStream())
                    {
                        stream.ReadTimeout = 5000;
                        Handshake(stream); ReadPacket(stream); ReadPacket(stream);
                        cancel.Cancel();
                        Assert.That(stream.ReadByte(), Is.EqualTo(-1));
                    }
                });
                try
                {
                    Assert.Throws<OperationCanceledException>(() => ConeRosServiceClient.SendAsync("127.0.0.1", port,
                        "/cone_placement/set_layout", Request(1), 5, cancel.Token).GetAwaiter().GetResult());
                    server.GetAwaiter().GetResult();
                }
                finally { listener.Stop(); }
            }
        }

        static byte[] Serialize(Message message)
        { var writer = new MessageSerializer(); writer.SerializeMessage(message); return writer.GetBytes(); }

        static void Handshake(Stream stream) => WritePacket(stream, "__handshake",
            Encoding.UTF8.GetBytes("{\"version\":\"v0.7.0\",\"metadata\":\"{\\\"protocol\\\":\\\"ROS2\\\"}\"}"));

        static void WritePacket(Stream stream, string topic, byte[] payload, bool fragment = false)
        {
            byte[] name = Encoding.UTF8.GetBytes(topic);
            var bytes = BitConverter.GetBytes(name.Length).Concat(name).Concat(BitConverter.GetBytes(payload.Length)).Concat(payload).ToArray();
            int chunk = fragment ? 1 : bytes.Length;
            for (int i = 0; i < bytes.Length; i += chunk) stream.Write(bytes, i, Math.Min(chunk, bytes.Length - i));
            stream.Flush();
        }

        static Tuple<string, byte[]> ReadPacket(Stream stream)
        {
            string topic = Encoding.UTF8.GetString(Read(stream, BitConverter.ToInt32(Read(stream, 4), 0)));
            return Tuple.Create(topic, Read(stream, BitConverter.ToInt32(Read(stream, 4), 0)));
        }

        static byte[] Read(Stream stream, int count)
        {
            var bytes = new byte[count]; int offset = 0;
            while (offset < count)
            { int read = stream.Read(bytes, offset, count - offset); if (read == 0) throw new EndOfStreamException(); offset += read; }
            return bytes;
        }
    }
}
