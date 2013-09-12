// <copyright file="ManagerBase.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common
{
    using System;
    using System.Net;
    using System.Reflection;
    using System.Threading;

    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Transport;

    using log4net;

    /// <summary>
    /// Base manager class with a few generic implementations. 
    /// </summary>
    public abstract class ManagerBase : IManager
    {
        #region Constants and Fields

        /// <summary>
        /// The log4net instance to use for logging. 
        /// </summary>
        protected static readonly ILog Log = LogManager.GetLogger(typeof(ManagerBase));

        /// <summary>
        /// Holds an instance of the listening socket server.
        /// </summary>
        private readonly Listener listener;

        /// <summary>
        /// The spin wait timeout when starting up the listener. 
        /// </summary>
        private readonly int listenerSpinWaitTimeout;

        /// <summary>
        /// A lock to ensure that the manager starts and stops correctly. 
        /// </summary>
        private readonly object runLock = new object();

        /// <summary>
        /// The port on which to listen as the server host. 
        /// </summary>
        private readonly int serverPort;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagerBase"/> class.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        protected ManagerBase(int port)
        {
            this.serverPort = port;
            this.listenerSpinWaitTimeout = 1000;
            this.listener = new Listener(IPAddress.Any, this.serverPort);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this <see cref="ManagerBase"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        public bool Running { get; private set; }

        #endregion

        #region Implemented Interfaces

        #region IManager

        /// <summary>
        /// Handles a command received on the listener.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public abstract void HandleCommand(object sender, EventArgs eventArgs);

        /// <summary>
        /// Starts this instance. 
        /// </summary>
        public void Start()
        {
            lock (this.runLock)
            {
                Log.Info(string.Format("Manager is starting (version {0})", Assembly.GetExecutingAssembly().GetName().Version));
                if (this.Running)
                {
                    Log.Error("Attempted to start an already running manager");
                    return;
                }

                this.PreStart();
                this.listener.OnCommandReceived += this.HandleCommand;
                this.listener.Start();

                // The manager isn't running until the listener isn't running
                // We use a spin wait to prevent the thread from being context switched
                while (!this.listener.Running)
                {
                    Thread.SpinWait(this.listenerSpinWaitTimeout);
                }

                this.PostStart();
                this.Running = true;
                Log.Info("Manager started");
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            lock (this.runLock)
            {
                if (!this.Running)
                {
                    Log.Error("Attempted to stop an already stopped manager");
                    return;
                }

                Log.Info("Manager is stopping");
                this.PreStop();
                this.listener.OnCommandReceived -= this.HandleCommand;
                if (this.listener.Running)
                {
                    this.listener.Stop();
                    while (this.listener.Running)
                    {
                        Thread.SpinWait(this.listenerSpinWaitTimeout);
                    }
                }

                this.PostStop();
                Log.Info("Manager stopped");
                this.Running = false;
            }
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Starts any other peripherals and processes. This gets executed after the listener is started. Invoked by <see cref="ManagerBase.Start"/>.
        /// </summary>
        protected abstract void PostStart();

        /// <summary>
        /// Stops any other peripherals and processes. This gets executed after the listener is stopped. Invoked by <see cref="ManagerBase.Stop"/>.
        /// </summary>
        protected abstract void PostStop();

        /// <summary>
        /// Starts any other peripherals and processes. This gets executed before the listener is started. Invoked by <see cref="ManagerBase.Start"/>.
        /// </summary>
        protected abstract void PreStart();

        /// <summary>
        /// Stops any other peripherals and processes. This gets executed before the listener is stopped. Invoked by <see cref="ManagerBase.Stop"/>.
        /// </summary>
        protected abstract void PreStop();

        #endregion
    }
}