namespace Ignite
{
    public class WorldSettings
    {
        /// <summary>
        /// List of systems for the world
        /// </summary>
        public Type[] Systems = [];

        /// <summary>
        /// Nodes infos for nodes by default in the world with a name and a list of components
        /// </summary>
        public (string, Type[])[] DefaultNodes = [];
    }
}
