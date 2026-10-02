using System;
using System.Collections.Generic;
using System.Linq;
using Olviia.CodeMap.Core.Model;

namespace Olviia.CodeMap.Core.Resolution
{
    /// <summary>
    /// Finds which project types implement an interface and in which module, so a port can list its adapters.
    /// Matches by short name, like <see cref="OverrideResolver"/>.
    /// </summary>
    public sealed class ImplementerResolver
    {
        private readonly Dictionary<string, List<string>> _implementers = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        /// <param name="modules">Every module with its files.</param>
        public ImplementerResolver(IEnumerable<ModuleFiles> modules)
        {
            foreach (ModuleFiles module in modules)
            {
                foreach (TypeEntry type in module.Files.SelectMany(f => f.Types))
                {
                    foreach (string baseType in type.BaseTypes)
                    {
                        string key = SimpleName(baseType);
                        if (!_implementers.TryGetValue(key, out List<string> list))
                            _implementers[key] = list = new List<string>();
                        string label = type.Name + " (" + module.Module.Name + ")";
                        if (!list.Contains(label))
                            list.Add(label);
                    }
                }
            }
        }

        /// <summary>Types that list the interface as a base type.</summary>
        /// <param name="interfaceName">Interface name as declared, e.g. <c>IBlurDisplay</c>.</param>
        /// <returns>Labels like <c>DepthOfFieldBlurDisplay (Oyung.Vision.Unity)</c>; empty when nothing implements it.</returns>
        public IReadOnlyList<string> ImplementersOf(string interfaceName)
        {
            return _implementers.TryGetValue(SimpleName(interfaceName), out List<string> list) ? list : (IReadOnlyList<string>)new string[0];
        }

        // "Game.IRepository<T>" becomes "IRepository".
        private static string SimpleName(string name)
        {
            int generic = name.IndexOf('<');
            if (generic >= 0)
                name = name.Substring(0, generic);
            int dot = name.LastIndexOf('.');
            return dot >= 0 ? name.Substring(dot + 1) : name;
        }
    }
}
