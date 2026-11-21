using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using UnityEngine;

namespace OperaSimVR.ConePlacement.Ros
{
    [Serializable]
    public sealed class ConeRosSettings
    {
        public string host = "127.0.0.1";
        public int port = 10001;
        public string serviceName = "/cone_placement/set_layout";
        public string frameId = "unity_world_flu";
        [Tooltip("Optional Unity pose of the ROS frame origin. Scale is ignored.")]
        public Transform frameOrigin;
        public int timeoutSeconds = 15;
    }

    public static class ConeRosRequestBuilder
    {
        public static bool TryBuild(IReadOnlyList<ConePositionGroup> source, string frameId,
            Vector3 origin, Quaternion rotation, out SetConeLayoutRequest request, out string error)
        {
            request = null;
            error = "";
            if (source == null || source.Count == 0)
            { error = "Place cones before sending."; return false; }
            if (source.Count > SetConeLayoutRequest.Capacity)
            { error = "ROS 2 supports at most 20 groups. Reduce the group count before sending."; return false; }
            if (string.IsNullOrWhiteSpace(frameId) || frameId.Any(char.IsWhiteSpace))
            { error = "Enter a Frame ID without spaces."; return false; }
            if (!Finite(origin) || !ConeLayout.IsFinite(rotation.x) || !ConeLayout.IsFinite(rotation.y) ||
                !ConeLayout.IsFinite(rotation.z) || !ConeLayout.IsFinite(rotation.w))
            { error = "The frame origin must have a finite position and rotation."; return false; }

            var result = new SetConeLayoutRequest { frame_id = frameId, group_count = (uint)source.Count };
            Quaternion inverse = Quaternion.Inverse(rotation);
            int slot = 0;
            int previousId = 0;
            foreach (ConePositionGroup group in source.OrderBy(g => g.Id))
            {
                if (group.Id <= previousId || group.Positions.Count == 0)
                { error = "Each used group must have a unique positive ID and cone positions."; return false; }
                previousId = group.Id;
                var positions = new PointMsg[group.Positions.Count];
                for (int i = 0; i < positions.Length; i++)
                {
                    Vector3 local = inverse * (group.Positions[i] - origin);
                    if (!Finite(local)) { error = "Cone coordinates must be finite."; return false; }
                    // Unity right/up/forward -> ROS forward/left/up, in metres.
                    positions[i] = new PointMsg(local.z, -local.x, local.y);
                }
                result.groups[slot++] = new ConeGroupMsg { group_id = (uint)group.Id, positions = positions };
            }
            request = result;
            return true;
        }

        static bool Finite(Vector3 value) => ConeLayout.IsFinite(value.x) && ConeLayout.IsFinite(value.y) && ConeLayout.IsFinite(value.z);
    }

    /// <summary>
    /// One request and one response, without reconnects/retries or a Unity Update dependency.
    /// Uses the dedicated, pre-registered endpoint supplied in ros2/operasimvr_cone_receiver.
    /// A separate port is required: ROS-TCP-Endpoint shares its reply queue across connections.
    /// </summary>
    public static class ConeRosServiceClient
    {
        // The endpoint routes late replies to its current connection, even after a timeout.
        // Unique IDs across connections prevent an old reply from acknowledging a new send.
        static long nextRequestId = DateTime.UtcNow.Ticks;
        const int MaxPayload = 16 * 1024 * 1024;
        [Serializable] sealed class Handshake { public string version; public string metadata; }
        [Serializable] sealed class Metadata { public string protocol; }
        [Serializable] sealed class ServiceCommand { public long srv_id; }
        [Serializable] sealed class LogCommand { public string text; }

        public static Task<SetConeLayoutResponse> SendAsync(string host, int port, string serviceName,
            SetConeLayoutRequest request, int timeoutSeconds, CancellationToken cancellation = default)
        {
#if !ROS2
            throw new InvalidOperationException("Enable the ROS2 scripting define before sending.");
#else
            if (string.IsNullOrWhiteSpace(host) || port < 1 || port > 65535)
                throw new ArgumentException("Enter a host and a port between 1 and 65535.");
            if (string.IsNullOrEmpty(serviceName) || serviceName[0] != '/' ||
                serviceName.Any(c => !(char.IsLetterOrDigit(c) || c == '_' || c == '/')))
                throw new ArgumentException("Enter an absolute ROS 2 service name (for example, /cone_placement/set_layout).");
            if (timeoutSeconds < 1 || timeoutSeconds > 300)
                throw new ArgumentException("Response timeout must be between 1 and 300 seconds.");
            var serializer = new MessageSerializer();
            serializer.SerializeMessage(request);
            byte[] payload = serializer.GetBytes();
            // Copy/serialize all Unity-side data before the worker starts.
            return Task.Run(() => Exchange(host.Trim(), port, serviceName, payload, timeoutSeconds, cancellation));
#endif
        }

        static SetConeLayoutResponse Exchange(string host, int port, string serviceName, byte[] payload,
            int timeoutSeconds, CancellationToken cancellation)
        {
            long requestId = Interlocked.Increment(ref nextRequestId);
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            using (var client = new TcpClient())
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                using (timeout.Token.Register(() => client.Close()))
                {
                    try
                    {
                        client.Connect(host, port);
                        client.NoDelay = true;
                        using (NetworkStream stream = client.GetStream())
                        {
                            ReadPacket(stream, out string destination, out byte[] data);
                            if (destination != "__handshake") throw new IOException("Missing endpoint handshake.");
                            var handshake = JsonUtility.FromJson<Handshake>(Encoding.UTF8.GetString(data));
                            var metadata = JsonUtility.FromJson<Metadata>(handshake.metadata);
                            if (handshake.version != "v0.7.0" || metadata.protocol != "ROS2")
                                throw new IOException("This sender requires ROS-TCP-Endpoint v0.7 ROS2 protocol.");

                            // The supplied endpoint registers and discovers the service BEFORE listening.
                            // Registering again here would replace that ready ROS client.
                            WritePacket(stream, "__request", Encoding.UTF8.GetBytes("{\"srv_id\":" + requestId.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}\0"));
                            WritePacket(stream, serviceName, payload);
                            bool awaitingPayload = false;
                            bool matchingResponse = false;
                            while (true)
                            {
                                ReadPacket(stream, out destination, out data);
                                if (awaitingPayload)
                                {
                                    awaitingPayload = false;
                                    // Consume a complete stale response pair before continuing to wait.
                                    if (!matchingResponse) continue;
                                    if (destination != serviceName) throw new IOException("Unexpected service response destination.");
                                    if (data.Length < 4 || data[0] != 0 || data[1] != 1)
                                        throw new IOException("Expected a little-endian ROS 2 CDR response.");
                                    var reader = new MessageDeserializer();
                                    reader.InitWithBuffer(data);
                                    // Return after the FIRST matching response, including success=false.
                                    return SetConeLayoutResponse.Deserialize(reader);
                                }
                                if (destination == "__response")
                                {
                                    var command = JsonUtility.FromJson<ServiceCommand>(Encoding.UTF8.GetString(data));
                                    matchingResponse = command.srv_id == requestId;
                                    awaitingPayload = true;
                                }
                                else if (destination == "__error")
                                    throw new IOException(JsonUtility.FromJson<LogCommand>(Encoding.UTF8.GetString(data)).text);
                                else if (destination != "" && destination != "__log" && destination != "__warn")
                                    throw new IOException("Unexpected endpoint packet: " + destination);
                            }
                        }
                    }
                    catch (Exception e) when (timeout.IsCancellationRequested)
                    {
                        if (cancellation.IsCancellationRequested) throw new OperationCanceledException("Send stopped.", e, cancellation);
                        throw new TimeoutException("No response confirmed within " + timeoutSeconds + " seconds. The receiver may have applied the layout. No automatic retry.", e);
                    }
                }
            }
        }

        static void WritePacket(Stream stream, string destination, byte[] payload)
        {
            byte[] name = Encoding.UTF8.GetBytes(destination);
            if (name.Length > 1024 || payload.Length > MaxPayload) throw new IOException("Endpoint packet is too large.");
            byte[] nameLength = BitConverter.GetBytes(name.Length), dataLength = BitConverter.GetBytes(payload.Length);
            stream.Write(nameLength, 0, 4);
            stream.Write(name, 0, name.Length);
            stream.Write(dataLength, 0, 4);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }

        static void ReadPacket(Stream stream, out string destination, out byte[] payload)
        {
            int nameLength = BitConverter.ToInt32(ReadExactly(stream, 4), 0);
            if (nameLength < 0 || nameLength > 1024) throw new IOException("Invalid endpoint destination length.");
            destination = Encoding.UTF8.GetString(ReadExactly(stream, nameLength)).TrimEnd('\0');
            int dataLength = BitConverter.ToInt32(ReadExactly(stream, 4), 0);
            if (dataLength < 0 || dataLength > MaxPayload) throw new IOException("Invalid endpoint payload length.");
            payload = ReadExactly(stream, dataLength);
        }

        static byte[] ReadExactly(Stream stream, int length)
        {
            var result = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int count = stream.Read(result, offset, length - offset);
                if (count == 0) throw new EndOfStreamException("The endpoint disconnected before a response was confirmed. No automatic retry.");
                offset += count;
            }
            return result;
        }
    }
}
