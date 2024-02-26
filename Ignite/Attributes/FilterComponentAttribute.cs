using Ignite.Components;
using Ignite.Systems;
using System.Diagnostics;

namespace Ignite.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class FilterComponentAttribute : Attribute
    {
        public readonly Context.AccessFilter Filter = Context.AccessFilter.AllOf;
        public readonly Context.AccessKind Kind = Context.AccessKind.Read | Context.AccessKind.Write;
        public readonly Type[] Types = Array.Empty<Type>();

        public FilterComponentAttribute(Context.AccessFilter filter, Context.AccessKind kind, params Type[] types)
        {
            Debug.Assert(types.Any(t => typeof(IComponent).IsAssignableFrom(t)), 
                "Why are we requiring a type that is not a component ?");
            (Types, Filter, Kind) = (types, filter, kind);
        }

        public FilterComponentAttribute(Context.AccessFilter filter, params Type[] types)
            : this(filter, Context.AccessKind.Read | Context.AccessKind.Write, types) { }
        public FilterComponentAttribute(Context.AccessKind kind, params Type[] types) 
            : this(Context.AccessFilter.AllOf, kind, types) { }
        public FilterComponentAttribute(params Type[] types) 
            : this(Context.AccessFilter.AllOf, Context.AccessKind.Read | Context.AccessKind.Write, types) { }
    }
}
