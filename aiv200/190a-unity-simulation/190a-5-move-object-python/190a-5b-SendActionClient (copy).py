'''-------------------------------------------------------------------------------------------------------
* Company Name : CTI One Corporation                                                                     *
* Program name : SendActionClient.py (Testing)                                                           *
* Coded By     : YY                                                                                      *
* Date         : 2022-02-18                                                                              *
* Updated By   :                                                                                         *
* Date         :                                                                                         *
* Version      : v1.0.0                                                                                  *
* Copyright    : Copyright (c) 2022 CTI One Corporation                                                  *
* Purpose      : Send an action number to Unity using ZeroMQ. NO ERROR HANDLING                          *
*              : v1.0.0 2022-02-18 YY Create                                                             *
-------------------------------------------------------------------------------------------------------'''

import zmq

context = zmq.Context()
socket = context.socket(zmq.REQ)
socket.connect("tcp://127.0.0.1:8555")

while True:
    actionMessage = input("Please enter the number; 1: Up, 2: Down\n")
    socket.send(actionMessage.encode('ascii'))   # ZeroMQ can send String in ASCII only. No Error Handling

    message = socket.recv()
    print("Received responce: ", message)
