1.	VPN in to your corporate Windows Domain network
2.	Bring up your git bash shell
3.	Navigate to a folder where you want this application to run
4.	Make a directory here called ActiveDirectoryQuery – mkdir ActiveDirectoryQuery
5.	Issue a git clone https://github.com/p8cakes/ActiveDirectoryQuery.git command
6.	Change directory to ActiveDirectoryQuery – cd ActiveDirectoryQuery
7.	Do an ls here to verify that files have indeed been retrieved
8.	Open ActiveDirectoryQuery.csproj in Visual Studio Express 2013 for Windows Desktop (or the installed version of Visual Studio that you have)
9.	Edit App.Config that you have in this solution to change the CorporateDomain key value to the domain name of your company or institution
10.	Click on the Properties item on the right “Solutions Explorer” tab of Visual Studio.
11.	Click on Debug on the far-left pane to access the Command line arguments option.
12.	Enter your own login name to the network – say, jdoe for your name “John Doe” in the Command Line Arguments. We’ll be doing a deep query for just this samAccountName.
13.	Compile and run your program! Have a breakpoint on line #46 to view the Console Window output before the program terminates. 
