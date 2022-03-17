// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Felix Gaisbauer

using CoSimulationMMU;
using MMICSharp.Adapter;
using MMICSharp.Adapter.MMUProvider;
using MMIStandard;
using MMICSharp.Common;
using MMICSharp;
using System;
using System.Collections.Generic;

namespace CoSimulationStandalone
{
    /// <summary>
    /// Class to store the CoSim and adapter description.
    /// </summary>
    public static class Data
    {
        internal static MMUDescription CoSimMMUDescription = new MMUDescription()
        {
            ID = Guid.NewGuid().ToString(),
            AssemblyName = "co-simulation",
            Author = "xy",
            Language = "C#",
            MotionType = "co-simulation",
            Name = "CoSimulation",
            ShortDescription = "short",
            LongDescription = "long",
            Parameters = new List<MParameter>(),
            Version = "Debug"
        };

        internal static MAdapterDescription AdapterDescription = new MAdapterDescription()
        {
            ID = "co-sim16102019-standalone-v1.0",
            Language = "C#",
            Name = "CoSimulation Adapter C#",
            Properties = new Dictionary<string, string>(),
            Addresses = new List<MIPAddress>() { new MIPAddress("127.0.0.1", 8998) }
        };


        internal static SessionData SessionData;

    }


    /// <summary>
    /// The application can be utilized for hosting the co-simulation as standalone application or for debugging purposes.
    /// </summary>
    class Program
    {

        /// The address of the thrift server
        private static MIPAddress address = new MIPAddress("127.0.0.1", 8998);

        ///The address of the register
        private static MIPAddress mmiRegisterAddress = new MIPAddress("127.0.0.1", 9009);

        static void Main(string[] args)
        {
            Console.WriteLine(@"   ______           _____ _                 __      __  _           ");
            Console.WriteLine(@"  / ____/___       / ___/(_)___ ___  __  __/ /___ _/ /_(_)___  ____ ");
            Console.WriteLine(@" / /   / __ \______\__ \/ / __ `__ \/ / / / / __ `/ __/ / __ \/ __ \");
            Console.WriteLine(@"/ /___/ /_/ /_____/__/ / / / / / / / /_/ / / /_/ / /_/ / /_/ / / / /");
            Console.WriteLine(@"\____/\____/     /____/_/_/ /_/ /_/\__,_/_/\__,_/\__/_/\____/_/ /_/ ");

            //Create a new logger instance
            Logger.Instance = new Logger
            {
                //Log everything
                Level = Log_level.L_DEBUG
            };

            if (!ParseCommandLineArguments(args))
            {
                Logger.Log(Log_level.L_ERROR, "Cannot parse the command line arguments. Closing the adapter!");
                return;
            }

            Logger.Log(Log_level.L_INFO, $"Adapter is reachable at: {address.Address}:{address.Port}");
            Logger.Log(Log_level.L_INFO, $"Register is reachable at: {mmiRegisterAddress.Address}:{mmiRegisterAddress.Port}");
            Logger.Log(Log_level.L_INFO, @"_________________________________________________________________");

            Data.AdapterDescription.Addresses[0] = address;

            Data.SessionData = new SessionData();
            var cosimInitiator = new CosimInstantiator(Data.AdapterDescription.Addresses[0], mmiRegisterAddress);
            //Create a new adapter controller
            using (AdapterController adapterController = new AdapterController(Data.SessionData, Data.AdapterDescription, mmiRegisterAddress, new DescriptionBasedMMUProvider(Data.CoSimMMUDescription),
                cosimInitiator, new CoSimAdapter(Data.SessionData, cosimInitiator)))
            {
                adapterController.Start();

                // This is broken!
                Console.ReadLine();
            }
        }


        /// <summary>
        /// Tries to parse the command line arguments
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool ParseCommandLineArguments(string[] args)
        {

            //Parse the command line arguments
            OptionSet p = new OptionSet()
            {
                { "a|address=", "The address of the hostet tcp server.",
                  v =>
                  {
                      //Split the address to get the ip and port
                      string[] addr  = v.Split(':');

                      if(addr.Length == 2)
                      {
                          address.Address = addr[0];
                          address.Port = int.Parse(addr[1]);
                      }
                  }
                },

                { "r|raddress=", "The address of the register which holds the central information.",
                  v =>
                  {
                      //Split the address to get the ip and port
                      string[] addr  = v.Split(':');

                      if(addr.Length == 2)
                      {
                          mmiRegisterAddress.Address = addr[0];
                          mmiRegisterAddress.Port = int.Parse(addr[1]);
                      }
                  }
                },

            };

            try
            {
                p.Parse(args); //Fixing end of the path, if it ends with a slash or backslash the adapter won't run
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot parse arguments");
            }


            return false;
        }
    }     



    /// <summary>
    /// Create a new class which instantiates the respective co-simulation MMU
    /// </summary>
    public class CosimInstantiator : IMMUInstantiation
    {

        private MIPAddress adapterAddress;
        private MIPAddress registryAddress;
        private MMICoSimulation.StandaloneCoSimulationAccess cosimAccess;

        public CosimInstantiator(MIPAddress adapterAddress, MIPAddress registryAddress)
        {
            this.adapterAddress = adapterAddress;
            this.registryAddress = registryAddress;

            // TODO Find better port management. 
            cosimAccess = new MMICoSimulation.StandaloneCoSimulationAccess(new MIPAddress(adapterAddress.Address, 8950), registryAddress);
            cosimAccess.Start();
        }

        public IMotionModelUnitDev InstantiateMMU(MMULoadingProperty mmuLoadingProperty)
        {
            if (mmuLoadingProperty.Description.ID  == Data.CoSimMMUDescription.ID)
            {
                CoSimulationMMUImpl instance = new CoSimulationMMUImpl(adapterAddress, registryAddress);
                cosimAccess.AddCoSimulator(instance);
                return instance;
            }
            return null;
        }
    }
}



