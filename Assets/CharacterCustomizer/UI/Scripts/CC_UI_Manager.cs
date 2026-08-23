using System.Collections.Generic;
using UnityEngine;

namespace CC
{
    public class CC_UI_Manager : MonoBehaviour
    {
        public static CC_UI_Manager instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        [Tooltip("The parent object of your customizable characters")]
        public GameObject CharacterParent;

        public List<AudioClip> UISounds = new List<AudioClip>();

        private int characterIndex = 0;
        private bool _hasExternalSelection;

        public void Start()
        {
            // Menu / host may already call SetActiveCharacter before Start.
            // Do not wipe that selection back to Dummy (index 0).
            if (_hasExternalSelection)
                return;

            if (CharacterParent == null || CharacterParent.transform.childCount == 0)
                return;

            // Prefer first active child, otherwise first human-looking character, else 0.
            var preferred = FindPreferredDefaultIndex();
            SetActiveCharacter(preferred);
        }

        public void playUIAudio(int Index)
        {
            var audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource && UISounds.Count > Index) audioSource.clip = UISounds[Index]; audioSource.Play();
        }

        public void SetActiveCharacter(int i)
        {
            if (CharacterParent == null)
                return;

            _hasExternalSelection = true;
            characterIndex = Mathf.Clamp(i, 0, Mathf.Max(0, CharacterParent.transform.childCount - 1));

            for (int j = 0; j < CharacterParent.transform.childCount; j++)
            {
                var character = CharacterParent.transform.GetChild(j).gameObject;
                var customization = character.GetComponent<CharacterCustomization>();
                var isActive = j == characterIndex;

                character.SetActive(isActive);
                if (customization != null && customization.UI != null)
                    customization.UI.SetActive(isActive);
            }
        }

        public void characterNext()
        {
            if (CharacterParent == null || CharacterParent.transform.childCount == 0)
                return;

            var next = characterIndex;
            for (var step = 0; step < CharacterParent.transform.childCount; step++)
            {
                next = next == CharacterParent.transform.childCount - 1 ? 0 : next + 1;
                if (IsSelectableCharacter(CharacterParent.transform.GetChild(next)))
                {
                    SetActiveCharacter(next);
                    return;
                }
            }
        }

        public void characterPrev()
        {
            if (CharacterParent == null || CharacterParent.transform.childCount == 0)
                return;

            var prev = characterIndex;
            for (var step = 0; step < CharacterParent.transform.childCount; step++)
            {
                prev = prev == 0 ? CharacterParent.transform.childCount - 1 : prev - 1;
                if (IsSelectableCharacter(CharacterParent.transform.GetChild(prev)))
                {
                    SetActiveCharacter(prev);
                    return;
                }
            }
        }

        private int FindPreferredDefaultIndex()
        {
            for (int i = 0; i < CharacterParent.transform.childCount; i++)
            {
                var child = CharacterParent.transform.GetChild(i);
                var name = child.name;
                var hasFemale = name.IndexOf("Female", System.StringComparison.OrdinalIgnoreCase) >= 0;
                var hasMale = name.IndexOf("Male", System.StringComparison.OrdinalIgnoreCase) >= 0;
                // "Female" contains substring "Male" — require Male without Female.
                if (hasMale && !hasFemale)
                    return i;
            }

            for (int i = 0; i < CharacterParent.transform.childCount; i++)
            {
                if (IsSelectableCharacter(CharacterParent.transform.GetChild(i)))
                    return i;
            }

            return 0;
        }

        private static bool IsSelectableCharacter(Transform character)
        {
            if (character == null)
                return false;

            // Skip Dummy mannequin in menu flow — its UI is world-space and easy to miss.
            if (character.name.IndexOf("Dummy", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return character.GetComponent<CharacterCustomization>() != null;
        }
    }
}
