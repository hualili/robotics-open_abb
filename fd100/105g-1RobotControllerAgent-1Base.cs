//----------------------------------------------------------------------------------/
// program    : RobotControllerAgent-1Base.cs                                       /
// coded by   : Unity (R. Kandas) https://github.com/rkandas/RobotArmMLAgentUnity   /
// updated by : Chee Vang, HL                                                       /
// copyrighted by : CTI One Corp.                                                   /
// version    : x0.1                                                                /
// status     : debug [], tested [x]                                                /
// note       : 1. Controller file for the 6 DoF robot arm                          /
//              2. Original file but with                                           /
//                    2.1) fixed starting and ending positions                      /
//                         of end effector                                          /
//                    2.2) data written into CSV file                               /
//              3. To have the same format as k2 CSV file, some                     /
//                 terms (e.g. t, beginDistance, etc) were kept                     /
//                 as 0 or empty                                                    /
//----------------------------------------------------------------------------------/ 
using System;
using System.Data.Common;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;
// CV 2021-6-15: File I/O
using System.IO;
using System.Text;

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
   private float beginDistance; // intial distance for each episode
   private float prevBest; // the distance closest to the target
   bool  inrange2 = false, inrange3 = false, inrange4 = false, inrange5 = false;
   private float baseAngle;
   private const float stepPenalty = -0.0001f;

   // CV 2021-6-15: File path for CSV file
   private string path = @"/home/chee/Documents/RobotArmMLAgentUnity_original/distance/distance-base-01.csv"; 

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
      // 2021-6-15: fixed starting endeffector position
      MoveToSafeUserPreDefinedPostion();
      //MoveToSafeRandomPosition();
      if (!trainingMode) MaxStep = 0;

      // CV, SS 2021-6-15: Label the columns in CSV file
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

      // 2021-6-15: fixed starting endeffector position
      MoveToSafeUserPreDefinedPostion();
      //MoveToSafeRandomPosition();
      UpdateNearestComponent();

      // CV 2021-6-15: Prints the initial data into CSV file
      float distance = Vector3.Distance(endEffector.transform.TransformPoint(Vector3.zero),
         nearestComponent.transform.position);
      float delta_distance = distance - prevBest; // CV 2021-5-19: delta distance
      float reward = 0;

      // CV 2021-6-15: write initial data into CSV file
      using(StreamWriter writer = new StreamWriter(path,true)) {
         writer.WriteLine( "" + "," + 
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
      // CV 2021-6-14: fixed target location
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

         // CV 2021-6-22: distance between endeffector and nearestComponent (target)
         float distance = Vector3.Distance(endEffector.transform.TransformPoint(Vector3.zero),
            nearestComponent.transform.position);
         // CV 2021-6-22: diff = total distance moved by endeffector from initial distance (beginDistance)
         float diff = beginDistance - distance; 
         
         // CV 2021-6-22: Checks if distance is moving away or towards the target by
         // comparing the current distance (distance) with the prevBest
         if (distance > prevBest)
         {
            // Penalty if the arm moves away from the closest position to target
            AddReward(prevBest - distance);

            // CV 2021-6-15: write data to CSV file
            using(StreamWriter writer = new StreamWriter(path,true)) {
               writer.WriteLine( "" + "," + // t DNE in this file
                                 beginDistance + "," + 
                                 prevBest + "," + 
                                 distance + "," + 
                                 "" + "," + // delta_distance DNE in this file
                                 (prevBest - distance) + "," +
                                 nearestComponent.transform.position.x + "," +
                                 nearestComponent.transform.position.y + "," +
                                 nearestComponent.transform.position.z + "," +
                                 endEffector.transform.position.x + "," +
                                 endEffector.transform.position.y + "," +
                                 endEffector.transform.position.z );
               writer.Close();
            }
         }
         else
         {
            // Reward if the arm moves closer to target
            AddReward(diff);
            // CV 2021-6-15: write data to CSV file
            using(StreamWriter writer = new StreamWriter(path,true)) {
               writer.WriteLine( "" + "," + // t DNE in this file
                                 beginDistance + "," + 
                                 prevBest + "," + 
                                 distance + "," + 
                                 "" + "," + // delta_distance DNE in this file
                                 diff + "," +
                                 nearestComponent.transform.position.x + "," +
                                 nearestComponent.transform.position.y + "," +
                                 nearestComponent.transform.position.z + "," +
                                 endEffector.transform.position.x + "," +
                                 endEffector.transform.position.y + "," +
                                 endEffector.transform.position.z );
               writer.Close();
            }
            prevBest = distance;
         }

         AddReward(stepPenalty);
      }
   }

   //------------------------------------------------------------/
   // Agent::OnActionReceived()
   //   - MLAgents C# function used to specify the agent behaviour at every 
   //     step based on the provided action
   //   - is called every time the agent receives an action to take
   //   - common to assign rewards in here
   //   - MLAgents API reference: https://docs.unity3d.com/Packages/com.unity.ml-agents@1.0/api/Unity.MLAgents.Agent.html#Unity_MLAgents_Agent_OnActionReceived_System_Single___
   //   - MLAgents Github reference: https://github.com/Unity-Technologies/ml-agents/blob/main/docs/Learning-Environment-Design-Agents.md
   public void GroundHitPenalty()
   {
      AddReward(-1f);
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
   // 2021-6-14: fixed endEffector starting position
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
