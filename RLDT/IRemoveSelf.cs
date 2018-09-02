using System;
using System.Collections.Generic;
using System.Text;

namespace RLDT
{
    public interface IRemoveSelf
    {
        void RemoveSelf();
        event EventHandler OnRemoveSelf;
    }
}
