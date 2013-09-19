Build Lights Server and Client
==============================

A server for receiving build notifications from a build server and notifying registered agents. This project includes the Windows client too.

# Installation
* The client runs as a Windows service, `Continuous Integration Lights Client Service`.
* The server runs as a Windows service, `Continuous Integration Lights Server Service`.
* After the first installation (i.e. not after subsequent upgrades), you need to start the services manually. This is due to a limitation with the Setup and Deployment project type.
* Ensure that your firewall allows for inbound connections on port 9191 and outbound connections on port 9192.
* The default installation path is `C:\Program Files (x86)\WhatsThatLight\`.
* You can find the config file in the installation path. Update it to write log files to a path of your choice.