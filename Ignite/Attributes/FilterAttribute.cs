using Ignite.Systems;

namespace Ignite.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FilterAttribute : Attribute
    {
        public readonly Context.AccessFilter Filter = Context.AccessFilter.AllOf;
        public readonly Context.AccessKind Kind = Context.AccessKind.Read | Context.AccessKind.Write;
        public readonly Type[] Types = Array.Empty<Type>();

        public FilterAttribute(Context.AccessFilter filter, Context.AccessKind kind, params Type[] types)
        {
            (Types, Filter, Kind) = (types, filter, kind);
        }

        public FilterAttribute(Context.AccessFilter filter, params Type[] types)
            : this(filter, Context.AccessKind.Read | Context.AccessKind.Write, types) { }
        public FilterAttribute(Context.AccessKind kind, params Type[] types) 
            : this(Context.AccessFilter.AllOf, kind, types) { }
        public FilterAttribute(params Type[] types) 
            : this(Context.AccessFilter.AllOf, Context.AccessKind.Read | Context.AccessKind.Write, types) { }
    }
}
