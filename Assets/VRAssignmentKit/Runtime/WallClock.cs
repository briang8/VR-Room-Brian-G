using System;
using TMPro;
using UnityEngine;

namespace VRAssignment
{
    /// <summary>
    /// Drives an analog clock from the system clock (DateTime.Now) every frame. No animation clips are used:
    /// each hand's rotation is calculated in code.
    ///   hour   angle = (hour % 12 + minute / 60) * 30 degrees
    ///   minute angle = (minute + second / 60) * 6 degrees
    ///   second angle = second * 6 degrees (sweeps smoothly using milliseconds when enabled)
    /// The clock face normal, the 12 o'clock direction and each hand's starting angle are measured from the
    /// meshes on Awake, so it works with any clock model whose hands pivot around the face centre.
    /// An optional TextMeshPro label shows the same time digitally. The digital display is a normal object in the
    /// hierarchy under the clock (Digital Clock > Background > Text), so its size, font and colour can be edited by hand.
    /// </summary>
    public class WallClock : MonoBehaviour
    {
        [Header("Analog hands")]
        public Transform hourHand;
        public Transform minuteHand;
        public Transform secondHand;
        [Tooltip("Sweep the second hand smoothly instead of ticking once per second.")]
        public bool smoothSeconds = true;

        [Header("Digital display (optional)")]
        public TMP_Text digitalTime;
        public TMP_Text digitalDate;
        public bool use24Hour = true;

        [Header("Calibration")]
        [Tooltip("Flip this if the hands turn anticlockwise on your model.")]
        public bool reverseDirection;

        struct HandRest
        {
            public Transform hand;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public float angleAtRest;
        }

        HandRest m_Hour, m_Minute, m_Second;
        Vector3 m_LocalCenter;
        Vector3 m_LocalAxis;
        int m_LastShownSecond = -1;

        void Awake()
        {
            GetFace(out m_LocalCenter, out m_LocalAxis, out var localUp, out _);
            m_Hour = Capture(hourHand, localUp);
            m_Minute = Capture(minuteHand, localUp);
            m_Second = Capture(secondHand, localUp);
        }

        void Update()
        {
            var now = DateTime.Now;
            var seconds = now.Second + (smoothSeconds ? now.Millisecond / 1000f : 0f);
            var minutes = now.Minute + seconds / 60f;
            var hours = now.Hour % 12 + minutes / 60f;

            Apply(m_Hour, hours * 30f);
            Apply(m_Minute, minutes * 6f);
            Apply(m_Second, seconds * 6f);

            if (now.Second != m_LastShownSecond)
            {
                m_LastShownSecond = now.Second;
                if (digitalTime != null)
                    digitalTime.text = now.ToString(use24Hour ? "HH:mm:ss" : "h:mm:ss tt");
                if (digitalDate != null)
                    digitalDate.text = now.ToString("dddd d MMMM yyyy");
            }
        }

        /// <summary>
        /// Measures the clock face in this transform's local space: centre, the normal pointing out of the
        /// face towards the viewer, the 12 o'clock direction and the face radius (world units).
        /// </summary>
        public void GetFace(out Vector3 localCenter, out Vector3 localFrontAxis, out Vector3 localUp, out float worldRadius)
        {
            var bodyBounds = new Bounds();
            var handBounds = new Bounds();
            bool hasBody = false, hasHands = false;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r.GetComponentInParent<Canvas>() != null)
                    continue;
                var isHand = IsHand(r.transform);
                var b = LocalBounds(r);
                if (isHand)
                {
                    if (hasHands) handBounds.Encapsulate(b); else handBounds = b;
                    hasHands = true;
                }
                else
                {
                    if (hasBody) bodyBounds.Encapsulate(b); else bodyBounds = b;
                    hasBody = true;
                }
            }

            if (!hasBody)
                bodyBounds = handBounds;

            // The face is the thinnest dimension of the clock body.
            var size = bodyBounds.size;
            var axisIndex = size.x <= size.y && size.x <= size.z ? 0 : size.y <= size.z ? 1 : 2;
            var axis = Vector3.zero;
            axis[axisIndex] = 1f;

            // Hands sit in front of the face, so the front normal points from the body towards the hands.
            if (hasHands && Vector3.Dot(handBounds.center - bodyBounds.center, axis) < 0f)
                axis = -axis;
            else if (!hasHands && Vector3.Dot(transform.InverseTransformDirection(-transform.forward), axis) < 0f)
                axis = -axis;

            localCenter = bodyBounds.center;
            if (hourHand != null && minuteHand != null)
            {
                var a = transform.InverseTransformPoint(hourHand.position);
                var b = transform.InverseTransformPoint(minuteHand.position);
                if ((a - b).sqrMagnitude < 1e-6f)
                    localCenter = a;
            }

            localFrontAxis = axis;

            // 12 o'clock is world up projected onto the face (the clock hangs on a wall).
            var up = Vector3.ProjectOnPlane(transform.InverseTransformDirection(Vector3.up), axis);
            if (up.sqrMagnitude < 1e-4f)
                up = Vector3.ProjectOnPlane(Vector3.up, axis);
            localUp = up.normalized;

            var extents = Vector3.Scale(bodyBounds.extents, transform.lossyScale);
            extents[axisIndex] = 0f;
            worldRadius = Mathf.Max(extents.x, extents.y, extents.z);
        }

        bool IsHand(Transform t)
        {
            for (var p = t; p != null && p != transform; p = p.parent)
            {
                if (p == hourHand || p == minuteHand || p == secondHand)
                    return true;
            }

            return false;
        }

        Bounds LocalBounds(Renderer r)
        {
            var filter = r.GetComponent<MeshFilter>();
            var meshBounds = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.zero);
            var result = new Bounds(transform.InverseTransformPoint(r.transform.TransformPoint(meshBounds.center)), Vector3.zero);
            var e = meshBounds.extents;
            for (var i = 0; i < 8; i++)
            {
                var corner = meshBounds.center + Vector3.Scale(e, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                result.Encapsulate(transform.InverseTransformPoint(r.transform.TransformPoint(corner)));
            }

            return result;
        }

        HandRest Capture(Transform hand, Vector3 localUp)
        {
            var rest = new HandRest { hand = hand };
            if (hand == null)
                return rest;

            rest.localPosition = hand.localPosition;
            rest.localRotation = hand.localRotation;

            // Work out where this hand currently points (degrees clockwise from 12) so any starting pose works.
            var r = hand.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                var tip = LocalBounds(r).center - m_LocalCenter;
                tip = Vector3.ProjectOnPlane(tip, m_LocalAxis);
                if (tip.sqrMagnitude > 1e-8f)
                    rest.angleAtRest = Vector3.SignedAngle(localUp, tip, m_LocalAxis) * (reverseDirection ? -1f : 1f);
            }

            return rest;
        }

        void Apply(HandRest rest, float clockwiseDegrees)
        {
            if (rest.hand == null)
                return;

            var sign = reverseDirection ? -1f : 1f;
            var delta = (clockwiseDegrees - rest.angleAtRest) * sign;

            // Rotate the hand's rest pose around the face centre, in this transform's local space.
            var parent = rest.hand.parent;
            var rot = Quaternion.AngleAxis(delta, transform.TransformDirection(m_LocalAxis));
            var center = transform.TransformPoint(m_LocalCenter);
            var restPos = parent != null ? parent.TransformPoint(rest.localPosition) : rest.localPosition;
            var restRot = parent != null ? parent.rotation * rest.localRotation : rest.localRotation;
            rest.hand.SetPositionAndRotation(center + rot * (restPos - center), rot * restRot);
        }
    }
}
