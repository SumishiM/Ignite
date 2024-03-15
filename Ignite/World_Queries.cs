using Ignite.Queries;
using Ignite.Systems;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Ignite
{
    public partial class World
    {
        /// <summary>
        /// Create a <see cref="Query"/> (<typeparamref name="T1"/>, <typeparamref name="T2"/>) and execute it if asked to. 
        /// </summary>
        /// <param name="query">Function that will be executed</param>
        /// <param name="executeImmediate">Execute the query immediately. /!\ Will return null /!\</param>
        /// <returns>A <see cref="Query"/> if <paramref name="executeImmediate"/> = false, of null</returns>
        public Query? Q<T1, T2>([DisallowNull] Action<Context> query, bool executeImmediate = false)
            where T1 : IComponent
            where T2 : IComponent
        {
            int contextId = GetOrCreateContext(Context.AccessFilter.AllOf, Lookup[typeof(T1)], Lookup[typeof(T2)]);
            return Q(contextId, query, executeImmediate);
        }

        /// <summary>
        /// Create a <see cref="Query"/> (<typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>) and execute it if asked to. 
        /// </summary>
        /// <param name="query">Function/Lambda that will be executed</param>
        /// <param name="executeImmediate">Execute the query immediately. /!\ Will return null /!\</param>
        /// <returns>A <see cref="Query"/> if <paramref name="executeImmediate"/> = false, of null</returns>
        public Query? Q<T1, T2, T3>([DisallowNull] Action<Context> query, bool executeImmediate = false)
            where T1 : IComponent
            where T2 : IComponent
            where T3 : IComponent
        {
            int contextId = GetOrCreateContext(Context.AccessFilter.AllOf, Lookup[typeof(T1)], Lookup[typeof(T2)], Lookup[typeof(T3)]);
            return Q(contextId, query, executeImmediate);
        }

        /// <summary>
        /// Create the query and execute it if asked to
        /// </summary>
        /// <param name="contextId"></param>
        /// <param name="query"></param>
        /// <param name="executeImmediate"></param>
        /// <returns></returns>
        private Query? Q(int contextId, [DisallowNull] Action<Context> query, bool executeImmediate = false)
        {
            Context context = _contexts[contextId];

            Query q = new(context, query);

            if (executeImmediate)
            {
                q.Execute();
                return null;
            }
            return q;
        }

    }
}
