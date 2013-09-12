// <copyright file="LightsManager.cs" company="What's That Light?">
// Copyright 2013 Pieter Rautenbach
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
namespace WhatsThatLight.Ci.Tools.BuildLights.Client
{
    using System;
    using System.ServiceProcess;
    using System.Threading;

    using WhatsThatLight.Ci.Tools.BuildLights.Client.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Client.Exceptions;
    using WhatsThatLight.Ci.Tools.BuildLights.Client.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Protocol;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Requests;

    using log4net;

    /// <summary>
    /// The lights manager controls the system as a whole. 
    /// </summary>
    public sealed class LightsManager : ManagerBase, IDisposable
    {
        //// CONSIDER: Ping in case server crashed/didn't notify on down

        #region Constants and Fields

        /// <summary>
        /// Local logger. 
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(typeof(LightsManager));

        /// <summary>
        /// The manager's device controller.
        /// </summary>
        private readonly ILightsDeviceController lightsDeviceController;

        /// <summary>
        /// The notification manager host.
        /// </summary>
        private readonly string notificationManagerHost;

        /// <summary>
        /// The notification manager port.
        /// </summary>
        private readonly int notificationManagerPort;

        /// <summary>
        /// The time (in ms) to wait between retries if the connection to
        /// the NotificationManager couldn't be made during registration. 
        /// </summary>
        private readonly int registrationRetryPeriod;

        /// <summary>
        /// The USB protocol to communicate with the device.
        /// </summary>
        private readonly UsbProtocolType usbProtocolType;

        /// <summary>
        /// The timer for managing registration with the NotificationManager. 
        /// </summary>
        private Timer registrationRetryTimer;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LightsManager"/> class.
        /// </summary>
        /// <param name="lightsDeviceController">The lights device controller.</param>
        public LightsManager(ILightsDeviceController lightsDeviceController)
                : this(lightsDeviceController, Config.GetLightsManagerPort(), Config.GetNotificationManagerHost(), Config.GetNotificationManagerPort(), Config.GetRegistrationRetryPeriod(), Config.GetUsbProtocolType())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LightsManager"/> class.
        /// </summary>
        /// <param name="lightsDeviceController">The lights device controller.</param>
        /// <param name="usbProtocolType">Type of the usb protocol.</param>
        public LightsManager(ILightsDeviceController lightsDeviceController, UsbProtocolType usbProtocolType)
                : this(lightsDeviceController, Config.GetLightsManagerPort(), Config.GetNotificationManagerHost(), Config.GetNotificationManagerPort(), Config.GetRegistrationRetryPeriod(), usbProtocolType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LightsManager"/> class.
        /// </summary>
        /// <param name="lightsDeviceController">The lights device controller.</param>
        /// <param name="lightsManagerPort">The lights manager port.</param>
        /// <param name="notificationManagerHost">The notification manager host.</param>
        /// <param name="notificationManagerPort">The notification manager port.</param>
        /// <param name="registrationRetryPeriod">The registration retry period.</param>
        /// <param name="usbProtocolType">Type of the USB protocol to use.</param>
        public LightsManager(ILightsDeviceController lightsDeviceController, int lightsManagerPort, string notificationManagerHost, int notificationManagerPort, int registrationRetryPeriod, UsbProtocolType usbProtocolType)
                : base(lightsManagerPort)
        {
            this.notificationManagerHost = notificationManagerHost;
            this.notificationManagerPort = notificationManagerPort;
            this.registrationRetryPeriod = registrationRetryPeriod;
            this.lightsDeviceController = lightsDeviceController;
            this.usbProtocolType = usbProtocolType;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Handles a command received on the listener.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public override void HandleCommand(object sender, EventArgs eventArgs)
        {
            try
            {
                IRequest request = (IRequest)sender;

                // Ignore server up requests, because the registration thread will pick things up
                if (request.GetType() == typeof(StatusRequest) && !((StatusRequest)request).Status)
                {
                    this.StartRegistrationRetryTimer();
                }

                LightsDeviceResult result = this.NotifyLightsDevice(request);
                switch (result)
                {
                    case LightsDeviceResult.Ack:
                        break;
                    case LightsDeviceResult.NotConnected:
                        log.Warn("No USB device connected");
                        break;
                    case LightsDeviceResult.NotOpen:
                        log.Error("USB device could not be opened");
                        break;
                    case LightsDeviceResult.NoResponse:
                        log.Error("USB device did not respond");
                        break;
                    case LightsDeviceResult.Nak:
                        log.Error("USB device could not understand the command");
                        break;
                    default:
                        log.Error(string.Format("Unknown USB result: {0}", result));
                        break;
                }
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }

        /// <summary>
        /// Handles the session logon event by recording the logged on user.
        /// </summary>
        /// <param name="sessionChangeDescription">The session change description.</param>
        public void HandleSessionLogonEvent(SessionChangeDescription sessionChangeDescription)
        {
            if (this.Running && sessionChangeDescription.Reason == SessionChangeReason.SessionLogon)
            {
                log.Info("Session logon event raised");
                this.StartRegistrationRetryTimer();
            }
        }

        /// <summary>
        /// Notifies the USB device to switch the LEDs correctly, given the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if the device was notified; otherwise, <c>false</c>.</returns>
        public LightsDeviceResult NotifyLightsDevice(IRequest request)
        {
            if (this.usbProtocolType == UsbProtocolType.DasBlinkenlichten)
            {
                return this.lightsDeviceController.SendCommand(Parser.TranslateForDasBlinkenlichten(request));
            }

            if (this.usbProtocolType == UsbProtocolType.Blink1)
            {
                short featureReportByteLength = this.lightsDeviceController.GetFeatureReportByteLength();
                byte[] bytes = null;
                if (request.GetType() == typeof(BuildActiveRequest))
                {
                    BuildActiveRequest buildActiveRequest = (BuildActiveRequest)request;
                    if (buildActiveRequest.IsBuildsActive)
                    {
                        bytes = Parser.TranslateForBlink1(buildActiveRequest, featureReportByteLength);
                    }
                }
                else if (request.GetType() == typeof(AttentionRequest))
                {
                    bytes = Parser.TranslateForBlink1((AttentionRequest)request, featureReportByteLength);
                }
                else if (request.GetType() == typeof(StatusRequest))
                {
                    StatusRequest statusRequest = (StatusRequest)request;
                    if (!statusRequest.Status)
                    {
                        bytes = Parser.TranslateForBlink1(statusRequest, featureReportByteLength);
                    }
                }

                // Hmm... This is a lie. We didn't send anything, but we didn't have to 
                // (or at least, should not have). 
                return bytes != null ? this.lightsDeviceController.SendCommand(bytes) : LightsDeviceResult.Ack;
            }

            throw new UnsupportedUsbProtocolTypeException(string.Format("Type {0} not supported", this.usbProtocolType));
        }

        #endregion

        #region Implemented Interfaces

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.registrationRetryTimer != null)
            {
                this.registrationRetryTimer.Dispose();
            }
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Starts any other peripherals and processes. This gets executed after the listener is started. Invoked by <see cref="ManagerBase.Start"/>.
        /// </summary>
        protected override void PostStart()
        {
            log.Info("Start monitoring device events");
            this.lightsDeviceController.OnDeviceInserted += this.OnDeviceInserted;
            this.lightsDeviceController.OnDeviceRemoved += this.OnDeviceRemoved;
            log.Info("Starting lights device controller");
            this.lightsDeviceController.Start();
        }

        /// <summary>
        /// Stops any other peripherals and processes. This gets executed after the listener is stopped.
        /// </summary>
        protected override void PostStop()
        {
            // Nothing to do
        }

        /// <summary>
        /// Starts any other peripherals and processes. This gets executed before the listener is started. Stop happens in reverse.
        /// </summary>
        protected override void PreStart()
        {
            this.registrationRetryTimer = new Timer(this.RegistrationRetryTimerExpired);
            this.StopRegistrationRetryTimer();
        }

        /// <summary>
        /// Stops any other peripherals and processes. This gets executed before the listener is stopped. Invoked by <see cref="ManagerBase.Stop"/>.
        /// </summary>
        protected override void PreStop()
        {
            log.Info("Stop monitoring device events");
            this.lightsDeviceController.OnDeviceInserted -= this.OnDeviceInserted;
            this.lightsDeviceController.OnDeviceRemoved -= this.OnDeviceRemoved;
            log.Info("Stopping lights device controller");
            if (this.usbProtocolType == UsbProtocolType.DasBlinkenlichten)
            {
                this.lightsDeviceController.SendCommand(Parser.TranslateForDasBlinkenlichten(new StatusRequest(false)));
            }
            else
            {
                this.lightsDeviceController.SendCommand(Parser.TranslateForBlink1(new StatusRequest(false), this.lightsDeviceController.GetFeatureReportByteLength()));
            }

            this.lightsDeviceController.Stop();
            log.Info("Stopping the registration retry timer");
            this.StopRegistrationRetryTimer();
            this.registrationRetryTimer.Dispose();
        }

        /// <summary>
        /// Called when the USB lights device got inserted.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnDeviceInserted(object sender, EventArgs e)
        {
            // SIDE-EFFECT WARNING: The HIDLibrary will raise this event after switching
            // on the monitoring of events if the device is connected, so we don't need
            // to have a registration step upon construction or initialisation. 
            log.Info("Lights device inserted");
            this.StartRegistrationRetryTimer();

            // Consider: This could be the alternative to set the lights device to some initial state
            ////this.HandleCommand(new StatusRequest(false), EventArgs.Empty);
        }

        /// <summary>
        /// Called when the USB lights device got removed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnDeviceRemoved(object sender, EventArgs e)
        {
            log.Info("Lights device removed");
        }

        /// <summary>
        /// Registers the device.
        /// </summary>
        private void Register()
        {
            // Ensure the listener is running, otherwise we won't be able to receive requests after registration
            if (!this.Running)
            {
                log.Debug("Server not running - ignoring request to register");
                return;
            }

            try
            {
                string user = Utils.GetUsername();
                if (string.IsNullOrEmpty(user))
                {
                    log.Warn(string.Format("Cannot register: No logged on user - will retry in about {0}s", this.registrationRetryPeriod / 1000));
                    return;
                }

                log.Info(string.Format("Registering device for user {0}", user));
                string host = Utils.GetHostname();
                if (string.IsNullOrEmpty(host))
                {
                    log.Warn(string.Format("Cannot register: Unable to resolve host - will retry in about {0}s", this.registrationRetryPeriod / 1000));
                    return;
                }

                RegistrationRequest request = new RegistrationRequest(host, user);
                string command = Parser.Encode(request);
                log.Info(string.Format("Sending command {0} to host {1}:{2}", command, this.notificationManagerHost, this.notificationManagerPort));
                if (Utils.SendCommand(this.notificationManagerHost, this.notificationManagerPort, command))
                {
                    this.StopRegistrationRetryTimer();
                }
                else
                {
                    log.Warn(string.Format("Could not send command to host - will retry in about {0}s", this.registrationRetryPeriod / 1000));
                }
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }

        /// <summary>
        /// Callback method for then the registration retry timer expired.
        /// </summary>
        /// <param name="source">The source.</param>
        private void RegistrationRetryTimerExpired(object source)
        {
            this.Register();
        }

        /// <summary>
        /// Starts the registration retry timer.
        /// </summary>
        private void StartRegistrationRetryTimer()
        {
            this.registrationRetryTimer.Change(0, this.registrationRetryPeriod);
        }

        /// <summary>
        /// Stops the registration retry timer.
        /// </summary>
        private void StopRegistrationRetryTimer()
        {
            this.registrationRetryTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #endregion
    }
}