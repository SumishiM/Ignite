using Ignite.Attributes;
using Ignite.Systems;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite
{
    public partial class World
    {
        /// <summary>
        /// Check whether the system have the <see cref="IgnorePauseAttribute"/> or not
        /// </summary>
        private bool DoSystemIgnorePause(ISystem system)
        {
            return Attribute.IsDefined(system.GetType(), typeof(IgnorePauseAttribute));
        }

        /// <summary>
        /// Check whether the system can pause, meaning being a <see cref="IUpdateSystem"/> or having the <see cref="IgnorePauseAttribute"/>
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        private bool CanSystemPause(ISystem system)
        {
            return system is IUpdateSystem && !DoSystemIgnorePause(system);
        }
    }
}
