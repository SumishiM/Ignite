using Ignite.Attributes;
using Ignite.Components;
using Ignite.Systems;
using System.Reflection;

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
        private static Type? _cachedLookupTableImplementation = null;

        /// <summary>
        /// Find the best lookup table implementation in projects using Ignite
        /// </summary>
        private ComponentLookupTable FindLookupTableImplementation()
        {
            if (_cachedLookupTableImplementation is null)
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

                _cachedLookupTableImplementation = candidateLookupImplementations.MaxBy(NumberOfParentClasses);
            }

            if (_cachedLookupTableImplementation is not null)
            {
                return (ComponentLookupTable)Activator.CreateInstance(_cachedLookupTableImplementation)!;
            }

            throw new InvalidOperationException("A generator is required to be run before running the game!");

            static int NumberOfParentClasses(Type type)
                => type.BaseType is null ? 0 : 1 + NumberOfParentClasses(type.BaseType);
        }
    }
}
