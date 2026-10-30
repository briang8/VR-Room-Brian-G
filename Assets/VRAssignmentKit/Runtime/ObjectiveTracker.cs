using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRAssignment
{
    /// <summary>
    /// Small checklist shown above the left wrist. Each objective is checked a few times per second and ticks
    /// off once done; when all are done a success sound plays. Objectives are listed in the Inspector, and the
    /// panel (Objectives Panel under the left controller) is a normal canvas that can be resized by hand.
    /// </summary>
    public class ObjectiveTracker : MonoBehaviour
    {
        public enum Kind { OpenDoor, FillSocket, MoveChessPiece, VisitPlaces, HoldObject }

        [Serializable]
        public class Objective
        {
            public string label;
            public Kind kind;
            public PhysicsDoor door;
            public XRSocketInteractor socket;
            public ChessBoard chessBoard;
            public Transform[] places = Array.Empty<Transform>();
            public XRGrabInteractable target;
            [NonSerialized] public bool done;
            [NonSerialized] public bool[] visited;
        }

        public List<Objective> objectives = new List<Objective>();

        [Header("Scene objects")]
        [Tooltip("The panel canvas (a child of the left controller, so it sits above the wrist).")]
        public GameObject panel;
        public TMP_Text titleText;
        public TMP_Text listText;

        [Header("Sounds")]
        public AudioClip completeClip;
        public AudioClip tickClip;

        float m_NextCheck;
        bool m_AllDone;

        public bool IsVisible => panel != null && panel.activeSelf;

        void Awake()
        {
            foreach (var o in objectives)
                o.visited = new bool[o.places != null ? o.places.Length : 0];
            Refresh();
        }

        public void SetVisible(bool visible)
        {
            if (panel != null)
                panel.SetActive(visible);
        }

        void Update()
        {
            if (Time.time < m_NextCheck)
                return;
            m_NextCheck = Time.time + 0.2f;

            var changed = false;
            foreach (var o in objectives)
            {
                if (o.done)
                    continue;
                if (Check(o))
                {
                    o.done = true;
                    changed = true;
                    KitAudio.PlayUI(tickClip != null ? tickClip : KitAudio.Snap, 0.35f);
                }
            }

            if (changed)
                Refresh();
        }

        bool Check(Objective o)
        {
            switch (o.kind)
            {
                case Kind.OpenDoor:
                    return o.door != null && o.door.MaxAngleReached > 45f;
                case Kind.FillSocket:
                    return o.socket != null && o.socket.hasSelection;
                case Kind.MoveChessPiece:
                    return o.chessBoard != null && o.chessBoard.PiecesMoved > 0;
                case Kind.HoldObject:
                    return o.target != null && o.target.isSelected && !(o.target.firstInteractorSelecting is XRSocketInteractor);
                case Kind.VisitPlaces:
                    var cam = Camera.main;
                    if (cam == null || o.places == null || o.places.Length == 0)
                        return false;
                    var all = true;
                    for (var i = 0; i < o.places.Length; i++)
                    {
                        if (o.places[i] == null)
                            continue;
                        var offset = cam.transform.position - o.places[i].position;
                        offset.y = 0f;
                        if (offset.magnitude < 0.8f && !o.visited[i])
                        {
                            o.visited[i] = true;
                            Refresh();
                        }
                        all &= o.visited[i];
                    }
                    return all;
            }

            return false;
        }

        void Refresh()
        {
            if (listText == null)
                return;

            var doneCount = 0;
            var sb = new StringBuilder();
            foreach (var o in objectives)
            {
                var label = o.label;
                if (o.kind == Kind.VisitPlaces && o.visited != null && o.visited.Length > 0 && !o.done)
                {
                    var visited = 0;
                    foreach (var v in o.visited)
                        visited += v ? 1 : 0;
                    label += $" ({visited}/{o.visited.Length})";
                }

                if (o.done)
                {
                    doneCount++;
                    sb.Append("<color=#6FCF8E>[x]</color>  <color=#9AA0A6>").Append(label).Append("</color>\n");
                }
                else
                {
                    sb.Append("<color=#9AA0A6>[  ]</color>  ").Append(label).Append('\n');
                }
            }

            listText.text = sb.ToString().TrimEnd('\n');
            if (titleText != null)
                titleText.text = $"Objectives  {doneCount}/{objectives.Count}";

            if (!m_AllDone && objectives.Count > 0 && doneCount == objectives.Count)
            {
                m_AllDone = true;
                if (titleText != null)
                    titleText.text = "All objectives done";
                KitAudio.PlayUI(completeClip != null ? completeClip : KitAudio.Success, 0.6f);
            }
        }
    }
}
