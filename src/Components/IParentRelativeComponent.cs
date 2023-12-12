using Ignite.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite.Components
{
    public interface IParentRelativeComponent : IComponent
    {

        void OnParentChanged(IComponent component, Entity child);
    }
}
