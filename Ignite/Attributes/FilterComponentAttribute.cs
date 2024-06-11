using Ignite.Components;
using Ignite.Systems;
using System.Diagnostics;

namespace Ignite.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class FilterComponentAttribute : Attribute
    {
        /// <summary>
        /// Type of system filter 
        /// </summary>
        public readonly Context.AccessFilter Filter = Context.AccessFilter.AllOf;

        /// <summary>
        /// Type of system filtered <see cref="IComponent"/> accessibility 
        /// </summary>
        public readonly Context.AccessKind Kind = Context.AccessKind.Read | Context.AccessKind.Write;

        /// <summary>
        /// <see cref="IComponent"/> types filtered by the system
        /// </summary>
        public readonly Type[] Types = Array.Empty<Type>();

        /// <summary>
        /// Create a filter on <see cref="IComponent"/> <see cref="Type"/> for a <see cref="ISystem"/>
        /// </summary>
        /// <param name="filter">Type of system filter</param>
        /// <param name="kind">Type of filter accessibility</param>
        /// <param name="types"><see cref="IComponent"/> <see cref="Type"/>s filtered</param>
        public FilterComponentAttribute(Context.AccessFilter filter, Context.AccessKind kind, params Type[] types)
        {
            Debug.Assert(types.Any(t => typeof(IComponent).IsAssignableFrom(t)), 
                "Why are we requiring a type that is not a component ?");
            (Types, Filter, Kind) = (types, filter, kind);
        }

        /// <summary>
        /// Create a filter on <see cref="IComponent"/> <see cref="Type"/> for a <see cref="ISystem"/>
        /// </summary>
        /// <param name="filter">Type of system filter</param>
        /// <param name="types"><see cref="IComponent"/> <see cref="Type"/>s filtered</param>
        public FilterComponentAttribute(Context.AccessFilter filter, params Type[] types)
            : this(filter, Context.AccessKind.Read | Context.AccessKind.Write, types) { }

        /// <summary>
        /// Create a filter on <see cref="IComponent"/> <see cref="Type"/> for a <see cref="ISystem"/>
        /// </summary>
        /// <param name="kind">Type of filter accessibility</param>
        /// <param name="types"><see cref="IComponent"/> <see cref="Type"/>s filtered</param>
        public FilterComponentAttribute(Context.AccessKind kind, params Type[] types) 
            : this(Context.AccessFilter.AllOf, kind, types) { }

        /// <summary>
        /// Create a filter on <see cref="IComponent"/> <see cref="Type"/> for a <see cref="ISystem"/>
        /// </summary>
        /// <param name="types"><see cref="IComponent"/> <see cref="Type"/>s filtered</param>
        public FilterComponentAttribute(params Type[] types) 
            : this(Context.AccessFilter.AllOf, Context.AccessKind.Read | Context.AccessKind.Write, types) { }
    }
}
