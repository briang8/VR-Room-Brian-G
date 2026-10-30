using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRAssignment
{
    /// <summary>
    /// Visual and audio feedback for a socket: the ring turns blue while a matching object hovers, green once
    /// something is seated, and a snap sound plays at the socket when an object locks in.
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public class SocketFeedback : MonoBehaviour
    {
        public Renderer indicator;
        public Color idleColor = new Color(0.55f, 0.58f, 0.62f);
        public Color hoverColor = new Color(0.25f, 0.52f, 0.90f);
        public Color filledColor = new Color(0.35f, 0.75f, 0.45f);
        public AudioClip snapClip;

        XRSocketInteractor m_Socket;
        MaterialPropertyBlock m_Block;

        void Awake()
        {
            m_Socket = GetComponent<XRSocketInteractor>();
            m_Block = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            m_Socket.hoverEntered.AddListener(OnHoverChanged);
            m_Socket.hoverExited.AddListener(OnHoverChanged);
            m_Socket.selectEntered.AddListener(OnSelectEntered);
            m_Socket.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            m_Socket.hoverEntered.RemoveListener(OnHoverChanged);
            m_Socket.hoverExited.RemoveListener(OnHoverChanged);
            m_Socket.selectEntered.RemoveListener(OnSelectEntered);
            m_Socket.selectExited.RemoveListener(OnSelectExited);
        }

        void Start() => UpdateColor();

        void OnHoverChanged(BaseInteractionEventArgs _) => UpdateColor();

        void OnSelectExited(SelectExitEventArgs _) => UpdateColor();

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            UpdateColor();
            if (Time.timeSinceLevelLoad > 0.5f)
                KitAudio.PlayAt(snapClip != null ? snapClip : KitAudio.Snap, transform.position, 0.6f);
        }

        void UpdateColor()
        {
            if (indicator == null)
                return;
            var color = m_Socket.hasSelection ? filledColor : m_Socket.hasHover ? hoverColor : idleColor;
            indicator.GetPropertyBlock(m_Block);
            m_Block.SetColor("_BaseColor", color);
            m_Block.SetColor("_Color", color);
            m_Block.SetColor("_EmissionColor", color * 0.6f);
            indicator.SetPropertyBlock(m_Block);
        }
    }
}
