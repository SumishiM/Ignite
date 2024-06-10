using System.Collections.Immutable;

namespace Ignite
{
    public partial class World
    {
        /// <summary>
        /// Will try to merge a <see cref="Subworld"/> at a parent <see cref="Node"/>
        /// </summary>
        /// <param name="parent">parent of the subworld</param>
        /// <param name="subworld">Subworld to merge with</param>
        /// <returns></returns>
        public Node Instantiate(Subworld subworld, Node parent)
        {
            //  add systems to a list of systems to add
            //  if the system is already in the world, skip it except if it's a IStartSystem
            //  else add them at the end of the current world update
            //  foreach startup systems
            //      if it was already added then we need to create a custom context with only subworld nodes
            //      else we just add it and run it normally
            //      note : it may just be simplier to do the first case
            // 

            

            return default;
        }
    }
}