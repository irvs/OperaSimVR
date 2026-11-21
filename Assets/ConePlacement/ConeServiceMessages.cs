// Wire types for ros2/operasimvr_interfaces. Fixed arrays have no length prefix.
using System;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace OperaSimVR.ConePlacement.Ros
{
    [Serializable]
    public sealed class ConeGroupMsg : Message
    {
        public const string k_RosMessageName = "operasimvr_interfaces/ConeGroup";
        public override string RosMessageName => k_RosMessageName;
        public uint group_id;
        public PointMsg[] positions = Array.Empty<PointMsg>();

        public static ConeGroupMsg Deserialize(MessageDeserializer reader)
        {
            var result = new ConeGroupMsg();
            reader.Read(out result.group_id);
            reader.Read(out result.positions, PointMsg.Deserialize, reader.ReadLength());
            return result;
        }

        public override void SerializeTo(MessageSerializer writer)
        {
            writer.Write(group_id);
            writer.WriteLength(positions);
            writer.Write(positions);
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register() => MessageRegistry.Register(k_RosMessageName, Deserialize);
    }

    [Serializable]
    public sealed class SetConeLayoutRequest : Message
    {
        public const int Capacity = 20;
        public const string k_RosMessageName = "operasimvr_interfaces/SetConeLayout";
        public override string RosMessageName => k_RosMessageName;
        public string frame_id = "";
        public uint group_count;
        public ConeGroupMsg[] groups = CreateSlots();

        static ConeGroupMsg[] CreateSlots()
        {
            var result = new ConeGroupMsg[Capacity];
            for (int i = 0; i < result.Length; i++) result[i] = new ConeGroupMsg();
            return result;
        }

        public static SetConeLayoutRequest Deserialize(MessageDeserializer reader)
        {
            var result = new SetConeLayoutRequest();
            reader.Read(out result.frame_id);
            reader.Read(out result.group_count);
            reader.Read(out result.groups, ConeGroupMsg.Deserialize, Capacity);
            return result;
        }

        public override void SerializeTo(MessageSerializer writer)
        {
            if (groups == null || groups.Length != Capacity)
                throw new InvalidOperationException("The service requires exactly 20 group slots.");
            writer.Write(frame_id);
            writer.Write(group_count);
            writer.Write(groups); // fixed array: deliberately no WriteLength
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register() => MessageRegistry.Register(k_RosMessageName, Deserialize);
    }

    [Serializable]
    public sealed class SetConeLayoutResponse : Message
    {
        public const string k_RosMessageName = SetConeLayoutRequest.k_RosMessageName;
        public override string RosMessageName => k_RosMessageName;
        public bool success;
        public string message = "";
        public uint group_count;
        public uint cone_count;

        public static SetConeLayoutResponse Deserialize(MessageDeserializer reader)
        {
            var result = new SetConeLayoutResponse();
            reader.Read(out result.success);
            reader.Read(out result.message);
            reader.Read(out result.group_count);
            reader.Read(out result.cone_count);
            return result;
        }

        public override void SerializeTo(MessageSerializer writer)
        {
            writer.Write(success);
            writer.Write(message);
            writer.Write(group_count);
            writer.Write(cone_count);
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register() => MessageRegistry.Register(k_RosMessageName, Deserialize, MessageSubtopic.Response);
    }
}
