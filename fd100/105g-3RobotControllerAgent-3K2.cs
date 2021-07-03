//----------------------------------------------------------------------------------/
// program    : RobotControllerAgent-3K2.cv                                         /
// coded by   : Unity (R. Kandas) https://github.com/rkandas/RobotArmMLAgentUnity   /
// updated by : Chee Vang, HL                                                       /
// copyrighted by : CTI One Corp.                                                   /
// version    : x0.2                                                                /
// status     : debug [], tested [x]                                                /
// note       : 1. re-wrote reward algorithm to switch                              /
//                 between base-line with time index and                            /
//                 picewise with two line segments & k1,k2                          /
//                 gain factor                                                      /
//              2. reward_flag set to "true" to use K2                              /
//                 algorithm (line 260)                                             /
//----------------------------------------------------------------------------------/ 
using System;
using System.Data.Common;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;
// 2021-5-14: File I/O
using System.IO;
using System.Text;
// 2021-5-17: List/Queues
using System.Collections.Generic;

//------------------------------------------------------------/
public class RobotControllerAgent : Agent
{
   [SerializeField]
   private GameObject[] armAxes; // Any "GameObject" is taken from Unity
   [SerializeField] 
   private GameObject endEffector;

   public bool trainingMode;
   private bool inFrontOfComponent = false;
   public GameObject nearestComponent; // target position, world coordinate system
   private Ray rayToTest = new Ray();
   private float[] angles = new float[5];
   private KinematicsCalculator calculator;
   private float beginDistance; // distance[0], remains constant for each episode
   private float prevBest; // min(delta_distance(t), prev(t-1))
   bool  inrange2 = false, inrange3 = false, inrange4 = false, inrange5 = false;
   private float baseAngle;
   private const float stepPenalty = -0.0001f;
   private string path = @"/home/chee/Documents/RobotArmMLAgentUnity_original/distance/distance-k2-1p5-01.csv"; // CV 2021-5-14: File path for CSV file
   
   private int epi_index = 1; // CV 2021-5-14: index used to determine number of times OnEpisodeBegin was called
   private int t; // CV 2021-5-17: a time index
   private int buffLength = 3;//1024;
   // Queues
   private Queue<float> deltaDistBuffer = new Queue<float>(); // CV 2021-5-20: Buffer for delta_distance
   private Queue<float> distanceBuffer = new Queue<float>(); // CV 2021-5-20: Buffer for distance
   private Queue<float> prevBestBuffer = new Queue<float>(); // CV 2021-5-20: Buffer for prevBest
   private Queue<float> tBuffer = new Queue<float>(); // CV 2021-5-20: Buffer for time index t

   //------------------------------------------------------------/
   // CV MonoBehaviour::Start()
   //   - called on frame when script is enabled just before any update methods
   //     are called the first time
   //   - referece: https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html
   private void Start()
   {
      
   }

   //------------------------------------------------------------/
   // Agent::Initialize ()
   //   - Perfroms a one-time initialization or set up of the agent instance 
   //   - MLAgents API referece: https://docs.unity3d.com/Packages/com.unity.ml-agents@1.0/api/Unity.MLAgents.Agent.html#Unity_MLAgents_Agent_Initialize
   public override void Initialize()
   {
      ResetAllAxis();
      // 2021-6-8: fixed starting endeffector position
      MoveToSafeUserPreDefinedPostion();
      //MoveToSafeRandomPosition();
      if (!trainingMode) MaxStep = 0;

      // CV, SS 2021-5-26: Label the columns in CSV file
      using(StreamWriter writer = new StreamWriter(path,false)) {
         writer.WriteLine("t" + "," + 
               "beginDistance" + "," +
               "prevBest" + "," + 
               "distance" + "," + 
               "delta_distance" + "," + 
               "reward" + "," + 
               "target x" + "," + 
               "target y" + "," + 
               "target z" + "," + 
               "endEff x " + "," +
               "endEff y " + "," +
               "endEff z ");
         writer.Close();
      }

   }

   //------------------------------------------------------------/
   // ResetAllAsix()
   //   - resets all axis to zero
   //   - function called in
   //       1) Initialized()
   //       2) OnEpisodeBegin()
   private void ResetAllAxis()
   {
      armAxes.All(c =>
      {
         c.transform.localRotation =  Quaternion.Euler(0f, 0f, 0f);
         return true;
      });
   }

   //------------------------------------------------------------/
   // Agent::OnEpisodeBegin()
   //   - Set up an agent instance to the start of an episode
   //   - Resets agent and environment to their starting position where 
   //     reset values are randomized
   //   - MLAgents API reference: https://docs.unity3d.com/Packages/com.unity.ml-agents@1.0/api/Unity.MLAgents.Agent.html#Unity_MLAgents_Agent_OnEpisodeBegin
   //   - MLAgents Github reference: https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Learning-Environment-Design-Agents.md
   public override void OnEpisodeBegin()
   {
      if(trainingMode)
         ResetAllAxis();

      // 2021-6-8: fixed starting endeffector position
      MoveToSafeUserPreDefinedPostion();
      //MoveToSafeRandomPosition();
      UpdateNearestComponent();

      // CV 2021-5-17: Resets time index at start of new episode
      //   - time_index is updated at each step and resetted at the
      //     start of a new episode
      t = 0;

      // CV 2021-5-14: Notifies start of a new episode in the CSV file
      //   - epi_index = number of times this OnEpisdoeBegin() is called
      //   - CompletedEpisodes = MLAgents C# property that returns number 
      //     of completed episodes
      //       + MLAgents API reference: https://docs.unity3d.com/Packages/com.unity.ml-agents@1.0/api/Unity.MLAgents.Agent.html
      /*using(StreamWriter writer = new StreamWriter(path,true)) {
         writer.WriteLine("--- Start New Episode #" + epi_index + " --- Agent Ep: " + CompletedEpisodes);
         writer.Close();
      }
      epi_index++;*/

      // CV 2021-6-2: Prints the initial data into CSV file
      float distance = Vector3.Distance(endEffector.transform.TransformPoint(Vector3.zero),
         nearestComponent.transform.position);
      float delta_distance = distance - prevBest; // CV 2021-5-19: delta distance
      float reward = 0;
      using(StreamWriter writer = new StreamWriter(path,true)) {
         writer.WriteLine( t + "," + 
                           beginDistance + "," + 
                           prevBest + "," + 
                           distance + "," + 
                           delta_distance + "," + 
                           reward + "," +
                           nearestComponent.transform.position.x + "," +
                           nearestComponent.transform.position.y + "," +
                           nearestComponent.transform.position.z + "," +
                           endEffector.transform.position.x + "," +
                           endEffector.transform.position.y + "," +
                           endEffector.transform.position.z );
         writer.Close();
      }
      t++;
   }

   //------------------------------------------------------------/
   // UpdateNearestComponent()
   //   - Original author's function to update the target ("nearestComponent") 
   //     position to a random, reachable location
   //   - function is called when
   //       1) OnEpisodeBegin() is called
   //       2) end effector reaches the target (Note: orginal code does 
   //          not end episode when end effector reaches target, see 
   //          JackpotRewar() for more details)
   //   - computes "beginDistance" and initializes "prevBest" with that value
   //   - checks that base angle (the whole robot arm, in Unity it is the prefab 
   //     "RobotwithColliders" in the hierarchy) is not negative value
   private void UpdateNearestComponent()
   {
      // CV 2021-5-27: fixed target location
      /*if (trainingMode)
      {
         inFrontOfComponent = UnityEngine.Random.value > 0.5f;
      }
      if(!inFrontOfComponent)
         nearestComponent.transform.position = transform.position + new Vector3(Random.Range(0.3f,0.6f),Random.Range(0.1f,0.3f), Random.Range(0.3f,0.6f));
      else
      {
         nearestComponent.transform.position = endEffector.transform.TransformPoint(Vector3.zero) + new Vector3(Random.Range(0.01f,0.15f),Random.Range(0.01f,0.15f), Random.Range(0.01f,0.15f));
      }*/
      nearestComponent.transform.position = transform.position + new Vector3(0.45f,0.2f,0.45f);

      beginDistance = Vector3.Distance(endEffector.transform.TransformPoint(Vector3.zero), nearestComponent.transform.position);
      prevBest = beginDistance;
      
      baseAngle = Mathf.Atan2( transform.position.x - nearestComponent.transform.position.x, transform.position.z - nearestComponent.transform.position.z) * Mathf.Rad2Deg;
      if (baseAngle < 0) baseAngle = baseAngle + 360f;
   }

   //------------------------------------------------------------/
   // Agent::CollectObservations()
   //   - MLAgents C# function to collect vector observations of the agents for the step 
   //     that describes the current environment from the perspective of the agent
   //   - is called every step when agent requests a decision
   //   - MLAgents API reference: https://docs.unity3d.com/Packages/com.unity.ml-agents@1.0/api/Unity.MLAgents.Agent.html#Unity_MLAgents_Agent_CollectObservations_Unity_MLAgents_Sensors_VectorSensor_
   //   - MLAgents Github reference: https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Learning-Environment-Design-Agents.md
   /// <summary>
   /// Markov Decision Process - Observes state for the current time step
   /// </summary>
   /// <param name="sensor"></param>
   public override void CollectObservations(VectorSensor sensor)
   {
      sensor.AddObservation(angles);
      sensor.AddObservation(transform.position.normalized);
      sensor.AddObservation(nearestComponent.transform.position.normalized);
      sensor.AddObservation(endEffector.transform.TransformPoint(Vector3.zero).normalized);
      Vector3 toComponent = (nearestComponent.transform.position - endEffector.transform.TransformPoint(Vector3.zero));
      sensor.AddObservation(toComponent.normalized);
      sensor.AddObservation(Vector3.Distance(nearestComponent.transform.position,endEffector.transform.TransformPoint(Vector3.zero)));
      sensor.AddObservation(StepCount / 5000);
   }

   //------------------------------------------------------------/
   // Agent::OnActionReceived()
   //   - MLAgents C# function used to specify the agent behaviour at every 
   //     step based on the provided action
   //   - is called every time the agent receives an action to take
   //   - common to assign rewards in here
   //   - MLAgents API reference: https://docs.unity3d.com/Packages/com.unity.ml-agents@1.0/api/Unity.MLAgents.Agent.html#Unity_MLAgents_Agent_OnActionReceived_System_Single___
   //   - MLAgents Github reference: https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Learning-Environment-Design-Agents.md
   public override void OnActionReceived(float[] vectorAction)
   {
      angles = vectorAction;
      if (trainingMode)
      {
         // Translate the floating point actions into Degrees of rotation for each axis
         armAxes[0].transform.localRotation =
            Quaternion.AngleAxis(angles[0] * 180f, armAxes[0].GetComponent<Axis>().rotationAxis);
         armAxes[1].transform.localRotation =
            Quaternion.AngleAxis(angles[1] * 90f, armAxes[1].GetComponent<Axis>().rotationAxis);
         armAxes[2].transform.localRotation =
            Quaternion.AngleAxis(angles[2] * 180f, armAxes[2].GetComponent<Axis>().rotationAxis);
         armAxes[3].transform.localRotation =
            Quaternion.AngleAxis(angles[3] * 90f, armAxes[3].GetComponent<Axis>().rotationAxis);
         armAxes[4].transform.localRotation =
            Quaternion.AngleAxis(angles[4] * 90f, armAxes[4].GetComponent<Axis>().rotationAxis);

         float distance = Vector3.Distance(endEffector.transform.TransformPoint(Vector3.zero),
            nearestComponent.transform.position);
         float diff = beginDistance - distance;
         float delta_distance = distance - prevBest; // CV 2021-5-19: delta distance
         float delta_R = beginDistance-prevBest; // CV 2021-5-19: 

         float k1 = 1.0f; // 2021-5-14 CV,HL: gain factor for Line 1
         float k2 = 1.5f; // 2021-5-14 CV,HL: gain factor for Line 2
         bool reward_flag = true; // 2021-5-17 CV: flag for using original (false) or our reward function (true)
         float reward;
         
         // CV,SS 2021-5-27: updates the queues with latest 1024 data
         if(deltaDistBuffer.Count >= buffLength)
         {
            deltaDistBuffer.Dequeue();
            distanceBuffer.Dequeue();
            prevBestBuffer.Dequeue();
            tBuffer.Dequeue();
         }
         deltaDistBuffer.Enqueue(delta_distance);
         distanceBuffer.Enqueue(distance);
         prevBestBuffer.Enqueue(prevBest);
         tBuffer.Enqueue(t);

         // CV,SS 2021-5-27: For debugging purposes to check if fixed queue size is working using
         // tBuffer by output buffer on console
         /*string deltaDistString = "";
         foreach(float n in tBuffer)
         {
            deltaDistString = deltaDistString + ", " + n;
         }
         Debug.LogWarning(deltaDistString);*/

         if(reward_flag == true) // CV 2021-5-19: reward function with delta_R
         {
            if (distance > prevBest) // penalty (positve x-axis)
            {
               reward = -1*k1*delta_distance; // CV 2021-5-19: Line 1
            }
            else // reward (negative x-axis)
            {
               reward = k2*(delta_R-delta_distance); // CV 2021-5-19: Line 2
               prevBest = distance;
            }
         }
         // CV 2021-5-14: Original algorithm (slightly modidfied to add reward outside of if statement)
         // Note: Since June 14, this "else" portion has been replaced by the original code to accurately
         // capture the original base-line results
         else
         {
            if (distance > prevBest)
            {
               // Penalty if the arm moves away from the closest position to target
               reward = prevBest - distance;
            }
            else
            {
               // Reward if the arm moves closer to target
               reward = diff;
               prevBest = distance;
            }
         }
         
         AddReward(reward);  // CV 2021-5-14: Adds reward given by the cases above
         
         // CV 2021-5-14: Write to CSV file
         //   1) t = time index (CV 2021-5-17)
         //   2) beginDistance = initial distance between end effector and target (fixed
         //      for each episode)
         //   3) prevBest = closest distance between end effector and target saved
         //   4) distance = distance between current end effector and target
         //   5) reward
         //   6) nearestComponent's position // CV,HL,SS 2021-6-2
         //   7) endEffector's position // CV,HL,SS 2021-6-2
         using(StreamWriter writer = new StreamWriter(path,true)) {
            writer.WriteLine( t + "," + 
                              beginDistance + "," + 
                              prevBest + "," + 
                              distance + "," + 
                              delta_distance + "," + 
                              reward + "," +
                              nearestComponent.transform.position.x + "," +
                              nearestComponent.transform.position.y + "," +
                              nearestComponent.transform.position.z + "," +
                              endEffector.transform.position.x + "," +
                              endEffector.transform.position.y + "," +
                              endEffector.transform.position.z );
            writer.Close();
         }
         t++; // CV 2021-5-17: increments  the time index

         AddReward(stepPenalty);
      }
   }

   //------------------------------------------------------------/
   // GroundHitPenalty()
   //   - Original author's function to give hefty penalty if robot 
   //     arm or gripper collides with ground
   //   - called in PenaltyColliders.cs file in same folder
   public void GroundHitPenalty()
   {
      AddReward(-1f);

      // CV 2021-5-17: Stores "Ground" in CSV file if ground hit
      /*using(StreamWriter writer = new StreamWriter(path,true)) {
         writer.WriteLine("Ground");
         writer.Close();
      }*/

      EndEpisode();
   }

   //------------------------------------------------------------/
   // Monobehavior::OnTriggerEnter()
   //   - Unity's function where this function is called when a GameObject
   //     collides with another GameObject
   //   - parameters: 
   //       + other = the other Collider involved in the collision 
   //   - Unity API reference: https://docs.unity3d.com/ScriptReference/Collider.OnTriggerEnter.html
   private void OnTriggerEnter(Collider other)
   {
      JackpotReward(other);

      /*// CV 2021-5-17: Stores "Jackpot" in CSV file if end effector reached target
      using(StreamWriter writer = new StreamWriter(path,true)) {
         writer.WriteLine("Jackpot");
         writer.Close();
      }*/
   }

   //------------------------------------------------------------/
   // JackpotReward()
   //   - add rewards when end effector reaches the target, where
   //          reward = 0.5 + bonus
   //          bonus = target (dot) endEffector
   //   - Notes: from https://docs.unity3d.com/ScriptReference/Vector3.Dot.html
   //       + dot product of two vectors (x and y are vectors)
   //             x (dot) y = ||x|| ||y|| cos(theta) 
   //       + for normalized vectors, it will return
   //           1)  1 = point in same direction
   //           2) -1 = point in completely opposite directions
   //           3)  0 = vectors are perpendicular
   public void JackpotReward(Collider other)
   {
      if (other.transform.CompareTag("Components"))
      {
         float SuccessReward = 0.5f;
         float bonus = Mathf.Clamp01(Vector3.Dot(nearestComponent.transform.up.normalized,
                          endEffector.transform.up.normalized));
         float reward = SuccessReward + bonus;
         if (float.IsInfinity(reward) || float.IsNaN(reward)) return;
          Debug.LogWarning("Great! Component reached. Positive reward:" + reward );
         AddReward(reward);
         //EndEpisode();
         UpdateNearestComponent();
      }
   }
   
   //------------------------------------------------------------/
   // private float[] NormalizedAngles()
   // {
   //    float[] normalized = new float[6];
   //    for (int i = 0; i < 6; i++)
   //    {
   //       normalized[i] = angles[i] / 360f;
   //    }
   //
   //    return normalized;
   // }

   //------------------------------------------------------------/
   // MoveToSafeRandomPosition()
   //   - Original author's function to make sure that endEffector is
   //     above the ground but not out of reach
   //   - Function called in
   //       1) Initialize()
   //       2) OnEpisodeBegin()
   private void MoveToSafeRandomPosition()
   {
      int maxTries = 100;
      
      while (maxTries > 0)
      {
         armAxes.All(axis =>
            {
               Axis ax = axis.GetComponent<Axis>();
               Vector3 angle = ax.rotationAxis * Random.Range(ax.MinAngle, ax.MaxAngle);
               ax.transform.localRotation = Quaternion.Euler(angle.x, angle.y, angle.z);
               return true;
            }
         );
         Vector3 tipPosition = endEffector.transform.TransformPoint(Vector3.zero);
         Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
         float distanceFromGround = groundPlane.GetDistanceToPoint(tipPosition);
         if (distanceFromGround > 0.1f && distanceFromGround <= 1f && tipPosition.y > 0.01f)
         {
            break;
         }
         maxTries--;
      }
   }

   //-------------------------------------------------------------/ 
   // CV, HL 2021-6-2: fixed endEffector starting position
   // Same as MoveToSafeRandomPosition() but the angles of each
   // joints are user defined (fixed) resulting in position 
   // ( -0.185766, 1.049303, 0.1233658 )
   private void MoveToSafeUserPreDefinedPostion()
   {
      int maxTries = 100;
      
      while (maxTries > 0)
      {
         armAxes.All(axis =>
            {
               Axis ax = axis.GetComponent<Axis>();
               // 2021-6-8: fixed position
               //Vector3 angle = ax.rotationAxis * Random.Range(ax.MinAngle, ax.MaxAngle);
               Vector3 angle = ax.rotationAxis * (1.1f*ax.MinAngle+0.9f*ax.MaxAngle)/2;

               ax.transform.localRotation = Quaternion.Euler(angle.x, angle.y, angle.z);
               return true;
            }
         );
         Vector3 tipPosition = endEffector.transform.TransformPoint(Vector3.zero);
         Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
         float distanceFromGround = groundPlane.GetDistanceToPoint(tipPosition);
         if (distanceFromGround > 0.1f && distanceFromGround <= 1f && tipPosition.y > 0.01f)
         {
            break;
         }
         maxTries--;
      }
   }

   //------------------------------------------------------------/
   // Monobehavior::Update()
   //   - Unity's function that is called in very frame
   //   - Specifically, this draws a green line between the 
   //     endeffector and targer
   //   - Unity API reference: https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html
   private void Update()
   {
      if(nearestComponent != null)
         Debug.DrawLine(endEffector.transform.position,nearestComponent.transform.position, Color.green);
   }
}

//--------------------- Random Basic Notes ----------------------/
// MonoBehaviour Class
//   - the base calss from which every Unity script derives
//   - provides a framework which allows you to attach script to GameObject
//     in the editor
//   - reference: 
//       + https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
//       + https://docs.unity3d.com/Manual/class-MonoBehaviour.html
//
// Agent Class
//   - an agent is an actor that can
//       1) observe environment
//       2) decide best course of action using observation
//       3) execute those actions within environmnet
//   - agents in environment operate on "steps"
//   - at each steps, agent does 
//       1) collects observation
//       2) passes them to decision-making policy
//       3) receives an action in response
//   - assign decision-making policy to an agent with "BehaviorParameters"
//     component attached to agent's GameObject
//   - to trigger agent decision automatically, attach "DecisionRequester" 
//     component to agent GameObject
//       + "DecisionPeriod" = the frequency with which the agent requests a decision
//           Ex: DecisioPeriod of 5 means that Agent will requiest a decision very 5
//               Academy steps 
//       + "TakeActionsBetweenDecisions" = indicates if agent should take action
//             during the Academy steps where it does not request a decision
//   - reference: https://docs.unity3d.com/Packages/com.unity.ml-agents@1.0/api/Unity.MLAgents.Agent.html#Unity_MLAgents_Agent_OnActionReceived_System_Single___