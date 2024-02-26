namespace Ignite.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public class RequireSystemAttribute : Attribute
    {
        public Type[] Types = Array.Empty<Type>();
        
        public RequireSystemAttribute(params Type[] types) 
        {
            Types = types;
        }
    }
}
