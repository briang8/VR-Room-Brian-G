using UnityEngine;

namespace VRAssignment
{
    /// <summary>
    /// Small floating label that turns to face the player and fades out with distance, used to point out
    /// interactions ("Grab the handle", "Flashlight dock", ...). The label is a normal canvas child
    /// (Label > Text), so its text and size can be edited by hand.
    /// </summary>
    public class Tooltip : MonoBehaviour
    {
        [Tooltip("Canvas Group on the label canvas; its alpha is faded with distance.")]
        public CanvasGroup group;
        public float visibleDistance = 3.5f;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            var toLabel = transform.position - cam.transform.position;
            toLabel.y = 0f;
            if (toLabel.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(toLabel.normalized, Vector3.up);

            if (group != null)
                group.alpha = Mathf.Clamp01((visibleDistance - toLabel.magnitude) / 1f);
        }
    }
}
