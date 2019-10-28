/****************** SERVER CODE ****************/
/* Program: serverTCP.c;     Coded by: HL      *
 * Status : tested;          Date: Aug 2018    *
 * Compilation and build: gcc -o main.o test.c * 
 * Purpose: Demo to send command to LPCNOD     * 
 * To test: run server code first, then run    *
 *          the client code.                   * 
 ***********************************************/ 
#include <stdio.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <string.h>

int main(){
  int welcomeSocket, newSocket;
  char buffer[1024];
  struct sockaddr_in serverAddr;
  struct sockaddr_storage serverStorage;
  socklen_t addr_size;

  /*---- Create the socket ----*/
  /* 1) Internet domain 2) Stream socket 3) Default protocol TCP */
  welcomeSocket = socket(PF_INET, SOCK_STREAM, 0);
  
  serverAddr.sin_family = AF_INET;   // Address family = Internet
  serverAddr.sin_port = htons(7891); // Port number 
  serverAddr.sin_addr.s_addr = inet_addr("127.0.0.1");  //IP address (localhost)
  memset(serverAddr.sin_zero, '\0', sizeof serverAddr.sin_zero); //Set all paddingn bits to 0 

  /*---- Bind address struct to the socket ----*/
  bind(welcomeSocket, (struct sockaddr *) &serverAddr, sizeof(serverAddr));

  if(listen(welcomeSocket,5)==0) //Listen the socket, 5 max connection requests qued 
    printf("Listening\n");
  else
    printf("Error\n");

  /*---- Accept call creates a new socket for the incoming connection ----*/
  addr_size = sizeof serverStorage;
  newSocket = accept(welcomeSocket, (struct sockaddr *) &serverStorage, &addr_size);
  /*---- Send message to the socket of the incoming connection ----*/
  strcpy(buffer,"Harry: Send the command from VIDPAT to IP Node such as LPCNOD\n");
  send(newSocket,buffer,100,0);
  return 0;
}
