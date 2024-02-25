// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Janis Sprenger

using System;
using MMIStandard;
using MMICSharp.Common;
using MMICSharp.Adapter;
using MMICSharp;
using MMICSharp.Services;

namespace RetargetingServiceServer
{

    class Program
    {

        /// The address of the thrift server
        private static MIPAddress address = new MIPAddress("127.0.0.1", 8900);
        private static MIPAddress addressInt = null; // new MIPAddress("127.0.0.1", 8900);

        ///The address of the register
        private static MIPAddress mmiRegisterAddress = new MIPAddress("127.0.0.1", 8900);

        /// The path of the mmus
        private static string mmuPath = "";


        static void Main(string[] args)
        {
            //Create a new logger instance
            Logger.Instance = new Logger
            {
                //Log everything
                Level = Log_level.L_DEBUG
            };

            //Register for unhandled exceptions in the application
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //Parse the command line arguments
            if (!ParseCommandLineArguments(args))
            {
                Logger.Log(Log_level.L_ERROR, "Cannot parse the command line arguments. Closing the adapter!");
                return;
            }

            if(addressInt == null)
            {
                // no internal address set, using external address
                addressInt = address;
            }

            Console.WriteLine($"Adapter is reachable at: {address.Address}:{address.Port}");
            Console.WriteLine($"Register is reachable at: {mmiRegisterAddress.Address}:{mmiRegisterAddress.Port}");
            Console.WriteLine($"MMUs will be loaded from: {mmuPath}");
            Console.WriteLine(@"_________________________________________________________________");

            Console.WriteLine("Retargeting Service");
            string sessionID = Guid.NewGuid().ToString();
            RetargetingInterfaceWrapper handler = new RetargetingInterfaceWrapper(addressInt.Address, addressInt.Port);


            ServiceController controller = new MMICSharp.Services.ServiceController(handler.GetDescription(), mmiRegisterAddress, new MRetargetingService.Processor(handler), addressInt);
            controller.Start();
            Console.ReadLine();
        }

        /// <summary>
        /// Callback for unhandled exceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Log(Log_level.L_ERROR, e.ExceptionObject.ToString());

            //Write a log file
            System.IO.File.WriteAllText("CSharpAdapter_Error.log", DateTime.Now.ToString() + " " + e.ExceptionObject.ToString());
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

                { "aint|addressInternal=", "The address of the hostet tcp server.",
                  v =>
                  {
                      //Split the address to get the ip and port
                      string[] addr  = v.Split(':');

                      if(addr.Length == 2)
                      {
                          addressInt = new MIPAddress();
                          addressInt.Address = addr[0];
                          addressInt.Port = int.Parse(addr[1]);
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

                { "m|mmupath=", "The path of the mmu folder.",
                  v =>
                  {
                        mmuPath = v;
                  }
                },
            };

            try
            {
                p.Parse(args);
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot parse arguments");
            }


            return false;

        }


    }
}
