using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace VRAssignment
{
    /// <summary>
    /// Loads scenes with a short fade to black. Lives on a DontDestroyOnLoad object created on first use.
    ///
    /// Every scene has its own XR Origin, XR Interaction Manager, Input Action Manager and EventSystem, and
    /// scenes are loaded in Single mode, so the old rig is destroyed and the new one starts fresh. After the
    /// load we re-enable every InputActionManager: the old scene's manager disables the shared XRI input
    /// action asset when it is destroyed, which could otherwise leave the new rig without controller input.
    /// </summary>
    public class SceneNavigator : MonoBehaviour
    {
        const float k_FadeTime = 0.35f;

        // Other names a scene has had. If a button asks for a name that is not in Build Settings,
        // the other names in the same group are tried, so travel keeps working before and after a rename.
        static readonly string[][] k_SceneAliases =
        {
            new[] { "VR Room - Brian G", "Create with VR Room", "Brian_Gatanazi Room" },
        };

        static SceneNavigator s_Instance;

        Canvas m_FadeCanvas;
        CanvasGroup m_FadeGroup;

        public static bool IsLoading { get; private set; }

        public static void Load(string sceneName)
        {
            if (IsLoading || string.IsNullOrEmpty(sceneName))
                return;

            var resolved = Resolve(sceneName);
            if (resolved == null)
            {
                Debug.LogError($"Scene '{sceneName}' is not in Build Settings. Add it under File > Build Profiles > Scene List.");
                return;
            }

            Instance.StartCoroutine(Instance.LoadRoutine(resolved));
        }

        public static void ReloadCurrent() => Load(SceneManager.GetActiveScene().name);

        /// <summary>True if the named scene (or one of its other names) is the scene that is open now.</summary>
        public static bool IsCurrent(string sceneName)
        {
            var active = SceneManager.GetActiveScene().name;
            if (active == sceneName)
                return true;
            foreach (var group in k_SceneAliases)
            {
                if (System.Array.IndexOf(group, sceneName) >= 0 && System.Array.IndexOf(group, active) >= 0)
                    return true;
            }

            return false;
        }

        static string Resolve(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
                return sceneName;
            foreach (var group in k_SceneAliases)
            {
                if (System.Array.IndexOf(group, sceneName) < 0)
                    continue;
                foreach (var alias in group)
                {
                    if (Application.CanStreamedLevelBeLoaded(alias))
                        return alias;
                }
            }

            return null;
        }

        static SceneNavigator Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var go = new GameObject("[Scene Navigator]");
                    DontDestroyOnLoad(go);
                    s_Instance = go.AddComponent<SceneNavigator>();
                    s_Instance.CreateFade();
                }

                return s_Instance;
            }
        }

        void CreateFade()
        {
            var go = new GameObject("Fade", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            m_FadeCanvas = go.AddComponent<Canvas>();
            m_FadeCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            m_FadeCanvas.planeDistance = 0.12f;
            m_FadeCanvas.sortingOrder = 32000;
            m_FadeGroup = go.AddComponent<CanvasGroup>();
            m_FadeGroup.alpha = 0f;
            m_FadeGroup.blocksRaycasts = false;
            m_FadeGroup.interactable = false;

            var image = new GameObject("Black", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(go.transform, false);
            image.GetComponent<Image>().color = Color.black;
            image.GetComponent<Image>().raycastTarget = false;
            KitUI.Stretch((RectTransform)image.transform);
            go.SetActive(false);
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            yield return Fade(0f, 1f);

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!op.isDone)
                yield return null;

            // Give the new scene one frame to run Awake/OnEnable, then make sure XR input is live.
            yield return null;
            foreach (var manager in FindObjectsByType<InputActionManager>(FindObjectsSortMode.None))
                manager.EnableInput();

            yield return Fade(1f, 0f);
            IsLoading = false;
        }

        IEnumerator Fade(float from, float to)
        {
            m_FadeCanvas.gameObject.SetActive(true);
            for (var t = 0f; t < k_FadeTime; t += Time.unscaledDeltaTime)
            {
                m_FadeCanvas.worldCamera = Camera.main;
                m_FadeGroup.alpha = Mathf.Lerp(from, to, t / k_FadeTime);
                yield return null;
            }

            m_FadeCanvas.worldCamera = Camera.main;
            m_FadeGroup.alpha = to;
            if (to <= 0f)
                m_FadeCanvas.gameObject.SetActive(false);
        }
    }
}
