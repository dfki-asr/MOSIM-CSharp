using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMICSharp.Adapter;
using MMICSharp.Common;
using MMIStandard;

namespace CoSimulationStandalone
{
    class CoSimAdapter : ThriftAdapterImplementation
    {
        public CoSimAdapter(SessionData session, IMMUInstantiation mmuInstantiator) : base(session, mmuInstantiator)
        {

        }

        public override MBoolResponse CloseSession(string sessionID)
        {
            foreach(var mmu in this.GetMMus(sessionID))
            {
                base.Dispose(mmu.ID, sessionID);
            }
            return base.CloseSession(sessionID);
        }
    }
}
