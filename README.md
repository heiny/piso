## ProcessHost:
This is the windows service host for the `JobObjectService` implementation.

* note: Its output is copied to the `HostDriver` output directory via a post build cmd.

---

## HostDriver:

HostDriver is the primary app that installs a windows service => ProcessHost.exe (which hosts a JobObjectService instance) running as the specified test_user.
It then connects a JobObjectClient to the service as a bidirectional IPC channel using named pipes.  It then fires off a StartProcess command
which in turn tells the service to start the specified command (ping, powershell, what have you) which starts a process as the service user
and adds it to the job object.



#### Running The HostDriver Test:

1. Edit the `HostDriver.Program.Run` method and change the following values:
  * containerDir - The directory where process host service will be installed and run from
  * processCred - The username/pwd for your test user account
2. Build and run the HostDriver application, you can then verify the job object and process boundaries by using process explorer.
3. You can modify the command which is run by the service by editing the `client.StartProcess()` call in the `HostDriver.Program.Run` method. (see examples below)

---

## HostController:
The HostController will connect to the specified (via binding) `ProcessHost` (JobObjectService instance) and can listen to the std IO.
You can also pass commands to the hosted process using the HostController CLI.  Useful to kill long running hosted processes.

#### Example Commands:
````
    run cmd.exe /c ping 127.0.0.1 -n 250 -w 1000  
    run powershell.exe -InputFormat None -NoLogo -NoProfile -NonInteractive -Command "echo 'START'; Start-Sleep -s 15; echo 'STOP'"  
    run powershell.exe -NoExit -Command "dir env:"  
````

---

#### Solution TODO:

1. Make client/service bindings configurable via code vs config file as these will need to be dynamic for multiple instances.
2. Implement security on the named pipe so only the test user can read/write from it
  * This will break HostController so need to add auth - either impersonation or require run-as on the cmd window.
3. Implement HostController features: (listen to process out, set job limits, send process stdin commands, etc)

#### Misc Notes:

- if you use ServiceController class prior to SC.exe command, it does something to make sc.exe think the service already exists but it doesn't.process-exec
