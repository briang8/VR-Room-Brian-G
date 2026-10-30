using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRAssignment
{
    /// <summary>
    /// Physics door driven by the user's hand.
    ///
    /// Setup on the door leaf: a non-kinematic Rigidbody (no gravity), a HingeJoint whose anchor/axis sit on the
    /// hinge edge (limits stop it at +/- 100 degrees), and an XRGrabInteractable whose only collider is the door's
    /// own handle ("Handle Grab"). The grab interactable has Track Position and Track Rotation switched off, so
    /// XRI does not move the door itself: it only tells this script who is holding the handle.
    ///
    /// While the handle is held, every physics step this script measures the hand's angle around the hinge,
    /// works out how far the door is from that angle, and gives the Rigidbody a rotation speed about the hinge
    /// towards it. The speed is capped and eased in, so the door feels heavy and swings at a natural pace while
    /// still following the hand continuously (push and pull). The HingeJoint keeps it attached to its frame.
    /// When released the door keeps a little of its swing and slows down from hinge friction, like a real door.
    /// No animation is used.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(HingeJoint))]
    public class PhysicsDoor : MonoBehaviour
    {
        public XRGrabInteractable grabInteractable;
        [Tooltip("Colliders of the static frame/wall the door sits in. Collisions with these are ignored.")]
        public Collider[] frameColliders;

        [Header("How the door feels")]
        [Tooltip("How quickly the door catches up with your hand. Lower = heavier door.")]
        public float followStrength = 4f;
        [Tooltip("Fastest the door can swing while held, in degrees per second. A real door is about 60-120.")]
        public float maxSwingSpeed = 75f;
        [Tooltip("How quickly the swing speed can build up, in degrees per second squared. Lower = more inertia.")]
        public float maxAcceleration = 220f;
        [Tooltip("How much swing the door keeps when you let go (0 = stops dead, 1 = keeps all of it).")]
        [Range(0f, 1f)] public float releaseMomentum = 0.3f;
        [Tooltip("Hinge friction when nobody holds the door (Rigidbody angular damping). Higher = stops sooner.")]
        public float hingeFriction = 3f;
        [Tooltip("Furthest the door opens either way, in degrees (keep below the HingeJoint limits).")]
        public float maxOpenAngle = 95f;

        [Header("Grab and latch")]
        [Tooltip("Release a near (hand) grab if the hand moves this far from the handle.")]
        public float breakDistance = 0.45f;
        [Tooltip("When released within this many degrees of closed, the door clicks shut.")]
        public float latchAngle = 4f;
        public AudioClip creakClip;
        public AudioClip latchClip;

        Rigidbody m_Rigidbody;
        HingeJoint m_Hinge;
        Quaternion m_ClosedLocalRotation;
        Vector3 m_ClosedLocalPosition;
        Vector3 m_HandleLocal;
        Vector3 m_ClosedDirection;
        AudioSource m_Creak;
        IXRSelectInteractor m_Holder;
        bool m_HolderIsNear;
        bool m_HasGrabOffset;
        float m_GrabOffset;
        float m_TargetAngle;
        float m_Speed;

        /// <summary>Current opening angle in degrees (0 = closed).</summary>
        public float Angle => m_Hinge != null ? MeasureAngle() : 0f;

        /// <summary>Largest absolute opening angle reached so far (used by the objectives).</summary>
        public float MaxAngleReached { get; private set; }

        Vector3 HingePoint => transform.TransformPoint(m_Hinge.anchor);
        Vector3 HingeAxis => transform.TransformDirection(m_Hinge.axis).normalized;

        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Hinge = GetComponent<HingeJoint>();
            m_ClosedLocalRotation = transform.localRotation;
            m_ClosedLocalPosition = transform.localPosition;
            if (grabInteractable == null)
                grabInteractable = GetComponent<XRGrabInteractable>();

            m_Rigidbody.useGravity = false;
            SetDamping(hingeFriction);

            // Reference direction: from the hinge line to the handle, with the door closed.
            var handleWorld = HandleCenter();
            m_HandleLocal = transform.InverseTransformPoint(handleWorld);
            m_ClosedDirection = Vector3.ProjectOnPlane(handleWorld - HingePoint, HingeAxis);

            var own = GetComponentsInChildren<Collider>();
            foreach (var frame in frameColliders)
            {
                if (frame == null)
                    continue;
                foreach (var c in own)
                    Physics.IgnoreCollision(c, frame, true);
            }

            m_Creak = gameObject.AddComponent<AudioSource>();
            m_Creak.clip = creakClip != null ? creakClip : KitAudio.Creak;
            m_Creak.loop = true;
            m_Creak.volume = 0f;
            m_Creak.spatialBlend = 1f;
            m_Creak.minDistance = 0.6f;
            m_Creak.maxDistance = 10f;
            m_Creak.playOnAwake = false;
        }

        void OnEnable()
        {
            if (grabInteractable == null)
                return;
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        void OnDisable()
        {
            if (grabInteractable == null)
                return;
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            m_Holder = args.interactorObject;
            m_HasGrabOffset = false;
            m_TargetAngle = MeasureAngle();
            m_Speed = 0f;

            var hand = m_Holder.GetAttachTransform(grabInteractable).position;
            m_HolderIsNear = Vector3.Distance(hand, ClosestHandlePoint(hand)) < breakDistance;

            var haptics = args.interactorObject.transform.GetComponentInParent<HapticImpulsePlayer>();
            if (haptics != null)
                haptics.SendHapticImpulse(0.35f, 0.08f);
        }

        void OnReleased(SelectExitEventArgs args)
        {
            if (args.interactorObject != m_Holder)
                return;

            m_Holder = null;
            SetDamping(hingeFriction);
            SetHingeSpeed(m_Speed * releaseMomentum);

            if (Mathf.Abs(MeasureAngle()) < latchAngle)
            {
                ResetDoor();
                KitAudio.PlayAt(latchClip != null ? latchClip : KitAudio.Snap, transform.position, 0.5f);
            }

            m_Speed = 0f;
        }

        void FixedUpdate()
        {
            if (m_Holder == null || !grabInteractable.isSelected)
                return;

            var current = MeasureAngle();
            var hand = m_Holder.GetAttachTransform(grabInteractable).position;
            var fromHinge = Vector3.ProjectOnPlane(hand - HingePoint, HingeAxis);

            // Ignore the hand when it is almost on the hinge line, where its angle is meaningless.
            if (fromHinge.sqrMagnitude > 0.01f)
            {
                var handAngle = Vector3.SignedAngle(m_ClosedDirection, fromHinge, HingeAxis);
                if (!m_HasGrabOffset)
                {
                    // Remember where on the door the hand grabbed so the door does not jump to the hand.
                    m_GrabOffset = Mathf.DeltaAngle(handAngle, current);
                    m_HasGrabOffset = true;
                }

                m_TargetAngle = Mathf.Clamp(handAngle + m_GrabOffset, -maxOpenAngle, maxOpenAngle);
            }

            var wanted = Mathf.Clamp(Mathf.DeltaAngle(current, m_TargetAngle) * followStrength, -maxSwingSpeed, maxSwingSpeed);
            m_Speed = Mathf.MoveTowards(m_Speed, wanted, maxAcceleration * Time.fixedDeltaTime);
            SetDamping(0f);
            SetHingeSpeed(m_Speed);
        }

        void Update()
        {
            var angle = MeasureAngle();
            MaxAngleReached = Mathf.Max(MaxAngleReached, Mathf.Abs(angle));

            var speed = m_Rigidbody.angularVelocity.magnitude;
            var target = Mathf.Clamp01((speed - 0.15f) / 1.5f) * 0.4f;
            m_Creak.volume = Mathf.MoveTowards(m_Creak.volume, target, Time.deltaTime * 3f);
            m_Creak.pitch = 0.8f + Mathf.Clamp01(speed / 2f) * 0.25f;
            if (m_Creak.volume > 0.01f && !m_Creak.isPlaying)
                m_Creak.Play();
            else if (m_Creak.volume <= 0.01f && m_Creak.isPlaying)
                m_Creak.Stop();

            if (m_Holder != null && m_HolderIsNear && grabInteractable.isSelected)
            {
                var hand = m_Holder.GetAttachTransform(grabInteractable).position;
                if (Vector3.Distance(hand, ClosestHandlePoint(hand)) > breakDistance)
                    grabInteractable.interactionManager.SelectExit(m_Holder, grabInteractable);
            }
        }

        float MeasureAngle()
        {
            var now = Vector3.ProjectOnPlane(transform.TransformPoint(m_HandleLocal) - HingePoint, HingeAxis);
            return Vector3.SignedAngle(m_ClosedDirection, now, HingeAxis);
        }

        /// <summary>Spins the door about its hinge line at the given speed (degrees per second).</summary>
        void SetHingeSpeed(float degreesPerSecond)
        {
            var omega = HingeAxis * (degreesPerSecond * Mathf.Deg2Rad);
            m_Rigidbody.angularVelocity = omega;
            SetVelocity(Vector3.Cross(omega, m_Rigidbody.worldCenterOfMass - HingePoint));
        }

        Vector3 HandleCenter()
        {
            if (grabInteractable == null || grabInteractable.colliders.Count == 0)
                return transform.position;
            var bounds = new Bounds();
            var first = true;
            foreach (var c in grabInteractable.colliders)
            {
                if (c == null)
                    continue;
                // Work from the collider's own transform so this is valid in Awake, before physics has synced.
                var center = c is BoxCollider box ? c.transform.TransformPoint(box.center) : c.transform.position;
                if (first) bounds = new Bounds(center, Vector3.zero); else bounds.Encapsulate(center);
                first = false;
            }

            return first ? transform.position : bounds.center;
        }

        Vector3 ClosestHandlePoint(Vector3 from)
        {
            var best = transform.position;
            var bestDistance = float.MaxValue;
            foreach (var c in grabInteractable.colliders)
            {
                if (c == null)
                    continue;
                var p = c.ClosestPoint(from);
                var d = (p - from).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = p;
                }
            }

            return best;
        }

        void SetVelocity(Vector3 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            m_Rigidbody.linearVelocity = velocity;
#else
            m_Rigidbody.velocity = velocity;
#endif
        }

        void SetDamping(float angular)
        {
#if UNITY_6000_0_OR_NEWER
            m_Rigidbody.angularDamping = angular;
#else
            m_Rigidbody.angularDrag = angular;
#endif
        }

        /// <summary>Puts the door back to closed (used by Reset in the room menu and by the latch).</summary>
        public void ResetDoor()
        {
            m_Rigidbody.angularVelocity = Vector3.zero;
            SetVelocity(Vector3.zero);
            transform.localRotation = m_ClosedLocalRotation;
            transform.localPosition = m_ClosedLocalPosition;
            m_Speed = 0f;
        }
    }
}
