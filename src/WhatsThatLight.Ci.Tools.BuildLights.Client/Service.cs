// <copyright file="Service.cs" company="What's That Light?">
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

    using WhatsThatLight.Ci.Tools.BuildLights.Common;

    using log4net;
    using log4net.Config;

    /// <summary>
    /// Generated code. 
    /// </summary>
    public partial class Service : ServiceBase
    {
        #region Constants and Fields

        /// <summary>
        /// Local logger. 
        /// </summary>
        protected static readonly ILog Log = LogManager.GetLogger(typeof(Service));

        /// <summary>
        /// An instance of the lights manager, the main service.
        /// </summary>
        private readonly LightsManager lightsManager;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Service"/> class.
        /// </summary>
        /// <remarks>
        /// Walkthrough: Creating a Windows Service Application in the Component Designer 
        /// http://msdn.microsoft.com/en-us/library/zt39148a(v=vs.90).aspx
        /// </remarks>
        public Service()
        {
            XmlConfigurator.Configure();
            this.InitializeComponent();
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanHandlePowerEvent = true;
            LightsDeviceController lightsDeviceController = new LightsDeviceController(Config.GetUsbProductId(), Config.GetUsbVendorId(), Config.GetUsbUsage(), Config.GetUsbUsagePage(), Config.GetWaitForDeviceRetryPeriod(), Config.GetUsbControlTransferType());
            this.lightsManager = new LightsManager(lightsDeviceController, Config.GetLightsManagerPort(), Config.GetNotificationManagerHost(), Config.GetNotificationManagerPort(), Config.GetRegistrationRetryPeriod(), Config.GetUsbProtocolType());
        }

        #endregion

        #region Methods

        /// <summary>
        /// When implemented in a derived class, <see cref="M:System.ServiceProcess.ServiceBase.OnContinue"/> runs when a Continue command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service resumes normal functioning after being paused.
        /// </summary>
        protected override void OnContinue()
        {
            this.OnStart(null);
            base.OnContinue();
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Pause command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service pauses.
        /// </summary>
        protected override void OnPause()
        {
            this.OnStop();
            base.OnPause();
        }

        /// <summary>
        /// When implemented in a derived class, executes when the computer's power status has changed. This applies to laptop computers when they go into suspended mode, which is not the same as a system shutdown.
        /// </summary>
        /// <param name="powerStatus">A <see cref="T:System.ServiceProcess.PowerBroadcastStatus"/> that indicates a notification from the system about its power status.</param>
        /// <returns>
        /// When implemented in a derived class, the needs of your application determine what value to return. For example, if a QuerySuspend broadcast status is passed, you could cause your application to reject the query by returning false.
        /// </returns>
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            bool status = false;
            try
            {
                switch (powerStatus)
                {
                    case PowerBroadcastStatus.QuerySuspend:
                        status = base.OnPowerEvent(powerStatus);
                        break;
                    case PowerBroadcastStatus.ResumeSuspend:
                        this.OnStart(null);
                        status = base.OnPowerEvent(powerStatus);
                        break;
                    case PowerBroadcastStatus.Suspend:
                        this.OnStop();
                        status = base.OnPowerEvent(powerStatus);
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                status = false;
            }

            Log.Info(string.Format("Responding {0} to {1} request", status, powerStatus));
            return status;
        }

        /// <summary>
        /// Executes when a change event is received from a Terminal Server session.
        /// </summary>
        /// <param name="changeDescription">A <see cref="T:System.ServiceProcess.SessionChangeDescription"/> structure that identifies the change type.</param>
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            this.lightsManager.HandleSessionLogonEvent(changeDescription);
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            try
            {
                if (this.lightsManager.Running)
                {
                    this.lightsManager.Stop();
                }

                this.lightsManager.Start();
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        /// <remarks>
        /// Since .NET 3.5 OnStop gets correctly invoked on system restart or shutdown, there is no need to override OnShutdown.
        /// See  also 
        /// http://social.msdn.microsoft.com/Forums/en/netfxbcl/thread/2598cde9-ff1b-45ff-a77c-b0021c1968a5" and
        /// http://msdn.microsoft.com/en-us/library/system.serviceprocess.servicebase.onshutdown.aspx"/>. 
        /// </remarks>
        protected override void OnStop()
        {
            try
            {
                this.lightsManager.Stop();
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        #endregion
    }
}