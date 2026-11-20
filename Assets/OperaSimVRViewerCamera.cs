using UnityEngine;

/// <summary>Frame the first received ZX200 pose for an explicitly selected desktop viewer.</summary>
public sealed class OperaSimVRViewerCamera : MonoBehaviour
{
    void LateUpdate()
    {
        if (!OperaSimVRDesktopInput.IsActive) return;
        GameObject machine = GameObject.Find("zx200");
        PoseSubscriber pose = machine == null ? null : machine.GetComponent<PoseSubscriber>();
        if (pose == null || !pose.HasReceivedPose) return;
        OVRCameraRig rig = GetComponentInChildren<OVRCameraRig>(true);
        if (rig == null || rig.centerEyeAnchor == null) return;

        Vector3 target = machine.transform.position + Vector3.up * 2f;
        Vector3 viewpoint = machine.transform.position + new Vector3(12f, 7f, 16f);
        Vector3 direction = target - viewpoint;
        float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        rig.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.position += viewpoint - rig.centerEyeAnchor.position;
        Debug.Log("[OperaSimVR Ubuntu] Framed the first received ZX200 pose in the desktop viewer.");
        enabled = false;
    }
}
