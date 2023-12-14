namespace Ignite.Attributes
{
    /// <summary>
    /// Specifies the required <see cref="Ignite.Components.IComponent"/> types
    /// for a <see cref="Ignite.Entities.Entity"/> to work properly.
    /// <para>This attribute must be used on <see cref="Ignite.Entities.Entity"/> types.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RequireComponentAttribute : Attribute
    {
        /// <summary>
        /// <see cref="Ignite.Components.IComponent"/> types 
        /// require for the entity to work properly.
        /// </summary>
        public Type[] Types { get; init; } = Array.Empty<Type>();

        /// <param name="types"><see cref="Ignite.Components.IComponent"/> types needed 
        /// by the <see cref="Ignite.Entities.Entity"/> to work properly.</param>
        public RequireComponentAttribute(params Type[] types)
        {
            Types = types;  
        }
    }
}
