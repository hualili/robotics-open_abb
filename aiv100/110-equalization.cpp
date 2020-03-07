/*------------------------------------------------------*
 * Code: equalization.cpp                               *
 * Coded by:  Harry Li;                                 *
 * Date: July 16, 2018;                                 *
 * Version: x0.1;                                       *
 * Status : release;                                    *
 * Purpose: 1. histogram equalization demo              *
 *------------------------------------------------------*/ 
 

//Uncomment the following line if you are compiling this code in Visual Studio
//#include "stdafx.h"

#include <opencv2/opencv.hpp>
#include <iostream>

using namespace cv;
using namespace std;

int main(int argc, char** argv)
{
    
  Mat image = imread( argv[1], 1 );
    if (image.empty())
    {
        cout << "Could not open or find the image" << endl;
        cin.get(); //wait for any key press
        return -1;
    }
  Mat image_gray; 
  cvtColor(image, image_gray, COLOR_BGR2GRAY); 

  //equalize the histogram
  Mat image_hist_equalized;
  equalizeHist(image_gray, image_hist_equalized); 
                      
  namedWindow("image", WINDOW_NORMAL); 
  namedWindow("histogram equalized", WINDOW_NORMAL);                       
  imshow("image", image);
  imshow("histogram equalized", image_hist_equalized);

    waitKey(0); // Wait for any keystroke in one of the windows

    destroyAllWindows(); //Destroy all open windows

    return 0;
}
