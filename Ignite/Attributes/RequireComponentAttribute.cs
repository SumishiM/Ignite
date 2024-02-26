using Ignite.Components;
using System.Collections.Immutable;

namespace Ignite.Attributes
{
    /// <summary>
    /// Attribute that tell the Ignite what <see cref="Ignite.Components.IComponent"/>
    /// another <see cref="Ignite.Components.IComponent"/> require.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireComponentAttribute : Attribute
    {
        /// <summary>
        /// Types the system will use on node creation
        /// </summary>
        public readonly Type[] Types = Array.Empty<Type>();
        /// <summary>
        /// Instantiated required components 
        /// </summary>
        internal ImmutableDictionary<Type, IComponent> Components = ImmutableDictionary.Create<Type, IComponent>();

        /// <param name="types">Types required for the <see cref="Ignite.Node"/></param>
        public RequireComponentAttribute(params Type[] types) 
        {
            Types = types;
        }
    }
}
