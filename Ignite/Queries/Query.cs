using Ignite.Systems;

namespace Ignite.Queries
{
    public class Query
    {
        private readonly Context _context;
        private readonly Action<Context> _query;

        internal Query(Context context, Action<Context> query)
        {
            _context = context;
            _query = query;
        }

        /// <summary>
        /// Execute the query
        /// </summary>
        public bool Execute()
        {
            try
            {
                _query.Invoke(_context);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
