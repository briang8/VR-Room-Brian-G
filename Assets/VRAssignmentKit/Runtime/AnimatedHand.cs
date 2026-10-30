using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRAssignment
{
    /// <summary>
    /// Drives the Stylized Simple Hands model (Assets/SimpleHands) that replaces the course's static hand meshes. It lives under a tracked controller and
    /// reads two analog inputs from that controller every frame:
    ///   Grip    (0-1) curls the middle, ring and little fingers (and partly the thumb)
    ///   Trigger (0-1) curls the index finger
    /// Each finger is a chain of three joints; a joint's rotation is its rest rotation plus a curl angle around
    /// its local X axis, blended by the input value and smoothed so motion looks natural.
    ///
    /// If an Animator is assigned (for example an imported hand model with "Grip" and "Trigger" float
    /// parameters), the same values are also written to it, so a skinned hand model can be used instead.
    ///
    /// The hand model is a normal object in the hierarchy (Animated Hand > Hand Model). To line it up with the
    /// controller, move or rotate "Hand Model" in the Scene view and save; nothing overrides it at runtime.
    /// </summary>
    public class AnimatedHand : MonoBehaviour
    {
        public enum Side { Left, Right }

        [Serializable]
        public class Finger
        {
            public string name;
            public Transform[] joints = new Transform[0];
            public Vector3 restCurl;
            public Vector3 fullCurl;
            public bool driveByTrigger;
            public bool isThumb;
            [NonSerialized] public Quaternion[] restRotations;
        }

        public Side side = Side.Right;

        [Header("Animation")]
        [Tooltip("Optional Animator with float parameters 'Grip' and 'Trigger'.")]
        public Animator animator;
        public float smoothing = 16f;
        [Tooltip("Set to -1 if the fingers bend backwards instead of into the palm.")]
        public float curlDirection = 1f;

        [SerializeField] Transform m_ModelRoot;
        [SerializeField] List<Finger> m_Fingers = new List<Finger>();

        InputAction m_GripAction;
        InputAction m_TriggerAction;
        float m_Grip;
        float m_Trigger;

        static readonly int s_GripParam = Animator.StringToHash("Grip");
        static readonly int s_TriggerParam = Animator.StringToHash("Trigger");

        /// <summary>The visible hand model (child "Hand Model").</summary>
        public Transform ModelRoot => m_ModelRoot;

        public float Grip => m_Grip;
        public float Trigger => m_Trigger;

        void Awake()
        {
            foreach (var f in m_Fingers)
            {
                f.restRotations = new Quaternion[f.joints.Length];
                for (var i = 0; i < f.joints.Length; i++)
                    f.restRotations[i] = f.joints[i] != null ? f.joints[i].localRotation : Quaternion.identity;
            }
        }

        void OnEnable()
        {
            var usage = side == Side.Left ? "{LeftHand}" : "{RightHand}";
            m_GripAction = new InputAction("Grip", InputActionType.Value, expectedControlType: "Axis");
            m_GripAction.AddBinding($"<XRController>{usage}/{{Grip}}");
            m_GripAction.AddBinding($"<XRController>{usage}/grip");
            m_TriggerAction = new InputAction("Trigger", InputActionType.Value, expectedControlType: "Axis");
            m_TriggerAction.AddBinding($"<XRController>{usage}/{{Trigger}}");
            m_TriggerAction.AddBinding($"<XRController>{usage}/trigger");
            m_GripAction.Enable();
            m_TriggerAction.Enable();
        }

        void OnDisable()
        {
            m_GripAction?.Dispose();
            m_TriggerAction?.Dispose();
            m_GripAction = null;
            m_TriggerAction = null;
        }

        void Update()
        {
            var grip = m_GripAction != null ? m_GripAction.ReadValue<float>() : 0f;
            var trigger = m_TriggerAction != null ? m_TriggerAction.ReadValue<float>() : 0f;
            var k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            m_Grip = Mathf.Lerp(m_Grip, grip, k);
            m_Trigger = Mathf.Lerp(m_Trigger, trigger, k);

            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetFloat(s_GripParam, m_Grip);
                animator.SetFloat(s_TriggerParam, m_Trigger);
            }

            foreach (var f in m_Fingers)
            {
                if (f.restRotations == null)
                    continue;
                var t = f.isThumb ? Mathf.Max(m_Grip * 0.85f, m_Trigger * 0.45f) : f.driveByTrigger ? m_Trigger : m_Grip;
                for (var i = 0; i < f.joints.Length && i < 3; i++)
                {
                    if (f.joints[i] == null)
                        continue;
                    var angle = Mathf.Lerp(f.restCurl[i], f.fullCurl[i], t);
                    f.joints[i].localRotation = f.restRotations[i] * Quaternion.Euler(angle * curlDirection, 0f, 0f);
                }
            }
        }
    }
}
