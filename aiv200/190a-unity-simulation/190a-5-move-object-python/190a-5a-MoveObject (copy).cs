/*--------------------------------------------------------------------------------------------------------
* Company Name : CTI One Corporation                                                                     *
* Program name : MoveObject.cs (Testing)                                                                 *
* Coded By     : YY                                                                                      *
* Date         : 2022-02-18                                                                              *
* Updated By   :                                                                                         *
* Date         :                                                                                         *
* Version      : v1.0.0                                                                                  *
* Copyright    : Copyright (c) 2022 CTI One Corporation                                                  *
* Purpose      : Move the object from Python program                                                     *
*              : v1.0.0 2022-02-18 YY Create                                                             *
---------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;

public class MoveObject : MonoBehaviour
{
    // Vector3 for objects moving up, down
    static Vector3 MOVE_VECTOR3_UP = new Vector3(0, 2, 0);
    static Vector3 MOVE_VECTOR3_DOWN = new Vector3(0, -2, 0);
    
    const int ACTION_STAY = 0;
    const int ACTION_UP = 1;
    const int ACTION_DOWN = 2;

    ResponseSocket responseSocket;

    // 0: Stay, 1: Up, 2: Down
    int actionNumber = 0;
    Thread serverThread;

    void Awake()
    {       
        ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet

        // Create Response(Server) socket
        responseSocket = new ResponseSocket("@tcp://localhost:8555");

        // Create a server thread for receiving messages from Python code
        ThreadStart serverThreadStart = new ThreadStart(GetRequest);
        serverThread = new Thread(serverThreadStart);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    // Update() is called once per frame
    void Update()
    {
        // Move the object up and down
        if(actionNumber == ACTION_UP){
            transform.position += MOVE_VECTOR3_UP;
            actionNumber = ACTION_STAY;

        } else if(actionNumber == ACTION_DOWN) {
            transform.position += MOVE_VECTOR3_DOWN;
            actionNumber = ACTION_STAY;
        }
    }

    // Child thread for communicate with Python
    void GetRequest(){
        while(true){
            // Receive a meesage and send it back
            var message = responseSocket.ReceiveFrameString();
            responseSocket.SendFrame(message);

            // Set the Action Numer, no error handling
            actionNumber = Int32.Parse(message);
        }
    }
    // OnDisable() is called when the behaviour becomes disabled.
    void OnDisable(){
        try{
            // Stop receiving thread
            serverThread.Abort();
            // Close the server socket
            responseSocket.Close();
            NetMQConfig.Cleanup(false);
        }catch(Exception exp){
             if (exp.Source != null){
                Console.WriteLine("Exeption source: {0}", exp.Source);
            }
        }
    }
}
