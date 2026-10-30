using UnityEngine;

namespace VRAssignment
{
    /// <summary>
    /// Start screen shown in the Start scene. Point a controller ray at a button and pull the trigger.
    /// The panel (Start Menu > Start Panel) is a normal canvas in the hierarchy; each button's On Click() calls
    /// Load("scene name"), ToggleControls() or Quit(), so labels and destinations can be edited in the Inspector.
    /// Inside any room the Room Menu (Y / B) brings you back here.
    /// </summary>
    public class StartMenu : MonoBehaviour
    {
        [Tooltip("Side panel that lists the controls.")]
        public GameObject controlsPanel;
        public AudioClip clickClip;

        void Awake()
        {
            if (controlsPanel != null)
                controlsPanel.SetActive(false);
        }

        /// <summary>Button: load a room by scene name.</summary>
        public void Load(string sceneName)
        {
            Click();
            SceneNavigator.Load(sceneName);
        }

        /// <summary>Button: show or hide the controls panel.</summary>
        public void ToggleControls()
        {
            if (controlsPanel == null)
                return;
            Click();
            controlsPanel.SetActive(!controlsPanel.activeSelf);
        }

        /// <summary>Button: quit the app (stops Play mode in the Editor).</summary>
        public void Quit()
        {
            Click();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Click() => KitAudio.PlayUI(clickClip != null ? clickClip : KitAudio.Click);
    }
}
