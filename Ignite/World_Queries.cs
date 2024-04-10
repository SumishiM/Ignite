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
        /// <param name="action">Function that will be executed</param>
        /// <param name="executeImmediate">Execute the query immediately. /!\ Will return null /!\</param>
        /// <returns>A <see cref="Query"/> if <paramref name="executeImmediate"/> = false, of null</returns>
        public Query? Q<T1, T2>([DisallowNull] Action<Context> action, bool executeImmediate = false)
            where T1 : IComponent
            where T2 : IComponent
        {
            int contextId = GetOrCreateContext(Context.AccessFilter.AllOf, typeof(T1), typeof(T2));
            return Q(contextId, action, executeImmediate);
        }

        /// <summary>
        /// Create a <see cref="Query"/> (<typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>) and execute it if asked to. 
        /// </summary>
        /// <param name="action">Function/Lambda that will be executed</param>
        /// <param name="executeImmediate">Execute the query immediately. /!\ Will return null /!\</param>
        /// <returns>A <see cref="Query"/> if <paramref name="executeImmediate"/> = false, of null</returns>
        public Query? Q<T1, T2, T3>([DisallowNull] Action<Context> action, bool executeImmediate = false)
            where T1 : IComponent
            where T2 : IComponent
            where T3 : IComponent
        {
            int contextId = GetOrCreateContext(Context.AccessFilter.AllOf, typeof(T1), typeof(T2), typeof(T3));
            return Q(contextId, action, executeImmediate);
        }

        /// <summary>
        /// Create the query and execute it if asked to
        /// </summary>
        /// <param name="contextId"></param>
        /// <param name="action"></param>
        /// <param name="executeImmediate"></param>
        /// <returns></returns>
        private Query? Q(int contextId, [DisallowNull] Action<Context> action, bool executeImmediate = false)
        {
            Context context = _contexts[contextId];

            Query query = new(context, action);

            if (executeImmediate)
            {
                query.Execute();
                return null;
            }
            return query;
        }

    }
}
