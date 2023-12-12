using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite.Attributes
{
    /// <summary>
    /// Attributes for setting a some require components for the entity to work fine.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    /// <param name="types">Require components types.</param>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class RequireComponentAttribute(params Type[] types) : Attribute
    {
        /// <summary>
        /// Types used by the system to create the components
        /// </summary>
        public Type[] Types { get; init; } = types;
    }
}
