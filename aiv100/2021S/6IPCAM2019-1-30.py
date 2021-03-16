'''
https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_gui/py_video_display/py_video_display.html
'''

import numpy as np
import cv2

cap = cv2.VideoCapture(0)
#cap = cv2.VideoCapture('rtsp://admin:admin123@192.168.1.62/1')

while(True):
    ret, frame = cap.read() 
 
    cv2.imshow('original',frame)  
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break
 
cap.release()
cv2.destroyAllWindows()
