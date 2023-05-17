'''
-------------------------------------------------------
Program        : 102b-turin_telnet.py;
Coded by       : SH and Shuwen Zheng, CTI One Corporation
          Santa Clara, CA 95051
Last updated by: Berry Jia, CTI One Corporation; 
Last tested by : Harry Li, SJSU. 
Last updated date: Feb. 2021;
Copyright      : CTI One Corporation. 
Note           : this code is a part of the fd-100 project  
      of the CTI One Corporation. The code is released
      for the education use by the CTI One Corporation. 
-------------------------------------------------------
'''
import getpass
import sys
import telnetlib
import time

HOST = '192.168.0.13'
PORT = 8527
tn = telnetlib.Telnet(HOST, PORT, timeout = 1)
print()
print("Connected : ", tn)
print()
#print (tn.read_all())
login_str = str('<Bodys> <Cmd Name="Login" Status="Send"><Param UserName = "administrator" Password="12345678"/></Cmd> </Bodys>')
print(login_str)
print("Authentication Successful")
tn.write((login_str+"\n").encode('ascii'))
print("Logged In to the TURIN System")

while True:
    print("Available Tasks:")
    print("1. Trajectory1")
    print("2. Trajectory2")
    print("3. Stop\n")
    print("Enter Task No: ")
    task=int(input())

    if task==1:
        action_str = str('<Bodys><Cmd Name="MotionEnd" Status="Send"/></Bodys>')
        tn.write((action_str+"\n").encode('ascii'))
        time.sleep(1)
        action_str = str('<Bodys> <Cmd Name="MotionBegin" Status="Send"> <Param FileName = "test1-05-03-2019.txt" StartLine = "0"/> </Cmd> </Bodys>')
        tn.write((action_str+"\n").encode('ascii'))
        print("Task1 sent.\n\n")
        #print (tn.read_until(b'/Bodys'))

    if task==2:
        action_str = str('<Bodys><Cmd Name="MotionEnd" Status="Send"/></Bodys>')
        tn.write((action_str+"\n").encode('ascii'))
        time.sleep(1)
        action_str2 = str('<Bodys> <Cmd Name="MotionBegin" Status="Send"> <Param FileName = "test1-05-17-2019.txt" StartLine = "0"/> </Cmd> </Bodys>')
        tn.write((action_str2+"\n").encode('ascii'))
        print("Task2 sent.\n\n")

    if task==3:
        action_str = str('<Bodys><Cmd Name="MotionEnd" Status="Send"/></Bodys>')
        tn.write((action_str+"\n").encode('ascii'))
        print("ARM Mobility Stopped.")
