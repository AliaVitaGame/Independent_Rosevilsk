using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameTest
{
    public sealed class CheatCodeRegistry : ICheatCodeRegistry, IDisposable
    {
        private readonly Dictionary<string, List<CheatCodeMeta>> _byCategory = new();
        private readonly List<string> _categories = new();

        public IReadOnlyList<string> Categories => _categories;

        public UniTask WarmUp()
        {
            BuildCache();
            return UniTask.CompletedTask;
        }

        public IReadOnlyList<CheatCodeMeta> GetByCategory(string category)
        {
            return _byCategory.TryGetValue(category, out var list) ? list : Array.Empty<CheatCodeMeta>();
        }

        public void Dispose()
        {
            _byCategory.Clear();
            _categories.Clear();
        }

        private void BuildCache()
        {
            _byCategory.Clear();
            _categories.Clear();

            var allMethods = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(x => !x.IsDynamic && x.GetName().Name == "Assembly-CSharp")
                .SelectMany(GetSafeTypes)
                .SelectMany(GetMarkedMethods)
                .ToList();

            var categories = allMethods
                .GroupBy(x => x.category)
                .Select(group => new
                {
                    Category = group.Key,
                    CategoryOrder = group.Min(x => x.categoryOrder),
                    Methods = group
                        .OrderBy(x => x.order)
                        .ThenBy(x => x.readableName)
                        .ToList()
                })
                .OrderBy(x => x.CategoryOrder)
                .ThenBy(x => x.Category);

            foreach (var categoryGroup in categories)
            {
                var list = new List<CheatCodeMeta>();
                _byCategory[categoryGroup.Category] = list;
                _categories.Add(categoryGroup.Category);

                foreach (var (method, attr, type, readableName, category, order, categoryOrder) in categoryGroup.Methods)
                    list.Add(new CheatCodeMeta(type, method, readableName, category));
            }
        }

        private static IEnumerable<Type> GetSafeTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        private static IEnumerable<(MethodInfo method, CheatCodeAttribute attr, Type type, string readableName, string category, int order, int categoryOrder)> GetMarkedMethods(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var methods = type.GetMethods(flags);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<CheatCodeAttribute>();
                if (attr == null)
                    continue;

                if (method.IsSpecialName || method.GetParameters().Length != 0)
                {
                    Debug.LogWarning($"[CheatCode] Skip {type.Name}.{method.Name}: only parameterless methods are supported.");
                    continue;
                }

                var readableName = string.IsNullOrWhiteSpace(attr.ReadableName) ? method.Name : attr.ReadableName;
                var category = string.IsNullOrWhiteSpace(attr.Category) ? type.Name : attr.Category;
                yield return (method, attr, type, readableName, category, attr.Order, attr.CategoryOrder);
            }
        }
    }
}
