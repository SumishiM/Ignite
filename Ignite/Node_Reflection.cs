using Ignite.Attributes;
using System.Reflection;

namespace Ignite
{
    /**
     * This part of the Node class handle reflection related stuffs
     **/
    public partial class Node
    {
        private void CheckIgnorePause()
        {
            IgnorePause = GetType().DeclaringType?
                .GetCustomAttribute(typeof(IgnorePauseAttribute), true) != null;
        }

        /// <summary>
        /// Check node required components and add them as empty if needed
        /// </summary>
        private void CheckRequiredComponents()
        {
            RequireComponentAttribute[] requires = (RequireComponentAttribute[])GetType()
                .GetCustomAttributes(typeof(RequireComponentAttribute), true);

            foreach (var require in requires)
            {
                foreach (Type componentType in require.Types)
                {
                    if (!HasComponent(componentType))
                        AddComponent(componentType);
                }
            }
        }
    }
}
