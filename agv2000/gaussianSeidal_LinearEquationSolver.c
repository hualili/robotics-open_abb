//---------------------------------------------------//
// Program: LinearGS.c                               //  
// Coded by: HL;                                     //
// Date: Nov 24, 2015;                               //
// Status: tested.                                   // 
// Note: this program solves for linear system based //   
//       on Gaussian Seidel technique, in particular //
//       for solving 3x3 linear systems, e.g.,       // 
//       AX = B                                      //
//       where A is a 3x3 matrix.                    //   
//---------------------------------------------------//
/* compilation and build
gcc -Wall linearGS.c -o main.o 
*/ 
#include <stdio.h>
#include <math.h>
#include <stdlib.h>  // for abs funciton  

#define ARRAY_LEN 4  //define array length, matrix is 3x3, but we dont 
                     //want to use 0-index  

//---------------------------------------------------//
// Parameter passing                                 //  
//---------------------------------------------------//
typedef struct
{
double A_Matrix[ARRAY_LEN][ARRAY_LEN];
double X_unknown[ARRAY_LEN];
double B_Col[ARRAY_LEN];
double convergentStop;  
} MY_STRUCT;  

void getGSPts( MY_STRUCT * );  // subroutine prototype  

//--------------------------------------------//
// main module                                //
//--------------------------------------------//

//-------define 4 feature points locations----// 
#define x_1 3  
#define y_1 0  
#define x_2 2  
#define y_2 1 
#define x_3 2  
#define y_3 2  
#define x_4 1  
#define y_4 4  

int main()
{
MY_STRUCT my_struct; 
int index; 

   printf("---------------------------------------------------------\n");
   printf("Computation of AX=B for 3x3 Linear System by GS Technique\n");

   //----------------------------------------------------------------
   // Preprocessing to form A (3x3) matrix and B (3x1) column vector  
   //----------------------------------------------------------------
     
   my_struct.convergentStop = 0.000000001;
   
   my_struct.A_Matrix[0][1] = 0;
   my_struct.A_Matrix[0][2] = 0;
   my_struct.A_Matrix[0][3] = 0;

   my_struct.X_unknown[1] = 0;
   my_struct.X_unknown[2] = 0;
   my_struct.X_unknown[3] = 0;

   my_struct.A_Matrix[1][1] = 4;
   my_struct.A_Matrix[1][2] = y_1 + y_2 + y_3 + y_4;
   my_struct.A_Matrix[1][3] = y_1*y_1 + y_2*y_2 + y_3*y_3 + y_4*y_4;
   my_struct.A_Matrix[2][1] = my_struct.A_Matrix[1][2];
   my_struct.A_Matrix[2][2] = my_struct.A_Matrix[1][3];
   my_struct.A_Matrix[2][3] = y_1*y_1*y_1 + y_2*y_2*y_2 + y_3*y_3*y_3 + y_4*y_4*y_4;
   my_struct.A_Matrix[3][1] = my_struct.A_Matrix[1][3];
   my_struct.A_Matrix[3][2] = my_struct.A_Matrix[2][3];
   my_struct.A_Matrix[3][3] = y_1*y_1*y_1*y_1 + y_2*y_2*y_2*y_2  
                                    + y_3*y_3*y_3*y_3 + y_4*y_4*y_4*y_4;
   my_struct.B_Col[0] = 0;
   my_struct.B_Col[1] = x_1 + x_2 + x_3 + x_4 ;
   my_struct.B_Col[2] = x_1*y_1 + x_2*y_2 + x_3*y_3 + x_4*y_4 ;
   my_struct.B_Col[3] = x_1*y_1*y_1 + x_2*y_2*y_2 + x_3*y_3*y_3 + x_4*y_4*y_4 ;

//   for(index = 1; index <= 3; index++) {
//   printf( "index i = %4d B = %4f \n", index, 
//                                my_struct.B_Col[index]);
//}

   printf( "Stop Criterion: Epsilon = %8f \n", my_struct.convergentStop);
   getGSPts( &my_struct );  
   
   for(index = 1; index <= 3; index++) {
   printf( "index i = %4d result x = %4f \n", index, 
                                my_struct.X_unknown[index]);
} //for     
   return 0; 
}// main  

//---------------------------------------------------------------- 
void getGSPts( MY_STRUCT *xx )   
{
double previ_x1 = 1.0; 
double previ_x2 = 1.0; 
double previ_x3 = 1.0; 
double X_accumulator = 100.0; 
int   i; 

i=1; 
//printf( "Stop Criterion: Epsilon = %4f \n", xx->convergentStop);

//while ( X_accumulator > xx->convergentStop ) {  
while ((1000-i) >=0 ) {  
      X_accumulator = abs(previ_x1 - xx-> X_unknown[1]) +  
                      abs(previ_x2 - xx-> X_unknown[2]) +  
                      abs(previ_x3 - xx-> X_unknown[3]) ;  
      xx->X_unknown[1] = 
       ( xx-> B_Col[1] - xx-> A_Matrix[1][2]* xx->X_unknown[2] 
         - xx-> A_Matrix[1][3]* xx->X_unknown[3])/xx->A_Matrix[1][1] ;  
      printf( "X1 difference = %8f \n",previ_x1-xx->X_unknown[1]);
      previ_x1 = xx->X_unknown[1];  

      xx->X_unknown[2] = 
       ( xx-> B_Col[2] - xx-> A_Matrix[2][1]* xx->X_unknown[1] 
         - xx-> A_Matrix[2][3]* xx->X_unknown[3])/xx->A_Matrix[2][2] ;  
      printf( "X2 difference = %8f \n",previ_x2-xx->X_unknown[2]);
      previ_x2 = xx->X_unknown[2];  

      xx->X_unknown[3] = 
       ( xx-> B_Col[3] - xx-> A_Matrix[3][1]* xx->X_unknown[1] 
         - xx-> A_Matrix[3][2]* xx->X_unknown[2])/xx->A_Matrix[3][3] ;  
      printf( "X3 difference = %8f \n",previ_x3-xx->X_unknown[3]);
      previ_x3 = xx->X_unknown[3];  

      printf( "Iteration i = %4d X_accumulator = %8f \n",i,X_accumulator);
      i++;   
} //while loop 
} // subroutine  
