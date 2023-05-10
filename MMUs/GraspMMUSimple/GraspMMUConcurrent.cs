using MMICSharp.Adapter;
using MMICSharp.Common;
using MMICSharp.Common.Attributes;
using MMIStandard;
using System;
using System.Collections.Generic;

namespace GraspMMUSimple
{
    /// <summary>
    /// Class used for debugging the MMU
    /// </summary>
    class Debug
    {
        public static SessionData SessionData = new SessionData();
        public static MIPAddress RegisterAddress = new MIPAddress("127.0.0.1", 9009);


        static void Main(string[] args)
        {
            MAdapterDescription adapterDescription = new MAdapterDescription()
            {
                ID = Guid.NewGuid().ToString(),
                Language = "C#",
                Name = "Debug Adapter C#",
                Properties = new Dictionary<string, string>(),
                Addresses = new List<MIPAddress>() { new MIPAddress("127.0.0.1", 8999) }
            };


            using (AdapterController adapterController = new AdapterController(SessionData, adapterDescription, RegisterAddress, new MMUProvider(), new MMUInstantiator()))
            {
                adapterController.Start();
                Console.ReadLine();
            }
        }


        public class MMUProvider : IMMUProvider
        {
            public event EventHandler<EventArgs> MMUsChanged;

            public Dictionary<string, MMULoadingProperty> GetAvailableMMUs()
            {

                //Load the description from path -> Change this line for testing a different MMU
                MMUDescription mmuDescription = MMICSharp.Common.Communication.Serialization.FromJsonString<MMUDescription>(System.IO.File.ReadAllText("../../../GraspMMUSimple/bin/Debug/description.json"));


                return new Dictionary<string, MMULoadingProperty>()
                {
                    {mmuDescription.ID, new MMULoadingProperty(){ Description = mmuDescription} }
                };
            }
        }

        public class MMUInstantiator : IMMUInstantiation
        {
            public IMotionModelUnitDev InstantiateMMU(MMULoadingProperty loadingProperty)
            {
                //Instantiate the respective MMU -> Change this line for testing a different MMU
                return new GraspMMUConcurrentImpl();
            }
        }
    }


    [MMUDescriptionAttribute("Janis Sprenger", "1.0", "GraspMMUConcurrent", "Pose/Grasp", "", "MMU allows to manipulate the finger joints by means of motion blending.", "MMU allows to manipulate the finger joints by means of motion blending.")]
    public class GraspMMUConcurrentImpl : MMUBase
    {
        /// <summary>
        /// Respective instance for the left/right hand
        /// </summaryReachMMUImpl
        private Dictionary<MInstruction, GraspMMUSimpleImpl> mmuInstances = new Dictionary<MInstruction, GraspMMUSimpleImpl>();

        private List<MInstruction> instructions = new List<MInstruction>();


        public override MBoolResponse Initialize(MAvatarDescription avatarDescription, Dictionary<string, string> properties)
        {
            return base.Initialize(avatarDescription, properties);
        }


        /// <summary>
        /// Method to assign an actual instruction
        /// </summary>
        /// <param name="instruction"></param>
        /// <param name="simulationState"></param>
        /// <returns></returns>

        [MParameterAttribute("Hand", "Left/Right", "The hand type.", true)]
        [MParameterAttribute("HandPose", "PostureConstraint", "The desired hand pose, joint constraints of the finger tips.", true)]
        [MParameterAttribute("UseGlobalCoordinates", "bool", "Specified whether the global coordinates of the fingers are used for establishing the hand pose (by default true).", false)]

        [MParameterAttribute("Duration", "float", "The desired duration until the pose is established.", false)]
        [MParameterAttribute("KeepHandPose", "bool", "Specifies whether the MMU finishes once the hand pose is establish, or if the MMU continues actively holding the posture", false)]
        public override MBoolResponse AssignInstruction(MInstruction instruction, MSimulationState simulationState)
        {
            //To do -> Check whether the execution is allowed
            GraspMMUSimpleImpl instance = new GraspMMUSimpleImpl
            {
                SceneAccess = this.SceneAccess,
                ServiceAccess = this.ServiceAccess,
                SkeletonAccess = this.SkeletonAccess
            };

            //Call the instance responsible for the left/right arm
            instance.Initialize(this.AvatarDescription, new Dictionary<string, string>());
            var mbresp = instance.AssignInstruction(instruction, simulationState);


            //Add the instructions and the mmu instance
            instructions.Add(instruction);
            mmuInstances.Add(instruction, instance);


            return mbresp;
        }

        [MSimulationEventAttribute("PositioningFinished", "PositioningFinished")]
        public override MSimulationResult DoStep(double time, MSimulationState simulationState)
        {
            //The simulation result which is provided as overall result
            MSimulationResult result = new MSimulationResult()
            {
                Posture = simulationState.Current,
                Events = new List<MSimulationEvent>(),
                SceneManipulations = new List<MSceneManipulation>()
            };

            //Handle each active MMU (each instruction coressponds to one MMU)
            for (int i = instructions.Count - 1; i >= 0; i--)
            {
                //Update the simulation state
                MSimulationResult localResult = mmuInstances[instructions[i]].DoStep(time, simulationState);

                //Update the simulation state
                //simulationState.Current = localResult.Posture;

                //Just forward the constraints
                simulationState.Constraints = localResult.Constraints;

                //Write the result
                result.Constraints = localResult.Constraints;
                result.Posture = localResult.Posture;

                //Merge the scene manipulations
                result.SceneManipulations?.AddRange(localResult.SceneManipulations);

                //Add the events
                if (localResult.Events != null && localResult.Events.Count > 0)
                    result.Events.AddRange(localResult.Events);

                //Merge the drawing calls
                result.DrawingCalls?.AddRange(localResult.DrawingCalls);

                if (localResult.Events.Exists(s => s.Type == mmiConstants.MSimulationEvent_End && s.Reference == instructions[i].ID))
                {
                    //Remove the respective MMU
                    mmuInstances.Remove(instructions[i]);

                    //Remove from the list
                    instructions.RemoveAt(i);
                }

            }

            return result;
        }

        public override MBoolResponse Abort(string instructionID = null)
        {
            if (instructionID != null)
            {
                MInstruction instruction = this.instructions.Find(S => S.ID == instructionID);

                if (instruction != null)
                {
                    if (mmuInstances.ContainsKey(instruction))
                        mmuInstances.Remove(instruction);

                    if (instructions.Contains(instruction))
                        instructions.Remove(instruction);
                }
            }

            return base.Abort(instructionID);
        }
    }
}
