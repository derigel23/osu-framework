// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using osu.Framework.Graphics;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// An attribute that may be attached to a class definitions or fields of a <see cref="Drawable"/> to indicate that the value should be cached as a dependency.
    /// Cached values may be resolved through <see cref="BackgroundDependencyLoaderAttribute"/> or <see cref="ResolvedAttribute"/>.
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public class CachedAttribute : Attribute
    {
        private const BindingFlags activator_flags = BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// The type to cache the value as. If null, the value will be cached as the value's most derived type.
        /// </summary>
        /// <example>
        /// For example, if this value is null on the following field definition:
        ///
        /// private BaseType obj = new DerivedType()
        ///
        /// Then the cached type will be "BaseType".
        /// </example>
        public Type Type;

        internal static CacheDependencyDelegate CreateActivator(Type type)
        {
            var additionActivators = new List<Action<object, DependencyContainer>>();

            foreach (var attribute in type.GetCustomAttributes<CachedAttribute>())
                additionActivators.Add((target, dc) => dc.CacheAs(attribute.Type ?? type, target));

            foreach (var field in type.GetFields(activator_flags).Where(f => f.GetCustomAttributes<CachedAttribute>().Any()))
            foreach (var attribute in field.GetCustomAttributes<CachedAttribute>())
            {
                additionActivators.Add((target, dc) =>
                {
                    var value = field.GetValue(target);
                    dc.CacheAs(attribute.Type ?? value.GetType(), value);
                });
            }

            if (additionActivators.Count == 0)
                return (_, existing) => existing;

            return (target, existing) =>
            {
                var dependencies = new DependencyContainer(existing);
                additionActivators.ForEach(a => a(target, dependencies));

                return dependencies;
            };
        }
    }
}
