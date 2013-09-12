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
namespace WhatsThatLight.Ci.Tools.BuildLights.Server
{
    using System;
    using System.ServiceProcess;
    using System.Threading;

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
        /// An instance of the notification manager, the main service.
        /// </summary>
        private readonly NotificationManager notificationManager;

        /// <summary>
        /// The thread in which the manager will start. 
        /// </summary>
        private readonly Thread notificationManagerThread;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Service"/> class.
        /// </summary>
        public Service()
        {
            XmlConfigurator.Configure();
            this.InitializeComponent();
            UserCache userCache = new UserCache(Config.GetPriorityTimerPeriod(), Config.GetPriorityPeriod());
            this.notificationManager = new NotificationManager(Config.GetNotificationManagerPort(), Config.GetLightsManagerPort(), userCache, Config.GetInitializationEnabled());
            this.notificationManagerThread = new Thread(this.StartManager);
        }

        #endregion

        #region Methods

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            this.notificationManagerThread.Start();
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
            this.StopManager();
        }

        /// <summary>
        /// Starts the manager.
        /// </summary>
        private void StartManager()
        {
            try
            {
                Log.Info("Starting manager");
                this.notificationManager.Start();
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        /// <summary>
        /// Stops the manager.
        /// </summary>
        private void StopManager()
        {
            Log.Info("Stopping manager");
            this.notificationManager.Stop();
            this.notificationManagerThread.Join();
        }

        #endregion
    }
}