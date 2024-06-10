using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Ignite.Attributes;

namespace Ignite
{
    public class Subworld
    {
        internal class Node
        {
            public ImmutableArray<Node> Children;
            public ImmutableHashSet<int> Components;

            public Node(Ignite.Node source)
            {
                var children = ImmutableArray.CreateBuilder<Node>();

                foreach (var child in source.Children)
                {
                    children.Add(new(child));
                }

                Children = [.. children];
                Components = [.. source.ComponentsIndices];
            }
        }

        internal readonly Node Root;

        internal ImmutableArray<Type> Systems;

        internal ImmutableArray<Type> StartSystems;
        internal ImmutableArray<Type> UpdateSystems;
        internal ImmutableArray<Type> FixedUpdateSystems;
        internal ImmutableArray<Type> RenderSystems;
        internal ImmutableArray<Type> ExitSystems;

        public Subworld([DisallowNull]Ignite.Node from)
        {
            Root = new(from);
            Systems = GetAllRequiredSystems(from.World);

            
        }

        private ImmutableHashSet<int> GetAllComponentsIndex(Node node)
        {
            var components = ImmutableHashSet.CreateBuilder<int>();
            components.UnionWith(node.Components);

            foreach (var child in node.Children)
            {
                components.UnionWith(GetAllComponentsIndex(child));
            }

            return components.ToImmutable();
        }

        private ImmutableArray<Type> GetAllRequiredSystems(World source)
        {
            var requiredSystems = ImmutableHashSet.CreateBuilder<Type>();
            var components = GetAllComponentsIndex(Root);
            bool added = false;

            foreach (var (type, _cachedFixedUpdateSystems) in source.TypeToSystem)
            {
                var requiredComponents = type.GetCustomAttributes<RequireComponentAttribute>();

                foreach (var attribute in requiredComponents)
                {
                    foreach (var requiredComponent in attribute.Types)
                    {
                        if (components.Contains(source.Lookup[requiredComponent]))
                        {
                            requiredSystems.Add(type);
                            added = true; // signal that the system has been added
                            break;
                        }
                    }

                    if (added) // if added we go back to the first loop
                    {
                        added = false;
                        break;
                    }
                }
            }

            return [.. requiredSystems];
        }
    }
}