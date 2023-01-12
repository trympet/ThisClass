using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SomeNamespace
{
    internal class SomeInterface<T> where T : SomeOtherNamespace.SomeOtherInterface
    {
    }
}
namespace SomeOtherNamespace
{
    internal class SomeOtherInterface
    {
    }
}
