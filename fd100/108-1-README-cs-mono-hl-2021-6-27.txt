'''++++++++++++++++++++++++++++++++++++++++++++++++++
'  File    : README-cs-mono.txt                     +
'  Date    : June 7, 2021                           +
'  Created by: HL                                   +
'  Purpose : To run CS for .NET on Linux            + 
'+++++++++++++++++++++++++++++++++++++++++++++++++'''
Ref: 
https://askubuntu.com/questions/1100537/how-can-i-compile-run-and-decompile-c-code-in-ubuntu-terminal

Note: You can use mono which is C# implementation and it is cross-platform support.
mono is open source. mono is sponsored by Ximian/Novell and is developed to be a platform 
independent version of the C# programming environment. While . NET is platform dependent, 
Mono allows developers to build Linux and cross- platform applications.

1. install mono as follows: 
sudo apt install apt-transport-https dirmngr

sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb https://download.mono-project.com/repo/ubuntu vs-bionic main" | sudo tee /etc/apt/sources.list.d/mono-official-vs.list

sudo apt update

sudo apt install mono-complete

2. Compile cs file to executable
 
$mcs -out:test.exe test.cs   

3. Run the executable

$mono test.exe 

Note: Decompile the executable file.
monodis --output=decompiled-hello.txt hello.exe
 
