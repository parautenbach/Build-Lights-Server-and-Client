======================================================================
Important notes
======================================================================
1. When making a release (new MSIs), bump the ProductCode and version
   under both MSIs' properties (VS should automatically bump the 
   ProductCode if you change the version). 
2. It mights be necessary to open the listening ports or allow the 
   services throught the firewall:
     netsh advfirewall firewall add rule name="%name%" dir=in 
       action=allow program="%path%" enable=yes
   We can't cater for all firewall technologies, and unfortunately
   Windows doesn't notify the user of an application being blocked if
   it is a Windows Service. 
======================================================================
Manual testing (monitor the appropriate log file(s)):
======================================================================
Suspend computer must suspend client service and restart when computer 
  resumed after sleeping or hibernating
----------------------------------------------------------------------
o Client service must be running
o Sleep computer -- service must stop
o Wake up computer -- service must restart AND connect to server
  (check retry device detection and retry connect to server)
----------------------------------------------------------------------
Device not connected when client service starts must wait for device 
  before proceeding
----------------------------------------------------------------------
o Client service must not be running
o Device must be disconnected
o Start service -- will block at enumerating for device
o Connect device -- enumeration must succeed
----------------------------------------------------------------------
Pause/Continue client service
----------------------------------------------------------------------
o Client service must be running
o Open Windows' service manager
o Pause the service -- it must stop
o Continue the service -- it must restart
----------------------------------------------------------------------
Change logged on user
----------------------------------------------------------------------
o Client service must be running
o Log in as another user or switch to a different user
o Service must register with new username
----------------------------------------------------------------------
Disconnected network
----------------------------------------------------------------------
o TBC
----------------------------------------------------------------------
Detecting the logged on user
----------------------------------------------------------------------
o TBC
----------------------------------------------------------------------
