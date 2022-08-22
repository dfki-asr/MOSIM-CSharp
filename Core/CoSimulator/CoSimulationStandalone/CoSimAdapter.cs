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
            string avatarID = "";
            // TODO Utilize AvatarID Properly

            foreach(var mmu in this.GetMMus(sessionID))
            {
                base.Dispose(mmu.ID, sessionID, avatarID);
            }
            return base.CloseSession(sessionID);
        }
    }
}
