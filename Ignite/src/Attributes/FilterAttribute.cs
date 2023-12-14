using Ignite.Contexts;

namespace Ignite.Attributes
{
    /// <summary>
    /// This attribute must be used on classes implementing Ignite <see cref="Systems.ISystem"/> or it's variations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class FilterAttribute : Attribute
    {
        /// <summary>
        /// System will target these types for filtering entities
        /// </summary>
        public Type[] Types { get; init; } = [];

        /// <summary>
        /// Access filter type by system. See <see cref="Context.AccessFilter"/>
        /// </summary>
        public Context.AccessFilter Filter { get; init; } = 
            Context.AccessFilter.AllOf;

        /// <summary>
        /// Access kind type by system. See <see cref="Context.AccessKind"/>
        /// </summary>
        public Context.AccessKind Kind { get; init; } = 
            Context.AccessKind.Read | Context.AccessKind.Write;

        /// <param name="filter">Filter</param>
        /// <param name="kind">Accessability</param>
        /// <param name="types">Targetted types</param>
        public FilterAttribute(
            Context.AccessFilter filter, 
            Context.AccessKind kind, 
            params Type[] types)
        {
            Types = types;
            Filter = filter;
            Kind = kind;
        }

        /// <param name="filter">Filter</param>
        /// <param name="types">Targetted types</param>
        public FilterAttribute(Context.AccessFilter filter, params Type[] types)
            : this(filter, Context.AccessKind.Read | Context.AccessKind.Write, types) { }

        /// <param name="kind">Accessability</param>
        /// <param name="types">Targetted types</param>
        public FilterAttribute(Context.AccessKind kind, params Type[] types)
            : this(Context.AccessFilter.AllOf, kind, types) { }

    }
}
