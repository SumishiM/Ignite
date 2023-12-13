using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite.Attributes
{
    /// <summary>
    /// Indicate that a system will not be deactivated on pause
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class IgnorePauseAttribute : Attribute
    {
    }
}
