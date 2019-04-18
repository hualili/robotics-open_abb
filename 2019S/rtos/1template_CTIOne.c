/*
 * program name:  
 * coded by    :  
 * date        : 
 * purpose     : 
 * Copyright(C) declaration 
 * All rights reserved.
 *
 * @par
 * This software is supplied "AS IS" without any warranties of
 * any kind, and CTI One (the company) and its licensor disclaim any and
 * all warranties, express or implied, including all implied warranties of
 * merchantability, fitness for a particular purpose and non-infringement of
 * intellectual property rights.  CTI One (the company) assumes no responsibility
 * or liability for the use of the software, conveys no license or rights under any
 * patent, copyright, mask work right, or any other intellectual property rights in
 * or to any products. CTI One (the company) reserves the right to make changes
 * in the software without notification. CTI One (the company) also makes no
 * representation or warranty that such application will be suitable for the
 * specified use without further testing or modification.
 *
 * @par
 * Permission to use, copy, modify, and distribute this software and its
 * documentation is hereby granted, under ??? (the Company's) and its
 * licensor's relevant copyrights in the software, without fee, provided that it
 * is used in conjunction with the company's product license agreement.  This
 * copyright, permission, and disclaimer notice must appear in all copies of
 * this code.
 */

/*****************************************************************************
 * include section
 ****************************************************************************/
 #include "board.h"

/*****************************************************************************
 * Private types/enumerations/variables
 ****************************************************************************/


/*****************************************************************************
 * Public types/enumerations/variables
 ****************************************************************************/


/*****************************************************************************
 * Private functions
 ****************************************************************************/
/* Sets up system hardware */
static void prvSetupHardware(void)
{
	SystemCoreClockUpdate();
	Board_Init();
	Board_LED_Set(0, false); /* Initial LED0 state is off */
}

/*****************************************************************************
 * Public functions
 ****************************************************************************/

/**
 * @brief	main routine for FreeRTOS blinky example
 * @return	Nothing, function should not exit
 */
int main(void)
{
	 
	return 0;
}

/**
 * @}
 */
