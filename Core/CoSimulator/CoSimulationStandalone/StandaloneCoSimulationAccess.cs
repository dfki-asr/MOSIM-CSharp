using MMICSharp.Clients;
using MMICSharp.Services;
using MMIStandard;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thrift.Protocol;
using CoSimulationStandalone;
using CoSimulationMMU;

using Logger = MMICSharp.Logger;
using Log_level = MMICSharp.Log_level;


namespace MMICoSimulation
{
    class StandaloneCoSimulationAccess : MCoSimulationAccess.Iface, IDisposable
    {
        #region protected fields

        private List<CoSimulationAccess> accesses = new List<CoSimulationAccess>();
        private List<CoSimulationMMUImpl> cosimMMUs = new List<CoSimulationMMUImpl>();

        protected ServiceController controller;
        private MIPAddress registerAddress;

        protected MServiceDescription description = new MServiceDescription()
        {
            Name = "coSimulationAccess",
            ID = Guid.NewGuid().ToString(),
            Language = "C#"
        };

        #endregion

        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="coSimulator">The instance of the co-simulator</param>
        /// <param name="address">The address where the CoSimulationAccess should be hosted</param>
        /// <param name="registerAddress">The address of the register</param>
        public StandaloneCoSimulationAccess(MIPAddress address, MIPAddress registerAddress)
        {
            //Add the address to the description
            this.description.Addresses = new List<MIPAddress>() { address };
            this.registerAddress = registerAddress;

            //Create a new service controller
            this.controller = new ServiceController(this.description, registerAddress, new MCoSimulationAccess.Processor(this));
        }



        public async void Start()
        {
            Task.Factory.StartNew(() =>
            {
                bool started = false;
                while (!started)
                {
                    try
                    {
                        this.controller.Start();
                        started = true;
                    }
                    catch (System.Exception e)
                    {
                        this.description.Addresses[0].Port++;
                        //Create a new service controller
                        this.controller = new ServiceController(this.description, registerAddress, new MCoSimulationAccess.Processor(this));
                    }
                }
                return true;
            });
            //Start asynchronously

        }


        /// <summary>
        /// Disposes the controller/server
        /// </summary>
        public void Dispose()
        {
            //Unregister at event
            //if (this.coSimulator != null)
            //    this.coSimulator.OnResult -= CoSimulator_OnResult;

            //Dispose the controller
            //if (this.controller != null)
            //    this.controller.Dispose();
            Logger.LogInfo("Cosimulator Access disposal?");
        }

        private void CleanCosimulators(string AvatarID)
        {
            bool found = false;

            for (int i = this.cosimMMUs.Count() - 1; i >= 0; i--)
            {
                if (this.cosimMMUs[i].GetDescription() != null && this.cosimMMUs[i].GetDescription().AvatarID == AvatarID)
                {
                    if (found)
                    {
                        this.cosimMMUs.RemoveAt(i);
                        this.accesses.RemoveAt(i);
                    } else
                    {
                        found = true;
                    }
                }
            }

        }

        public void AddCoSimulator(CoSimulationMMU.CoSimulationMMUImpl cosim)
        {
            if(cosim.GetDescription() != null)
            {
                this.CleanCosimulators(cosim.GetDescription().AvatarID);
            }
            this.cosimMMUs.Add(cosim);
            var access = new CoSimulationAccess((MMICoSimulation.MMICoSimulator) cosim.coSimulator, null, null);
            this.accesses.Add(access);
        }



        //Callback for assigning an instruction to the co simulator
        public MBoolResponse AssignInstruction(MInstruction instruction, Dictionary<string, string> properties)
        {
            string avatarID;
            if (properties.TryGetValue("AvatarID", out avatarID))
            {
                this.CleanCosimulators(avatarID);
                for(int i = 0; i < this.cosimMMUs.Count; i++)
                {
                    if(this.cosimMMUs[i].GetDescription() != null && this.cosimMMUs[i].GetDescription().AvatarID == avatarID)
                    {
                        return this.accesses[i].AssignInstruction(instruction, null);
                    }
                }
                return new MBoolResponse(false) { LogData = new List<string>() { $"No cosimulator found for avatar id {avatarID}" } };
            }
            return new MBoolResponse(false) { LogData = new List<string>() { $"No avatarID found in the properties" } };
            //Directly call the co-simulator
        }


        /*
        /// <summary>
        /// Returns the events of the current frame
        /// </summary>
        /// <returns></returns>
        public MCoSimulationEvents GetCurrentEvents()
        {
            return this.currentEvents;
        }*/

        /*
        /// <summary>
        /// Returns all events of the specific type
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        public List<MCoSimulationEvents> GetHistory(string eventType)
        {
            return this.data.Values.Where(s => s.Events.Exists(x => x.Type == eventType)).ToList();
        }
        */

        /*
        /// <summary>
        /// Returns the simulation events of the specified evnt type occured within the given frames
        /// </summary>
        /// <param name="fromFrame"></param>
        /// <param name="toFrame"></param>
        /// <param name="eventType"></param>
        /// <returns></returns>
        public List<MCoSimulationEvents> GetHistoryFromFrames(int fromFrame, int toFrame, string eventType)
        {
            return this.data.Values.Where(s => s.FrameNumber >= fromFrame && s.FrameNumber < toFrame && s.Events.Exists(x => x.Type == eventType)).ToList();
        }
        */


        /*
        /// <summary>
        /// Returns the simulation events of the specified evnt type occured within the given timespan
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="eventType"></param>
        /// <returns></returns>
        public List<MCoSimulationEvents> GetHistoryFromTime(double startTime, double endTime, string eventType)
        {
            return this.data.Values.Where(s => s.SimulationTime >= startTime && s.SimulationTime < endTime && s.Events.Exists(x => x.Type == eventType)).ToList();
        }*/


        /// <summary>
        /// Unregisters at a specific event given the event type (e.g. simulation end).
        /// The given clientAddress is used to provide an event based communication.
        /// </summary>
        /// <param name="clientAddress"></param>
        /// <param name="eventType">"AvatarID"/"eventType"</param>
        /// <returns></returns>
        public MBoolResponse RegisterAtEvent(MIPAddress clientAddress, string eventType)
        {
            var parts = eventType.Split('/');
            if(parts.Length > 1)
            {
                for (int i = 0; i < this.cosimMMUs.Count; i++)
                {
                    if (this.cosimMMUs[i].GetDescription() != null && this.cosimMMUs[i].GetDescription().AvatarID == parts[0])
                    {
                        return this.accesses[i].RegisterAtEvent(clientAddress, parts[1]);
                    }
                }
                return new MBoolResponse(false) { LogData = new List<string>() { $"No cosimulator found for avatar id {parts[0]}" } };
            }
            return new MBoolResponse(false) { LogData = new List<string>() { $"No avatar ID found. Please separate AvatarID and eventType with a /" } };
        }

        /// <summary>
        /// Unregisters at a specific event given the event type (e.g. simulation end)
        /// </summary>
        /// <param name="clientAddress"></param>
        /// <param name="eventType"></param>
        /// <returns></returns>
        public MBoolResponse UnregisterAtEvent(MIPAddress clientAddress, string eventType)
        {
            var parts = eventType.Split('/');
            if (parts.Length > 1)
            {
                for (int i = 0; i < this.cosimMMUs.Count; i++)
                {
                    if (this.cosimMMUs[i].GetDescription() != null && this.cosimMMUs[i].GetDescription().AvatarID == parts[0])
                    {
                        return this.accesses[i].UnregisterAtEvent(clientAddress, parts[1]);
                    }
                }
                return new MBoolResponse(false) { LogData = new List<string>() { $"No cosimulator found for avatar id {parts[0]}" } };
            }
            return new MBoolResponse(false) { LogData = new List<string>() { $"No avatar ID found. Please separate AvatarID and eventType with a /" } };
            
        }

        /// <summary>
        /// Aborts all instructions
        /// </summary>
        /// <returns></returns>
        public MBoolResponse Abort()
        {
            return new MBoolResponse(false) { LogData = new List<string>() { $"Abort currently not available." } };
        }

        /// <summary>
        /// Aborts a single instruction
        /// </summary>
        /// <param name="instructionID"></param>
        /// <returns></returns>
        public MBoolResponse AbortInstruction(string instructionID)
        {
            foreach(CoSimulationAccess acc in this.accesses)
            {
                MBoolResponse resp = acc.AbortInstruction(instructionID);
                if (resp.Successful)
                {
                    return resp;
                }
            }
            return new MBoolResponse(false) { LogData = new List<string>() { $"Instruction {instructionID} not found. " } };

        }

        /// <summary>
        /// Aborts multiple instructions given by the id
        /// </summary>
        /// <param name="instructionIDs"></param>
        /// <returns></returns>
        public MBoolResponse AbortInstructions(List<string> instructionIDs)
        {
            foreach (string instructionID in instructionIDs)
                this.AbortInstruction(instructionID);

            return new MBoolResponse(true);
        }

        /// <summary>
        /// Returns the present status (required by MMIServiceBase)
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetStatus()
        {
            return new Dictionary<string, string>()
                {
                    { "Running", true.ToString()}
                };
        }

        /// <summary>
        /// Returns the description of the co simulation access
        /// </summary>
        /// <returns></returns>
        public MServiceDescription GetDescription()
        {
            return this.description;
        }

        /// <summary>
        /// Basic setup routine -> Nothing to do in here
        /// </summary>
        /// <param name="avatar"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public virtual MBoolResponse Setup(MAvatarDescription avatar, Dictionary<string, string> properties)
        {
            this.CleanCosimulators(avatar.AvatarID);
            return new MBoolResponse(true);
        }

        /// <summary>
        /// Generic consume method as required by MMIServiceBase -> Nothing to do in here
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public virtual Dictionary<string, string> Consume(Dictionary<string, string> properties)
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Basic dispose method that is remotely called
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public MBoolResponse Dispose(Dictionary<string, string> properties)
        {
            //Dispose the current service
            this.Dispose();

            return new MBoolResponse(true);
        }

        /// <summary>
        /// Restart function being remotely called -> tbd
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public MBoolResponse Restart(Dictionary<string, string> properties)
        {
            return new MBoolResponse(false);
        }

        public List<MCoSimulationEvents> GetHistoryFromTime(double startTime, double endTime, string eventType)
        {
            Logger.LogError("GetHistoryFromTime not implemented. Returning empty list. ");
            return new List<MCoSimulationEvents>();
            //throw new NotImplementedException();
        }

        public List<MCoSimulationEvents> GetHistoryFromFrames(int fromFrame, int toFrame, string eventType)
        {
            Logger.LogError("GetHistoryFromFrames not implemented. Returning empty list. ");
            return new List<MCoSimulationEvents>();
        }

        public List<MCoSimulationEvents> GetHistory(string eventType)
        {
            Logger.LogError("GetHistory not implemented. Returning empty list. ");
            return new List<MCoSimulationEvents>();
        }

        public MCoSimulationEvents GetCurrentEvents()
        {
            Logger.LogError("GetCurrentEvents not implemented. Returning null. ");
            return new MCoSimulationEvents();
        }
    }
}
