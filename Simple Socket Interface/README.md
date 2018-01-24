# Code Documentation
All scripts contain comments about their functionality, this document will basicly eplain how the scripts work and how they can be used.

### Program.cs
This starts the Listening process in ApplicationClient.cs

### ApplicationClient.cs
This script is responsible of listening to any tcp clients.
The whole process starts via the *StartAcceptingClients* method. 
After a enduser has connected *HandleAsyncConnection* starts the *Listen* process and repeats listening to new a Client again.
*Listen* listens to inbound RPC's, these RPC's are handled by *HandleCommand* wich executes the RPC's via reflection.
All available RPC's functions are explained in the script.

### ConsoleClient.cs
*ConsoleClient* is responsible for the OPC UA connection. This code is based on the OPC UA Console Client code.

### Extensions.cs
This contains some extensions for easy to use functionality.

RPC Remote Procedure Call, for executing methods trough a socket connection
```
//Local
var TcpClient = new TcpClient();
Client.RPC("TestCommand", true);

//Remote
public void TestCommand(bool nx) => Console.Print(nx);
```

For further documentation, see the comments in the scripts
