namespace Ignite.Attributes
{
    /// <summary>
    /// Specifies the required <see cref="Ignite.Systems.ISystem"/> types
    /// required for a <see cref="Ignite.Systems.ISystem"/> to work properly.
    /// <para>This attribute must be used on <see cref="Ignite.Systems.ISystem"/> types.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireSystemAttribute : Attribute
    {
        /// <summary>
        /// Types of classes implementing <see cref="Ignite.Systems.ISystem"/> 
        /// If not, it'll throw an error at runtime.
        /// </summary>
        public Type[] Types = Array.Empty<Type>();

        /// <param name="types">Other <see cref="Ignite.Systems.ISystem"/> types required by the system to work</param>
        public RequireSystemAttribute(params Type[] types) 
        {
            Types = types;
        }
    }
}
