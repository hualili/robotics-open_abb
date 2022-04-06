'''-------------------------------------------------------------------------------------------------------
* Company Name : CTI One Corporation                                                                     *
* Program name : CAPP_Kart_4_Observation.py (Testing)                                                    *
* Coded By     : YY                                                                                      *
* Date         : 2022-04-01                                                                              *
* Updated By   : YY                                                                                      *
* Date         :                                                                                         *
* Version      :                                                                                         *
* Copyright    : Copyright (c) 2022 CTI One Corporation                                                  *
* Purpose      : Integrate reward function Send an direction number to Unity using ZeroMQ.               *
*              : NO ERROR HANDLING, multithreading for using multiple TCP ports                          *
*              :                                                                                         *
*              : v1.0.0 2022-04-01 YY Create from CAPP_Kart_3_Destination.py. Add observation values     *
-------------------------------------------------------------------------------------------------------'''

from os import path
from threading import Thread
import math
import traceback
import datetime
import zmq


GOAL_MESSAGE = b'GOAL'
RESET_MESSAGE = 'reset'

BASE_TCP_PORT = 8000           # For sending messages to Unity
TCP_PORT_LEN = 20

# To reset values, these variable need to be global
# heuristic_value = 0                 # Total Distance
# angleList = []                      # Angle list
# rewardList = []                     # Reward value list


# Reset the robot position in Unity and Python side
'''
def resetRobotPosition():
    global heuristic_value, angleList, rewardList, robot_position, point_A

    heuristic_value = 0
    angleList = []
    rewardList = []
    robot_position[0] = point_A[0]
    robot_position[1] = point_A[1]
'''


# 2022-3-15 YY Integrate from BP code
def get_intersection_point(pointP, pointA, pointB):
    a = pointA
    b = pointB

    xd = b[0] - a[0]
    yd = b[1] - a[1]

    a = xd
    b = yd

    c = (a * pointB[1]) - (b * pointB[0])

    px = pointP[0]
    py = pointP[1]

    lambda_ = -((a * py) - ((b * px) + c)) * ((a ** 2 + b ** 2) ** -1)
    # print("lambda_:", lambda_ )
    x_prime = px + (lambda_ * -b)
    y_prime = py + (lambda_ * a)

    intersection_point = [0, 0]
    intersection_point[0] = x_prime
    intersection_point[1] = y_prime

    return intersection_point


# 2022-3-15 YY Integrate from BP code
def getAngle(pointA, pointB, currentPosition):
    v1x = pointA[0] - pointB[0]
    v1y = pointA[1] - pointB[1]

    v2x = pointA[0] - currentPosition[0]
    v2y = pointA[1] - currentPosition[1]

    numv = v1x*v2x + v1y*v2y
    denv = math.sqrt(v1x**2 + v1y**2) * math.sqrt(v2x**2 + v2y**2)

    # 2022-03-22 YY Add to check if denv is zero to avoid division by zero
    if denv == 0:
        theta = 0
    else:
        # 2022-04-04 YY Add to check math domain error
        try:
            numvdenv = numv / denv
            if numvdenv > 1:
                numvdenv = 1
            elif numvdenv < -1:
                numvdenv = -1

            theta = math.acos(numvdenv)
        except Exception as e:
            print("Error #####: ", e)
            print("pointA: ", pointA, " pointB:", pointB, " currentPosition:", currentPosition)
            print("Error #####: ", traceback.format_exc())

        # theta = math.acos(numv / denv)

    theta = round(math.degrees(theta))

    # print("angle = " + str(theta))

    return theta


# 2022-3-15 YY Integrate from BP code
# 2022-3-22 YY Update reward function provided by HL
def getReward(angle):
    if angle >= 0:
        reward = -(1/90) * angle + 1
    else:
        reward = (1/90) * angle + 1

    return reward


def getDistanceFromDestination(destPointB, robotPosition):
    destinationDistance = math.sqrt((destPointB[0] - robotPosition[0]) ** 2 +
                                    (destPointB[1] - robotPosition[1]) ** 2)

    return destinationDistance


# calculateReward() is run in a child thread
def calculateReward(tcpPort):
    # global context, heuristic_value, angleList, rewardList, robot_position, point_A, point_B

    robot_position = [0, 0]     # Robot position in Unity
    point_A = [0, 0]            # Point A position in Unity
    point_B = [0, 0]            # Point B position in Unity
    previous_robot_position = [0, 0]    # previous robot position

    # distance reward and observation value for sending back data to Unity
    distanceReward = 1          # 1 or -1, for multiplying to the angle reward
    distanceObservation = 1     # 0 or 1

    context = zmq.Context()
    server_socket = context.socket(zmq.REP)
    server_socket.bind("tcp://127.0.0.1:" + tcpPort)


    sending_count = 0

    while True:
        # Recive the robot position
        message = server_socket.recv()
        messageList = message.decode("ascii").split(',')

        # Set Point A and B
        point_A[0] = float(messageList[0])
        point_A[1] = float(messageList[1])
        point_B[0] = float(messageList[2])
        point_B[1] = float(messageList[3])
        robot_position[0] = float(messageList[4])
        robot_position[1] = float(messageList[5])
        previous_robot_position[0] = float(messageList[6])
        previous_robot_position[1] = float(messageList[7])

        end_point_perpendicular_line = get_intersection_point(robot_position, point_A,
                                                              point_B)

        distance = math.sqrt((end_point_perpendicular_line[0] - robot_position[0]) ** 2 +
                             (end_point_perpendicular_line[1] - robot_position[1]) ** 2)

        # 2022-03-28 YY get the distance from Point B to current robot position and previous robot position
        currentDistPointB = getDistanceFromDestination(point_B, robot_position)
        previousDistPointB = getDistanceFromDestination(point_B, previous_robot_position)
        distanceDifference = currentDistPointB - previousDistPointB

        # set distance reward as 1 or -1, set distance observation value
        if distanceDifference < 0:
            distanceReward = 1
            distanceObservation = 1
        else:
            distanceReward = -1
            distanceObservation = 0

        # heuristic_value = heuristic_value + distance

        angleFormed = getAngle(point_A, point_B, robot_position)
        normalizedAngle = angleFormed/180
        # angleList.append(angleFormed)

        # 2022-03-28 YY multiply distance reward to the reward
        # currentReward = getReward(angleFormed)
        currentReward = getReward(angleFormed)

        if currentReward > 0:       # if the angle reward is positive, multiply distanceReward
            currentReward = currentReward*distanceReward

        # rewardList.append(currentReward)
        rewardStr = str(currentReward)
        # 2022-03-23 YY send back reward, angle, distance value(approaching/leaving)
        rewardByts = bytes(rewardStr + "," + str(normalizedAngle) + "," + str(distanceObservation), 'ascii')
        # rewardByts = bytes(str(angleFormed) + ',' + rewardStr, 'ascii')
        # Send back a reward
        # time.sleep(0.0001)
        server_socket.send(rewardByts)

        sending_count += 1
        # print("#######################################")
        # print("x: ", robot_position[0], "z: ", robot_position[1])
        # print("end_point_perpendicular_line:", end_point_perpendicular_line)
        # print("Distance: ", distance)
        # print("Angle: ", angleFormed)
        if sending_count % 1000 == 0:
            print("Reward: ", currentReward, " Normalized angle: ", normalizedAngle, " distanceObservation",
                  distanceObservation, " ", sending_count, " ",
                  datetime.datetime.now().strftime("%Y-%m-%d_%H-%M-%S")+"\n")

        #print("heuristic_value: ", heuristic_value)
        #print("angleList: ", angleList)
        #print("rewardList: ", rewardList)


# Start the reward function
# TCP_PORT_LEN = 20
for i in range(TCP_PORT_LEN):

    Thread(target=calculateReward, args=(str(BASE_TCP_PORT+i),)).start()

# calculateReward()
