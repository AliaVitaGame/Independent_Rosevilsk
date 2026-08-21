using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameTest
{
    public class MethodButtonGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private TMP_Dropdown categoryDropdown;
        [SerializeField] private Transform buttonRoot;
        [SerializeField] private GameObject categoryHeaderPrefab;

        private readonly List<GameObject> _spawnedButtons = new();
        private readonly List<GameObject> _spawnedCategoryHeaders = new();
        private readonly Dictionary<Type, object> _targetByType = new();
        private ICheatCodeRegistry _registry;
        private Func<Type, object> _diResolver;

        public void Configure(GameObject runtimeButtonPrefab, TMP_Dropdown runtimeCategoryDropdown, Transform runtimeButtonRoot, GameObject runtimeCategoryHeaderPrefab = null)
        {
            buttonPrefab = runtimeButtonPrefab;
            categoryDropdown = runtimeCategoryDropdown;
            buttonRoot = runtimeButtonRoot;
            categoryHeaderPrefab = runtimeCategoryHeaderPrefab;
        }

        public void GenerateMethodButtons(ICheatCodeRegistry registry, MonoBehaviour[] methodTargets, Func<Type, object> diResolver = null)
        {
            _registry = registry;
            _diResolver = diResolver;
            BuildTargetMap(methodTargets);
            SetupDropdown();
            RebuildButtonsForSelectedCategory();
            Debug.Log($"[CheatCode] Generated {_spawnedButtons.Count} runtime cheat buttons.");
        }

        private void BuildTargetMap(IEnumerable<MonoBehaviour> methodTargets)
        {
            _targetByType.Clear();

            foreach (var target in methodTargets)
            {
                if (target == null)
                    continue;

                var type = target.GetType();
                if (!_targetByType.ContainsKey(type))
                    _targetByType[type] = target;
            }
        }

        private void SetupDropdown()
        {
            if (categoryDropdown == null || _registry == null)
                return;

            categoryDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            categoryDropdown.ClearOptions();
            categoryDropdown.AddOptions(new List<string>(_registry.Categories));
            categoryDropdown.onValueChanged.AddListener(OnDropdownChanged);
            categoryDropdown.SetValueWithoutNotify(0);
        }

        private void OnDropdownChanged(int _)
        {
            RebuildButtonsForSelectedCategory();
        }

        private void RebuildButtonsForSelectedCategory()
        {
            ClearButtons();

            if (_registry == null || _registry.Categories.Count == 0)
            {
                Debug.LogWarning("[CheatCode] No categories found. Check that methods have [CheatCode] attributes.");
                return;
            }

            if (categoryDropdown == null)
            {
                foreach (var category in _registry.Categories)
                {
                    SpawnCategoryHeader(category);
                    var cheatsInCategory = _registry.GetByCategory(category);
                    foreach (var cheat in cheatsInCategory)
                        SpawnButton(cheat);
                }

                RefreshLayout();
                return;
            }

            var selectedCategory = categoryDropdown != null
                ? _registry.Categories[Mathf.Clamp(categoryDropdown.value, 0, _registry.Categories.Count - 1)]
                : _registry.Categories[0];

            var cheats = _registry.GetByCategory(selectedCategory);
            foreach (var cheat in cheats)
            {
                SpawnButton(cheat);
            }

            RefreshLayout();
        }

        private void SpawnButton(CheatCodeMeta cheat)
        {
            if (buttonRoot == null || buttonPrefab == null)
            {
                Debug.LogWarning($"[CheatCode] Cannot spawn button for {cheat.ReadableName}: button root or prefab is missing.");
                return;
            }

            var instance = Instantiate(buttonPrefab, buttonRoot);
            instance.SetActive(true);
            ConfigureButtonLayout(instance);
            _spawnedButtons.Add(instance);

            var label = instance.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = cheat.ReadableName;

            var button = instance.GetComponent<Button>();
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => InvokeCheat(cheat));
        }

        private static void ConfigureButtonLayout(GameObject instance)
        {
            var rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, 34f);
            }

            var layout = instance.GetComponent<LayoutElement>();
            if (layout == null)
                layout = instance.AddComponent<LayoutElement>();

            layout.minWidth = 100f;
            layout.minHeight = 34f;
            layout.preferredHeight = 34f;
            layout.flexibleWidth = 1f;
        }

        private void RefreshLayout()
        {
            if (buttonRoot is not RectTransform rect)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Canvas.ForceUpdateCanvases();
        }

        private void InvokeCheat(CheatCodeMeta cheat)
        {
            try
            {
                object target = null;
                if (!cheat.Method.IsStatic)
                {
                    target = ResolveTarget(cheat.DeclaringType);
                    if (target == null)
                    {
                        Debug.LogWarning($"[CheatCode] No target instance for {cheat.DeclaringType.Name}.{cheat.Method.Name}");
                        return;
                    }
                }

                cheat.Method.Invoke(target, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CheatCode] Failed to execute {cheat.DeclaringType.Name}.{cheat.Method.Name}: {e}");
            }
        }

        private void ClearButtons()
        {
            for (var i = 0; i < _spawnedCategoryHeaders.Count; i++)
            {
                var go = _spawnedCategoryHeaders[i];
                if (go != null)
                    Destroy(go);
            }

            _spawnedCategoryHeaders.Clear();

            for (var i = 0; i < _spawnedButtons.Count; i++)
            {
                var go = _spawnedButtons[i];
                if (go != null)
                    Destroy(go);
            }

            _spawnedButtons.Clear();
        }

        private object ResolveTarget(Type declaringType)
        {
            if (_targetByType.TryGetValue(declaringType, out var exact))
                return exact;

            var assignable = _targetByType
                .Where(x => declaringType.IsAssignableFrom(x.Key))
                .Select(x => x.Value)
                .FirstOrDefault();
            if (assignable != null)
                return assignable;

            return _diResolver?.Invoke(declaringType);
        }

        private void SpawnCategoryHeader(string category)
        {
            if (buttonRoot == null)
                return;

            if (categoryHeaderPrefab != null)
            {
                var instance = Instantiate(categoryHeaderPrefab, buttonRoot);
                instance.SetActive(true);
                var label = instance.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = category;
                _spawnedCategoryHeaders.Add(instance);
                return;
            }

            var headerObject = new GameObject($"Category_{category}", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            headerObject.transform.SetParent(buttonRoot, false);
            _spawnedCategoryHeaders.Add(headerObject);

            var layout = headerObject.GetComponent<LayoutElement>();
            layout.minHeight = 28f;

            var text = headerObject.GetComponent<TextMeshProUGUI>();
            text.text = category;
            text.fontSize = 20f;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.65f, 1f, 0.65f, 1f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.margin = new Vector4(6f, 0f, 0f, 0f);
        }
    }
}
