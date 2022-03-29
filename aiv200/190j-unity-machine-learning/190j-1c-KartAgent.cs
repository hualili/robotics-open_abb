/*--------------------------------------------------------------------------------------------------------
* Company Name : CTI One Corporation                                                                     *
* Program name : KartAgent.cs (Testing)                                                                  *
* Coded By     :                                                                                         *
* Date         :                                                                                         *
* Updated By   : YY                                                                                      *
* Date         : 2022-03-10                                                                              *
* Version      : v1.0.0                                                                                  *
* Copyright    : Copyright (c) 2022 CTI One Corporation                                                  *
* Purpose      : Kart AI part modified by CTI One                                                        *
*              :                                                                                         *
*              : v1.0.0 2022-03-23 YY Modified CAPP                                                      *
---------------------------------------------------------------------------------------------------------*/

using System;
using System.Threading;
using KartGame.KartSystems;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

using AsyncIO;
using NetMQ;
using NetMQ.Sockets;

namespace KartGame.AI
{
    /// <summary>
    /// Sensors hold information such as the position of rotation of the origin of the raycast and its hit threshold
    /// to consider a "crash".
    /// </summary>
    [System.Serializable]
    public struct Sensor
    {
        public Transform Transform;
        public float RayDistance;
        public float HitValidationDistance;
    }

    /// <summary>
    /// We only want certain behaviours when the agent runs.
    /// Training would allow certain functions such as OnAgentReset() be called and execute, while Inferencing will
    /// assume that the agent will continuously run and not reset.
    /// </summary>
    public enum AgentMode
    {
        Training,
        Inferencing
    }

    /// <summary>
    /// The KartAgent will drive the inputs for the KartController.
    /// </summary>
    public class KartAgent : Agent, IInput
    {
#region Training Modes
        [Tooltip("Are we training the agent or is the agent production ready?")]
        public AgentMode Mode = AgentMode.Training;
        [Tooltip("What is the initial checkpoint the agent will go to? This value is only for inferencing.")]
        public ushort InitCheckpointIndex;

#endregion

#region Senses
        [Header("Observation Params")]
        [Tooltip("What objects should the raycasts hit and detect?")]
        public LayerMask Mask;
        [Tooltip("Sensors contain ray information to sense out the world, you can have as many sensors as you need.")]
        public Sensor[] Sensors;
        [Header("Checkpoints"), Tooltip("What are the series of checkpoints for the agent to seek and pass through?")]
        public Collider[] Colliders;
        [Tooltip("What layer are the checkpoints on? This should be an exclusive layer for the agent to use.")]
        public LayerMask CheckpointMask;

        [Space]
        [Tooltip("Would the agent need a custom transform to be able to raycast and hit the track? " +
            "If not assigned, then the root transform will be used.")]
        public Transform AgentSensorTransform;
#endregion

#region Rewards
        [Header("Rewards"), Tooltip("What penatly is given when the agent crashes?")]
        public float HitPenalty = -1f;
        [Tooltip("How much reward is given when the agent successfully passes the checkpoints?")]
        public float PassCheckpointReward;
        [Tooltip("Should typically be a small value, but we reward the agent for moving in the right direction.")]
        public float TowardsCheckpointReward;
        [Tooltip("Typically if the agent moves faster, we want to reward it for finishing the track quickly.")]
        public float SpeedReward;
        [Tooltip("Reward the agent when it keeps accelerating")]
        public float AccelerationReward;
        [Tooltip("Reward the agent reaces the Point B(Goal)")]
        public float GOAL_REWARD = 1.0f;         // 2022-3-24 YY Reward for reaching the goal(Point B)
#endregion
#region Room
        [Header("Room"), Tooltip("Point B(Goal) size")]
        public float GOAL_SIZE = 3.0f;           // The goal point size on the point B, 3x3. float type, Vector3 x, y, z are float
        [Tooltip("Sending the kart position to Python code to get reward")]
        public bool REWARD_CAPP_FLAG = true;    // Sending the kart position to Python code to get reward
#endregion
#region ResetParams
        [Header("Inference Reset Params")]
        [Tooltip("What is the unique mask that the agent should detect when it falls out of the track?")]
        public LayerMask OutOfBoundsMask;
        [Tooltip("What are the layers we want to detect for the track and the ground?")]
        public LayerMask TrackMask;
        [Tooltip("How far should the ray be when casted? For larger karts - this value should be larger too.")]
        public float GroundCastDistance;
#endregion

#region Debugging
        [Header("Debug Option")] [Tooltip("Should we visualize the rays that the agent draws?")]
        public bool ShowRaycasts;
#endregion

        ArcadeKart m_Kart;
        bool m_Acceleration;
        bool m_Brake;
        float m_Steering;
        int m_CheckpointIndex;

        bool m_EndEpisode;
        float m_LastAccumulatedReward;

        // 2022-03-21 YY Add
        GameObject point_a;
        GameObject point_b;
        const String TCP_PORT_COORDINATOR = "8000";     // For sending coordinators from Unity. String or string???
        String sendingTcpPort = "8000";           
        const int BASE_TCP_PORT = 8000;         // BASE TCP Port number to communicate the reward server(Python program)
        const int NUM_TCP_PORT = 20;            // The number of the listen tcp port on the reward server 
        RequestSocket clientSocket;             // For sending Robot coordinate from Unity to Python
        Thread sendingThread;
        Vector3 initialPoint;                   // Initial Point (on Point A)

        static bool send_initial_flag = false;  // for sending Point A and B position onece

        bool sending_flag = false;              // set true while sending kart position to the reward server(Python program)
        String positionX = "";                  // Kart position X to send to the reward server 
        String positionZ = "";                  // Kart position Z to send to the reward server
        void Awake()
        {
            m_Kart = GetComponent<ArcadeKart>();
            if (AgentSensorTransform == null) AgentSensorTransform = transform;

            if( Mode == AgentMode.Training && REWARD_CAPP_FLAG){
                // get TCP Port number from Object name
                int startIndex = name.IndexOf("(");               
                int endIndex = name.IndexOf(")");
                int additionalNumTcpPort = 0;       // Additional number for tcp port

                // Debug.Log("Object Name:" + name);
                // Debug.Log("tcpPortIndex:" + startIndex.ToString());
                // Debug.Log("endIndex:" + endIndex.ToString());
                if( startIndex != -1 && endIndex != -1){
                    String tcpPortIndex = name.Substring(startIndex+1, endIndex-startIndex-1);
                    Debug.Log("tcpPortIndex:" + tcpPortIndex);
                    int intTcpPortIndex = int.Parse(tcpPortIndex);

                    if(intTcpPortIndex < NUM_TCP_PORT){
                        additionalNumTcpPort = intTcpPortIndex;
                    }
                    
                }
                sendingTcpPort = (BASE_TCP_PORT+additionalNumTcpPort).ToString();

                clientSocket = new RequestSocket();
                clientSocket.Connect("tcp://localhost:" + sendingTcpPort);
                // clientSocket.Connect("tcp://localhost:" + TCP_PORT_COORDINATOR);

                ThreadStart sendingThreadStart = new ThreadStart(SendKartPosition);
                sendingThread = new Thread(sendingThreadStart);
                sendingThread.IsBackground = true;
                sendingThread.Start();
            }
        }

        void Start()
        {
            
            // If the agent is training, then at the start of the simulation, pick a random checkpoint to train the agent.
            OnEpisodeBegin();

            if (Mode == AgentMode.Inferencing) m_CheckpointIndex = InitCheckpointIndex;

            // 2022-03-21 YY Add
            point_a = GameObject.Find("Point_A");
            point_b = GameObject.Find("Point_B");
            Debug.Log("Point A World Coordinator: " + point_a.transform.position.ToString());
            Debug.Log("Point B World Coordinator: " + point_b.transform.position.ToString());

            Debug.Log("GOAL_SIZE:" + GOAL_SIZE.ToString());
            Debug.Log("GOAL_REWARD:" + GOAL_REWARD.ToString());

            // set the initial point :
            initialPoint = new Vector3(point_a.transform.position.x, transform.position.y, point_a.transform.position.z);

            if(Mode == AgentMode.Training  && !send_initial_flag && REWARD_CAPP_FLAG){

                send_initial_flag = true;
                
                // send Point A and B coordinator
                clientSocket.SendFrame(point_a.transform.position.x.ToString() + "," + point_a.transform.position.z.ToString() + "," + 
                                        point_b.transform.position.x.ToString() + "," + point_b.transform.position.z.ToString()  );
            
                TimeSpan receiveTimeout = TimeSpan.FromMilliseconds(500);
                bool gotMessage = false;
                String message;
                gotMessage = clientSocket.TryReceiveFrameString(receiveTimeout, out message);

                Debug.Log("Sending Point A and B position: " + message);

            }

        }

        void Update()
        {
            if (m_EndEpisode)
            {
                Debug.Log("Update() EndEpisode Hit Penalty:" + m_LastAccumulatedReward.ToString());
                m_EndEpisode = false;
                // YY m_LastAccumulatedReward is accumulated hit penalty
                AddReward(m_LastAccumulatedReward);
                EndEpisode();
                OnEpisodeBegin();
            }
        }

        void LateUpdate()
        {
            switch (Mode)
            {
                case AgentMode.Inferencing:
                    if (ShowRaycasts) 
                        Debug.DrawRay(transform.position, Vector3.down * GroundCastDistance, Color.cyan);

                    // We want to place the agent back on the track if the agent happens to launch itself outside of the track.
                    if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out var hit, GroundCastDistance, TrackMask)
                        && ((1 << hit.collider.gameObject.layer) & OutOfBoundsMask) > 0)
                    {
                        // Reset the agent back to its last known agent checkpoint
                        var checkpoint = Colliders[m_CheckpointIndex].transform;
                        transform.localRotation = checkpoint.rotation;
                        transform.position = checkpoint.position;
                        m_Kart.Rigidbody.velocity = default;
                        m_Steering = 0f;
						m_Acceleration = m_Brake = false; 
                    }

                    break;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            var maskedValue = 1 << other.gameObject.layer;
            var triggered = maskedValue & CheckpointMask;

            FindCheckpointIndex(other, out var index);

            // Ensure that the agent touched the checkpoint and the new index is greater than the m_CheckpointIndex.
            if (triggered > 0 && index > m_CheckpointIndex || index == 0 && m_CheckpointIndex == Colliders.Length - 1)
            {
                // 2022-03-22 YY Comment out reward function
                // AddReward(PassCheckpointReward);
                m_CheckpointIndex = index;
            }
        }

        void FindCheckpointIndex(Collider checkPoint, out int index)
        {
            for (int i = 0; i < Colliders.Length; i++)
            {
                if (Colliders[i].GetInstanceID() == checkPoint.GetInstanceID())
                {
                    index = i;
                    return;
                }
            }
            index = -1;
        }

        float Sign(float value)
        {
            if (value > 0)
            {
                return 1;
            } 
            if (value < 0)
            {
                return -1;
            }
            return 0;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(m_Kart.LocalSpeed());

            // Add an observation for direction of the agent to the next checkpoint.
            var next = (m_CheckpointIndex + 1) % Colliders.Length;
            var nextCollider = Colliders[next];
            if (nextCollider == null)
                return;

            var direction = (nextCollider.transform.position - m_Kart.transform.position).normalized;
            sensor.AddObservation(Vector3.Dot(m_Kart.Rigidbody.velocity.normalized, direction));

            if (ShowRaycasts)
                Debug.DrawLine(AgentSensorTransform.position, nextCollider.transform.position, Color.magenta);

            m_LastAccumulatedReward = 0.0f;
            m_EndEpisode = false;
            for (var i = 0; i < Sensors.Length; i++)
            {
                var current = Sensors[i];
                var xform = current.Transform;
                var hit = Physics.Raycast(AgentSensorTransform.position, xform.forward, out var hitInfo,
                    current.RayDistance, Mask, QueryTriggerInteraction.Ignore);

                if (ShowRaycasts)
                {
                    Debug.DrawRay(AgentSensorTransform.position, xform.forward * current.RayDistance, Color.green);
                    Debug.DrawRay(AgentSensorTransform.position, xform.forward * current.HitValidationDistance, 
                        Color.red);

                    if (hit && hitInfo.distance < current.HitValidationDistance)
                    {
                        Debug.DrawRay(hitInfo.point, Vector3.up * 3.0f, Color.blue);
                    }
                }

                if (hit)
                {
                    Debug.Log("Name:" + name + "hit distance " + i.ToString() +":" + hitInfo.distance );
                    if (hitInfo.distance < current.HitValidationDistance)
                    {
                        m_LastAccumulatedReward += HitPenalty;
                        m_EndEpisode = true;
                        // Debug.Log("Hit Penalty:" + m_LastAccumulatedReward.ToString());
                    }
                }

                sensor.AddObservation(hit ? hitInfo.distance : current.RayDistance);
            }

            sensor.AddObservation(m_Acceleration);

            // 2022-03-22 YY Add Check if a machine reaches the goal (Point B)
            if( ( point_b.transform.position.x - GOAL_SIZE <= transform.position.x &&
                  transform.position.x <= point_b.transform.position.x + GOAL_SIZE )
                  &&
                ( point_b.transform.position.z - GOAL_SIZE <= transform.position.z &&
                  transform.position.z <= point_b.transform.position.z + GOAL_SIZE ))
            {
                Debug.Log("GOAL");
                m_LastAccumulatedReward += GOAL_REWARD;
                // m_EndEpisode = true;
                transform.position = initialPoint;
            }

        }

        public override void OnActionReceived(float[] vectorAction)
        {
            base.OnActionReceived(vectorAction);
            InterpretDiscreteActions(vectorAction);

            // Debug.Log("OnActionReceived:");

            // Find the next checkpoint when registering the current checkpoint that the agent has passed.
            var next = (m_CheckpointIndex + 1) % Colliders.Length;
            var nextCollider = Colliders[next];
            var direction = (nextCollider.transform.position - m_Kart.transform.position).normalized;
            var reward = Vector3.Dot(m_Kart.Rigidbody.velocity.normalized, direction);

            if (ShowRaycasts) Debug.DrawRay(AgentSensorTransform.position, m_Kart.Rigidbody.velocity, Color.blue);

            // 2022-03-22 YY Comment out reward function
            // Add rewards if the agent is heading in the right direction
            AddReward(reward * TowardsCheckpointReward);
            //Debug.Log("Towards Reward:" + (reward * TowardsCheckpointReward).ToString());
            //Debug.Log("Reward:" + reward.ToString());

            AddReward((m_Acceleration && !m_Brake ? 1.0f : 0.0f) * AccelerationReward);
            AddReward(m_Kart.LocalSpeed() * SpeedReward);

            if( Mode == AgentMode.Training && REWARD_CAPP_FLAG){
                // 2022-03-25 YY Modify to multithread
                while(sending_flag){                // Wait for finishing previous sending the coordinators 
                    Thread.Sleep(1);                // Sleep 1 milliseconds
                }
                // set Kart position X(right) and Z(forward) to class variables
                positionX = m_Kart.transform.position.x.ToString();
                positionZ = m_Kart.transform.position.z.ToString();
                sending_flag = true;

            }
            /*
            // 2022-03-21 YY Add
            // Send the machine postion in World Coordinator
            if( Mode == AgentMode.Training && REWARD_CAPP_FLAG){
                clientSocket.SendFrame(m_Kart.transform.position.x.ToString() + "," + m_Kart.transform.position.z.ToString() );
                TimeSpan receiveTimeout = TimeSpan.FromMilliseconds(500);
                bool gotMessage = false;
                String message;

                gotMessage = clientSocket.TryReceiveFrameString(receiveTimeout, out message);
                if(gotMessage){
                    float rewardCAPP = float.Parse(message);
                    AddReward(rewardCAPP);
                }else{
                    // The below error happens after the message could not receive
                    // FiniteStateMachineException: Req.XSend - cannot send another request 
                    Debug.Log("Message was not received!!!!");

                    clientSocket.Close();
                    clientSocket = new RequestSocket();     // Just Connect() function does not work. need to recreate the socket
                    clientSocket.Connect("tcp://localhost:" + TCP_PORT_COORDINATOR);

                    // gotMessage = clientSocket.TryReceiveFrameString(receiveTimeout, out message);
                    // reset the connection
                }
            }*/
        }

        public override void OnEpisodeBegin()
        {
            switch (Mode)
            {
                case AgentMode.Training:
                    
                    m_CheckpointIndex = Random.Range(0, Colliders.Length - 1);
                    var collider = Colliders[m_CheckpointIndex];

                    // 2022-03-22 YY Change the machine start position to Point A
                    // transform.localRotation = collider.transform.rotation;
                    // transform.position = collider.transform.position;
                    transform.position = initialPoint;
                    m_Kart.Rigidbody.velocity = default;
                    m_Acceleration = false;
                    m_Brake = false;
                    m_Steering = 0f;
                    break;
                default:
                    // 2022-03-24 YY In any case, Kart will get started at Point A
                    transform.position = initialPoint;
                    break;
            }
        }

        void InterpretDiscreteActions(float[] actions)
        {
            m_Steering = actions[0] - 1f;
            m_Acceleration = actions[1] >= 1.0f;
            m_Brake = actions[1] < 1.0f;
        }

        public InputData GenerateInput()
        {
            return new InputData
            {
                Accelerate = m_Acceleration,
                Brake = m_Brake,
                TurnInput = m_Steering
            };
        }

        void SendKartPosition(){
            while(true){

                if(sending_flag){
                    String sendingPositionX = positionX;
                    String sendingPositionZ = positionZ;
                    sending_flag = false;

                    clientSocket.SendFrame(sendingPositionX + "," + sendingPositionZ );
                    TimeSpan receiveTimeout = TimeSpan.FromMilliseconds(500);
                    bool gotMessage = false;
                    String message;

                    gotMessage = clientSocket.TryReceiveFrameString(receiveTimeout, out message);
                    if(gotMessage){
                        float rewardCAPP = float.Parse(message);
                        AddReward(rewardCAPP);
                    }else{
                        // The below error happens after the message could not receive
                        // FiniteStateMachineException: Req.XSend - cannot send another request 
                        Debug.Log("Message was not received!!!!");

                        clientSocket.Close();
                        clientSocket = new RequestSocket();     // Just Connect() function does not work. need to recreate the socket
                        clientSocket.Connect("tcp://localhost:" + sendingTcpPort);

                        // gotMessage = clientSocket.TryReceiveFrameString(receiveTimeout, out message);
                        // reset the connection
                    }
                }
            }
        }
    }
}
