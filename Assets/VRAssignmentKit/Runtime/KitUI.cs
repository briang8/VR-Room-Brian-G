using UnityEngine;

namespace VRAssignment
{
    /// <summary>
    /// Small shared helpers for the world-space menus. All menus, panels and labels are normal objects in the
    /// scene hierarchy, so their size, text and colours can be edited by hand in the Inspector.
    /// </summary>
    public static class KitUI
    {
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Places a transform in front of the user's head at a comfortable reading height.</summary>
        public static void PlaceInFrontOfCamera(Transform target, float distance, float heightOffset = -0.1f)
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            var forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
                forward = cam.transform.up;
            forward.Normalize();
            target.position = cam.transform.position + forward * distance + Vector3.up * heightOffset;
            target.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }
}
