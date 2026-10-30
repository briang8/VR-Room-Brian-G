using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace VRAssignment
{
    /// <summary>
    /// In-experience menu, available in every room. Press Y (left) or B (right) or the menu button to open it
    /// in front of you; point a controller ray at a button and pull the trigger to press it.
    ///
    /// The menu itself is a normal world-space canvas in the hierarchy (Room Menu > Menu Panel). Every button's
    /// On Click() is wired in the Inspector to one of the public methods below, e.g. Go("VR Room"),
    /// so text, sizes, colours and destinations can all be edited by hand.
    /// </summary>
    public class RoomMenu : MonoBehaviour
    {
        [Header("Scene objects")]
        [Tooltip("The menu canvas. It is hidden at start and moved in front of you when opened.")]
        public Canvas panel;
        [Tooltip("Side panel that lists the controls.")]
        public GameObject controlsPanel;
        public TMP_Text controlsButtonLabel;
        [Tooltip("Button that shows/hides the objectives (hidden automatically if the room has no objectives).")]
        public GameObject objectivesButton;
        public TMP_Text objectivesButtonLabel;
        public TMP_Text movementButtonLabel;
        [Tooltip("Optional UI toggles (e.g. the Settings panel) that should follow the continuous movement setting.")]
        public Toggle[] movementToggles = new Toggle[0];

        [Header("Behaviour")]
        public string startSceneName = "Start";
        public float distance = 1.1f;
        public float heightOffset = -0.1f;
        public AudioClip clickClip;

        ObjectiveTracker m_Objectives;
        InputAction m_Toggle;
        readonly List<Snapshot> m_Snapshots = new List<Snapshot>();

        struct Snapshot
        {
            public XRGrabInteractable interactable;
            public Transform parent;
            public Vector3 position;
            public Quaternion rotation;
        }

        public bool IsOpen => panel != null && panel.gameObject.activeSelf;

        void Awake()
        {
            if (panel != null)
                panel.gameObject.SetActive(false);
            if (controlsPanel != null)
                controlsPanel.SetActive(false);
        }

        void Start()
        {
            m_Objectives = FindFirstObjectByType<ObjectiveTracker>(FindObjectsInactive.Include);
            if (objectivesButton != null)
                objectivesButton.SetActive(m_Objectives != null);
            RefreshLabels();

            foreach (var grab in FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            {
                m_Snapshots.Add(new Snapshot
                {
                    interactable = grab,
                    parent = grab.transform.parent,
                    position = grab.transform.position,
                    rotation = grab.transform.rotation,
                });
            }
        }

        void OnEnable()
        {
            m_Toggle = new InputAction("Toggle Room Menu", InputActionType.Button);
            m_Toggle.AddBinding("<XRController>{LeftHand}/secondaryButton");
            m_Toggle.AddBinding("<XRController>{RightHand}/secondaryButton");
            m_Toggle.AddBinding("<XRController>{LeftHand}/menuButton");
            m_Toggle.AddBinding("<Keyboard>/m");
            m_Toggle.performed += OnToggle;
            m_Toggle.Enable();
        }

        void OnDisable()
        {
            m_Toggle.performed -= OnToggle;
            m_Toggle.Dispose();
        }

        void OnToggle(InputAction.CallbackContext _)
        {
            if (IsOpen) Close(); else Open();
        }

        public void Open()
        {
            if (panel == null)
                return;
            KitUI.PlaceInFrontOfCamera(panel.transform, distance, heightOffset);
            panel.worldCamera = Camera.main;
            panel.gameObject.SetActive(true);
            RefreshLabels();
            Click();
        }

        public void Close()
        {
            if (panel != null)
                panel.gameObject.SetActive(false);
        }

        /// <summary>Button: travel to a scene by name (set the name in the button's On Click()).</summary>
        public void Go(string sceneName)
        {
            Click();
            if (SceneNavigator.IsCurrent(sceneName))
                return;
            Close();
            SceneNavigator.Load(sceneName);
        }

        /// <summary>Button: back to the start screen.</summary>
        public void GoToStart() => Go(startSceneName);

        /// <summary>Button: show or hide the controls side panel.</summary>
        public void ToggleControls()
        {
            if (controlsPanel == null)
                return;
            Click();
            controlsPanel.SetActive(!controlsPanel.activeSelf);
            RefreshLabels();
        }

        /// <summary>Button: show or hide the objectives panel on the left wrist.</summary>
        public void ToggleObjectives()
        {
            if (m_Objectives == null)
                return;
            Click();
            m_Objectives.SetVisible(!m_Objectives.IsVisible);
            RefreshLabels();
        }

        /// <summary>Button: turn continuous movement (left thumbstick walking) on or off. It is off by default.</summary>
        public void ToggleContinuousMovement()
        {
            Click();
            SetContinuousMovement(!IsContinuousMovementOn());
        }

        public static bool IsContinuousMovementOn()
        {
            var move = FindFirstObjectByType<ContinuousMoveProvider>(FindObjectsInactive.Include);
            return move != null && move.isActiveAndEnabled;
        }

        public void SetContinuousMovement(bool on)
        {
            var move = FindFirstObjectByType<ContinuousMoveProvider>(FindObjectsInactive.Include);
            if (move == null)
                return;
            move.gameObject.SetActive(on);
            move.enabled = true;
            foreach (var toggle in movementToggles)
            {
                if (toggle != null)
                    toggle.SetIsOnWithoutNotify(on);
            }

            RefreshLabels();
        }

        void RefreshLabels()
        {
            if (controlsButtonLabel != null && controlsPanel != null)
                controlsButtonLabel.text = controlsPanel.activeSelf ? "Hide controls" : "Show controls";
            if (objectivesButtonLabel != null && m_Objectives != null)
                objectivesButtonLabel.text = m_Objectives.IsVisible ? "Hide objectives" : "Show objectives";
            if (movementButtonLabel != null)
                movementButtonLabel.text = IsContinuousMovementOn() ? "Continuous movement: On" : "Continuous movement: Off";
        }

        void Click() => KitAudio.PlayUI(clickClip != null ? clickClip : KitAudio.Click);

        /// <summary>
        /// Button: puts every grabbable object back where it started. Anything currently held (by a hand or a
        /// socket) is released first; sockets then pick their pieces back up because the pieces return inside them.
        /// </summary>
        public void ResetRoom()
        {
            Click();
            foreach (var s in m_Snapshots)
            {
                var grab = s.interactable;
                if (grab == null)
                    continue;

                if (grab.isSelected && grab.interactionManager != null)
                    grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);

                var door = grab.GetComponent<PhysicsDoor>();
                if (door != null)
                {
                    door.ResetDoor();
                    continue;
                }

                grab.transform.SetParent(s.parent, true);
                grab.transform.SetPositionAndRotation(s.position, s.rotation);
                var body = grab.GetComponent<Rigidbody>();
                if (body != null && !body.isKinematic)
                {
#if UNITY_6000_0_OR_NEWER
                    body.linearVelocity = Vector3.zero;
#else
                    body.velocity = Vector3.zero;
#endif
                    body.angularVelocity = Vector3.zero;
                }
            }
        }
    }
}
