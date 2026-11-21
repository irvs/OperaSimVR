using System;
using System.IO;
using System.Threading.Tasks;
using OperaSimVR.ConePlacement.Ros;
using UnityEditor;
using UnityEngine;

namespace OperaSimVR.ConePlacement.Tests
{
    /// <summary>Opt-in real ROS integration. Use an isolated Unity project and ROS domain.</summary>
    public static class ConeRosIntegrationBatch
    {
        static GameObject field, owner;
        static ConePlacementController controller;
        static Task<SetConeLayoutResponse> rejection;
        static string acceptedStatus;
        static double deadline;

        [Serializable] sealed class Result
        {
            public bool passed;
            public bool playMode;
            public string accepted;
            public bool rejectionFinished;
            public string rejectionMessage;
            public int placedCones;
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run this check in an isolated batch project.");
            Directory.CreateDirectory("Logs/cone-ros-service");
            field = GameObject.CreatePrimitive(PrimitiveType.Cube);
            field.transform.SetPositionAndRotation(new Vector3(2000, 5, 2000), Quaternion.Euler(0, 0, 10));
            field.transform.localScale = new Vector3(100, 1, 100);
            field.AddComponent<ConePlacementSurface>();
            owner = new GameObject("ROS integration cone layout");
            controller = owner.AddComponent<ConePlacementController>();
            Physics.SyncTransforms();
            controller.AddVertex(new Vector3(1995, 5, 2000));
            controller.AddVertex(new Vector3(2005, 5, 2000));
            controller.AddVertex(new Vector3(2010, 5, 2010));
            controller.SetNeighbors(1, new[] { 2 });
            if (!controller.PlaceCones()) throw new InvalidOperationException(controller.Status);
            controller.RosSettings.port = int.Parse(Environment.GetEnvironmentVariable("CONE_TEST_PORT") ?? "19001");
            controller.TryGetPlacedGroups(out var groups, out _);
            ConeRosRequestBuilder.TryBuild(groups, "unity_world_flu", Vector3.zero, Quaternion.identity, out var request, out _);
            File.WriteAllText("Logs/cone-ros-service/expected-layout.json", JsonUtility.ToJson(request, true));
            if (!controller.SendToRos()) throw new InvalidOperationException(controller.RosStatus);
            if (controller.SendToRos()) throw new InvalidOperationException("A second click started a duplicate send.");
            deadline = EditorApplication.timeSinceStartup + 40;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Integration deadline exceeded.");
                controller.PollRosService();
                if (controller.IsSendingToRos) return;
                if (rejection == null)
                {
                    acceptedStatus = controller.RosStatus;
                    if (!acceptedStatus.StartsWith("Accepted:")) throw new InvalidOperationException(acceptedStatus);
                    // A separate, deliberately invalid request exercises a genuine ROS rejection response.
                    var invalid = new SetConeLayoutRequest { frame_id = "unity_world_flu", group_count = 1 };
                    rejection = ConeRosServiceClient.SendAsync("127.0.0.1", controller.RosSettings.port,
                        controller.RosSettings.serviceName, invalid, 15);
                    return;
                }
                if (!rejection.IsCompleted) return;
                var response = rejection.GetAwaiter().GetResult();
                if (response.success) throw new InvalidOperationException("Receiver accepted an invalid request.");
                File.WriteAllText("Logs/cone-ros-service/integration.json", JsonUtility.ToJson(new Result
                {
                    passed = true, playMode = Application.isPlaying, accepted = acceptedStatus,
                    rejectionFinished = true, rejectionMessage = response.message, placedCones = controller.PlacedConeCount
                }, true));
                Finish(0);
            }
            catch (Exception e) { Debug.LogException(e); Finish(1); }
        }

        static void Finish(int code)
        {
            EditorApplication.update -= Poll;
            UnityEngine.Object.DestroyImmediate(owner);
            UnityEngine.Object.DestroyImmediate(field);
            EditorApplication.Exit(code);
        }
    }
}
