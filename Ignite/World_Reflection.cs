using Ignite.Attributes;
using Ignite.Components;
using Ignite.Systems;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ignite
{
    public partial class World
    {
        /// <summary>
        /// Check whether the system have the <see cref="IgnorePauseAttribute"/> or not
        /// </summary>
        private bool DoSystemIgnorePause(ISystem system)
        {
            return Attribute.IsDefined(system.GetType(), typeof(IgnorePauseAttribute));
        }

        /// <summary>
        /// Check whether the system can pause, meaning being a <see cref="IUpdateSystem"/> or having the <see cref="IgnorePauseAttribute"/>
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        private bool CanSystemPause(ISystem system)
        {
            return system is IUpdateSystem && !DoSystemIgnorePause(system);
        }

        /// <summary>
        /// Cache the lookup implementation for this game.
        /// </summary>
        private static Type? _cachedLookupImplementation = null;

        private ComponentLookupTable FindLookupTableImplementation()
        {
            if (_cachedLookupImplementation is null)
            {
                Type lookup = typeof(ComponentLookupTable);

                var isLookup = (Type t) => !t.IsInterface && !t.IsAbstract && lookup.IsAssignableFrom(t);

                // We might find more than one lookup implementation, when inheriting projects with a generator.
                List<Type> candidateLookupImplementations = new();

                Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly s in allAssemblies)
                {
                    foreach (Type t in s.GetTypes())
                    {
                        if (isLookup(t))
                        {
                            candidateLookupImplementations.Add(t);
                        }
                    }
                }

                _cachedLookupImplementation = candidateLookupImplementations.MaxBy(NumberOfParentClasses);
            }

            if (_cachedLookupImplementation is not null)
            {
                return (ComponentLookupTable)Activator.CreateInstance(_cachedLookupImplementation)!;
            }

            throw new InvalidOperationException("A generator is required to be run before running the game!");

            static int NumberOfParentClasses(Type type)
                => type.BaseType is null ? 0 : 1 + NumberOfParentClasses(type.BaseType);
        }
    }
}
