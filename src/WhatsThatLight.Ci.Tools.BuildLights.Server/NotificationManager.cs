// <copyright file="NotificationManager.cs" company="What's That Light?">
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
    using System.Threading;
    using System.Threading.Tasks;

    using WhatsThatLight.Ci.Tools.BuildLights.Common;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Entities;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Notifications;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Protocol;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Requests;
    using WhatsThatLight.Ci.Tools.BuildLights.Server.Factories;
    using WhatsThatLight.Ci.Tools.BuildLights.Server.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Server.Wrappers;

    using log4net;

    // TODO: Admin interface, view/write cache (only usernames and hosts)

    /// <summary>
    /// The notification manager controls the system as a whole.
    /// </summary>
    public sealed class NotificationManager : ManagerBase, IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// The log4net instance to use for logging. 
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(typeof(NotificationManager));

        /// <summary>
        /// The port to which to connect to send a notification to a client host that has lights connected. 
        /// </summary>
        private readonly int clientPort;

        /// <summary>
        /// Whether the manager must initialize. 
        /// </summary>
        private readonly bool initialize;

        /// <summary>
        /// A cache of users, their active builds and responsibilities.
        /// </summary>
        private readonly UserCache userCache;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationManager"/> class.
        /// </summary>
        public NotificationManager()
                : this(Config.GetNotificationManagerPort(), Config.GetLightsManagerPort(), new UserCache(), Config.GetInitializationEnabled())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationManager"/> class.
        /// </summary>
        /// <param name="serverPort">The server port on which to receive notifications from the build server or registration requests from clients.</param>
        /// <param name="clientPort">The client port of host to connect to for broadcasting notifications.</param>
        /// <param name="userCache">The user cache.</param>
        /// <param name="initialize">if set to <c>true</c> initialize the manager.</param>
        public NotificationManager(int serverPort, int clientPort, UserCache userCache, bool initialize)
                : base(serverPort)
        {
            this.clientPort = clientPort;
            this.userCache = userCache;
            this.initialize = initialize;
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
            if (sender.GetType() == typeof(BuildNotification) || sender.GetType() == typeof(ResponsibilityNotification))
            {
                IBuildServerNotification notification = (IBuildServerNotification)sender;
                this.userCache.Update(notification);
            }
            else if (sender.GetType() == typeof(RegistrationRequest))
            {
                RegistrationRequest request = (RegistrationRequest)sender;
                this.userCache.Register(request);
            }
            else
            {
                log.Warn(string.Format("Command of type {0} ignored", sender.GetType()));
            }
        }

        /// <summary>
        /// Handles a cache update.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public void HandleUpdate(object sender, EventArgs eventArgs)
        {
            User user = (User)sender;
            if (user.IsBuildActive())
            {
                IRequest buildActiveRequest = new BuildActiveRequest(user.IsBuildActive());
                this.NotifyHost(user, Parser.Encode(buildActiveRequest));
            }
            else
            {
                IRequest attentionRequest = new AttentionRequest(user.IsAttentionRequired(), user.IsAttentionPriority);
                this.NotifyHost(user, Parser.Encode(attentionRequest));
            }
        }

        #endregion

        #region Implemented Interfaces

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.userCache != null)
            {
                this.userCache.Dispose();
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
            // Nothing to do
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
            if (this.initialize)
            {
                this.Initialize();
            }

            log.Info("Start monitoring user cache events");
            this.userCache.OnUpdate += this.HandleUpdate;
        }

        /// <summary>
        /// Stops any other peripherals and processes. This gets executed before the listener is stopped. Invoked by <see cref="ManagerBase.Stop"/>.
        /// </summary>
        protected override void PreStop()
        {
            log.Info("Stop monitoring user cache events");
            this.userCache.OnUpdate -= this.HandleUpdate;
            log.Info("Broadcasting server down message to all registered users");
            IRequest request = new StatusRequest(false);
            string command = Parser.Encode(request);
            Parallel.ForEach(this.userCache.GetRegisteredUsers(), user => this.NotifyHost(user, command));
        }

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        private void Initialize()
        {
            log.Info("Initializing user cache from build server (please be patient)");
            SvnCredentials vcsCredentials = new SvnCredentials(Config.GetVcsServerUrl(), Config.GetBuildServerUsername(), Config.GetVcsServerPassword());
            IVcsClient svnClient = VcsServerClientFactory.Create<SvnWrapperClient>(vcsCredentials);
            TeamCityCredentials buildServerCredentials = new TeamCityCredentials(Config.GetBuildServerUrl(), Config.GetBuildServerUsername(), Config.GetBuildServerPassword());
            IBuildServerClient teamCityClient = BuildServerClientFactory.Create<TeamCityWrapperClient>(buildServerCredentials, svnClient);

            // This is supposed to crash this server if the remote servers/services can't be reached
            this.userCache.Initialize(teamCityClient);
        }

        /// <summary>
        /// Notifies the host which lights to turn on or off.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="command">The command.</param>
        private void NotifyHost(User user, string command)
        {
            if (string.IsNullOrEmpty(user.Hostname))
            {
                log.Debug(string.Format("Skipping notification for user {0}: Host not registered ({1})", user.Username, command));
                return;
            }

            // TODO: Improve by dropping onto a queue to give it a more controlled async behaviour
            log.Info(string.Format("Sending command {0} to host {1}:{2} (user {3})", command, user.Hostname, this.clientPort, user.Username));
            Thread thread = new Thread(() =>
            {
                bool success = Utils.SendCommand(user.Hostname, this.clientPort, command);
                if (!success)
                {
                    log.Warn("Could not send command to host");
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }

        #endregion
    }
}