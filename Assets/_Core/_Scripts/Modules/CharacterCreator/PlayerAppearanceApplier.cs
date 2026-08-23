using System;
using CC;
using HEAVYART.TopDownShooter.Netcode;
using UnityEngine;

namespace Modules.CharacterCreator
{
    /// <summary>
    /// Replaces Kyle as the gameplay body with a CharacterCustomizer humanoid.
    /// Same Humanoid FPS clips play on the CC avatar; weapons move to CC hands.
    /// </summary>
    public static class PlayerAppearanceApplier
    {
        public static void ApplyToLocalPlayer(CC_CharacterData appearance)
        {
            if (appearance == null || string.IsNullOrEmpty(appearance.CharacterPrefab))
            {
                Debug.LogWarning("[Appearance] Missing appearance or CharacterPrefab.");
                return;
            }

            var player = FindLocalPlayer();
            if (player == null)
            {
                Debug.LogWarning("[Appearance] Local player not found; cannot apply character.");
                return;
            }

            ApplyToHost(player.transform, appearance);
        }

        public static void ApplyToHost(Transform host, CC_CharacterData appearance)
        {
            if (host == null || appearance == null)
                return;

            var originalModel = host.Find("Model");
            var gameplayAnimator = originalModel != null
                ? originalModel.GetComponent<Animator>()
                : host.GetComponentInChildren<Animator>();

            if (gameplayAnimator == null)
            {
                Debug.LogWarning("[Appearance] Gameplay Animator missing on player Model.");
                return;
            }

            RescueWeaponsOntoGameplayHands(host, gameplayAnimator);
            StripBinders(host);

            var existing = FindAppearanceRoot(host);
            if (IsAppearanceAlreadyApplied(existing, appearance.CharacterPrefab))
                return;

            if (existing != null)
            {
                existing.name = "AppearanceRoot_OLD";
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var prefabName = appearance.CharacterPrefab;
            var prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                ShowGameplayBodyMeshes(originalModel != null ? originalModel : host);
                gameplayAnimator.enabled = true;
                Debug.LogWarning($"[Appearance] Resources prefab '{prefabName}' not found; keeping Kyle.");
                return;
            }

            HideGameplayBodyMeshes(originalModel != null ? originalModel : host);

            var root = new GameObject("AppearanceRoot");
            root.transform.SetParent(host, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var instance = UnityEngine.Object.Instantiate(prefab, root.transform);
            instance.name = prefabName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var customization = instance.GetComponent<CharacterCustomization>()
                                ?? instance.GetComponentInChildren<CharacterCustomization>();
            if (customization != null)
            {
                customization.Autoload = false;
                customization.UI = null;
                customization.CharacterName = string.IsNullOrEmpty(appearance.CharacterName)
                    ? prefabName
                    : appearance.CharacterName;
                customization.Initialize(CloneAppearance(appearance));
            }

            PreferHighestDetailMeshes(instance);

            var bodyAnimator = instance.GetComponentInChildren<Animator>();
            if (bodyAnimator == null)
            {
                Debug.LogWarning("[Appearance] Custom body has no Animator.");
                ShowGameplayBodyMeshes(originalModel != null ? originalModel : host);
                return;
            }

            foreach (var ik in bodyAnimator.GetComponents<CharacterIKController>())
                UnityEngine.Object.DestroyImmediate(ik);

            foreach (var ik in gameplayAnimator.GetComponents<CharacterIKController>())
                ik.enabled = false;

            var controller = gameplayAnimator.runtimeAnimatorController;
            bodyAnimator.runtimeAnimatorController = controller;
            bodyAnimator.applyRootMotion = false;
            bodyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            bodyAnimator.updateMode = gameplayAnimator.updateMode;
            bodyAnimator.enabled = true;
            bodyAnimator.Rebind();
            bodyAnimator.Update(0f);

            gameplayAnimator.applyRootMotion = false;
            gameplayAnimator.enabled = false;

            BindWeaponsToVisualHands(host, bodyAnimator);

            var animationController = host.GetComponent<CharacterAnimationController>();
            if (animationController != null)
                animationController.RebindToAnimator(bodyAnimator);

            var weaponSystem = host.GetComponent<WeaponControlSystem>();
            if (weaponSystem != null)
                weaponSystem.ReapplySelectedWeaponVisuals();

            Debug.Log($"[Appearance] Replaced Kyle with CharacterCustomizer '{prefabName}' on {host.name}.");
        }

        private static void BindWeaponsToVisualHands(Transform host, Animator bodyAnimator)
        {
            if (host == null || bodyAnimator == null)
                return;

            BindWeaponSocket(host, "RightHandWeapons", bodyAnimator.GetBoneTransform(HumanBodyBones.RightHand));
            BindWeaponSocket(host, "LeftHandWeapons", bodyAnimator.GetBoneTransform(HumanBodyBones.LeftHand));
        }

        private static void BindWeaponSocket(
            Transform host,
            string socketName,
            Transform visualHand)
        {
            if (visualHand == null)
                return;

            Transform socket = null;
            foreach (var child in host.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.name == socketName)
                {
                    socket = child;
                    break;
                }
            }

            if (socket == null)
                return;

            // Kyle wrist offsets are in a different bone space than CharacterCustomizer hand_r.
            socket.SetParent(visualHand, false);
            socket.localPosition = Vector3.zero;
            socket.localRotation = Quaternion.identity;
        }

        private static void RescueWeaponsOntoGameplayHands(Transform host, Animator gameplayAnimator)
        {
            if (host == null || gameplayAnimator == null)
                return;

            var rightHand = gameplayAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            var leftHand = gameplayAnimator.GetBoneTransform(HumanBodyBones.LeftHand);

            foreach (var child in host.GetComponentsInChildren<Transform>(true))
            {
                if (child == null)
                    continue;

                if (child.name == "RightHandWeapons" && rightHand != null && child.parent != rightHand)
                    child.SetParent(rightHand, true);
                else if (child.name == "LeftHandWeapons" && leftHand != null && child.parent != leftHand)
                    child.SetParent(leftHand, true);
            }
        }

        private static void StripBinders(Transform host)
        {
            foreach (var binder in host.GetComponentsInChildren<WeaponHandBinder>(true))
            {
                if (binder != null)
                    UnityEngine.Object.DestroyImmediate(binder);
            }
        }

        private static void HideGameplayBodyMeshes(Transform modelRoot)
        {
            if (modelRoot == null)
                return;

            foreach (var smr in modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                smr.enabled = false;
        }

        private static void ShowGameplayBodyMeshes(Transform modelRoot)
        {
            if (modelRoot == null)
                return;

            foreach (var smr in modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                smr.enabled = true;
        }

        private static void PreferHighestDetailMeshes(GameObject instance)
        {
            foreach (var lodGroup in instance.GetComponentsInChildren<LODGroup>(true))
                lodGroup.enabled = false;

            foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var name = smr.name;
                if (name.IndexOf("LOD1", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("LOD2", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("LOD3", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    smr.enabled = false;
                }
            }
        }

        private static bool IsAppearanceAlreadyApplied(Transform appearanceRoot, string prefabName)
        {
            if (appearanceRoot == null || string.IsNullOrEmpty(prefabName))
                return false;

            Transform instance = null;
            for (var i = 0; i < appearanceRoot.childCount; i++)
            {
                var child = appearanceRoot.GetChild(i);
                if (child != null && child.name == prefabName)
                {
                    instance = child;
                    break;
                }
            }

            if (instance == null)
                return false;

            var bodyAnimator = instance.GetComponentInChildren<Animator>();
            return bodyAnimator != null
                   && bodyAnimator.enabled
                   && bodyAnimator.runtimeAnimatorController != null;
        }

        private static CC_CharacterData CloneAppearance(CC_CharacterData source) =>
            JsonUtility.FromJson<CC_CharacterData>(JsonUtility.ToJson(source));

        private static PlayerBehaviour FindLocalPlayer()
        {
            var players = UnityEngine.Object.FindObjectsByType<PlayerBehaviour>(FindObjectsInactive.Exclude);
            foreach (var player in players)
            {
                if (player != null && player.IsOwner)
                    return player;
            }

            if (players != null && players.Length > 0 && players[0] != null)
                return players[0];

            return null;
        }

        private static Transform FindAppearanceRoot(Transform host)
        {
            if (host == null)
                return null;

            var direct = host.Find("AppearanceRoot");
            if (direct != null)
                return direct;

            var model = host.Find("Model");
            return model != null ? model.Find("AppearanceRoot") : null;
        }
    }
}
