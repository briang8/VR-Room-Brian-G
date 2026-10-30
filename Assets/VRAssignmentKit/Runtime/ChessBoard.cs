using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRAssignment
{
    /// <summary>
    /// 8x8 grid of XR Socket Interactors, one per square. Each socket's Interaction Layer Mask is set to the
    /// "Chess" layer only, and every chess piece's XRGrabInteractable is on "Default" + "Chess", so hands can
    /// grab pieces, pieces snap onto squares, and nothing else (boxes, rope, flashlight) can be placed on the
    /// board. Pieces start already seated in their squares and keep their original facing when moved.
    /// </summary>
    public class ChessBoard : MonoBehaviour
    {
        public List<XRSocketInteractor> squares = new List<XRSocketInteractor>();
        public AudioClip placeClip;

        readonly Dictionary<XRSocketInteractor, IXRSelectInteractable> m_StartPieces = new Dictionary<XRSocketInteractor, IXRSelectInteractable>();
        bool m_Ready;

        /// <summary>Number of squares holding a different piece than at the start.</summary>
        public int PiecesMoved { get; private set; }

        void Start()
        {
            foreach (var square in squares)
            {
                if (square == null)
                    continue;
                m_StartPieces[square] = square.startingSelectedInteractable;
                square.selectEntered.AddListener(OnPlaced);
                square.selectExited.AddListener(OnLifted);
            }

            m_Ready = true;
        }

        void OnDestroy()
        {
            foreach (var square in squares)
            {
                if (square == null)
                    continue;
                square.selectEntered.RemoveListener(OnPlaced);
                square.selectExited.RemoveListener(OnLifted);
            }
        }

        void OnPlaced(SelectEnterEventArgs args)
        {
            if (!m_Ready)
                return;
            KitAudio.PlayAt(placeClip != null ? placeClip : KitAudio.Snap, args.interactorObject.transform.position, 0.4f);
            Recount();
        }

        void OnLifted(SelectExitEventArgs args)
        {
            if (m_Ready)
                Recount();
        }

        void Recount()
        {
            var moved = 0;
            foreach (var square in squares)
            {
                if (square == null || !square.hasSelection)
                    continue;
                m_StartPieces.TryGetValue(square, out var start);
                if ((Object)square.interactablesSelected[0] != (Object)start)
                    moved++;
            }

            PiecesMoved = Mathf.Max(PiecesMoved, moved);
        }
    }
}
