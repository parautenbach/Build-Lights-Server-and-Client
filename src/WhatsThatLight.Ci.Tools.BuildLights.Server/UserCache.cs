// <copyright file="UserCache.cs" company="What's That Light?">
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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using WhatsThatLight.Ci.Tools.BuildLights.Common;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Entities;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Notifications;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Requests;
    using WhatsThatLight.Ci.Tools.BuildLights.Server.Interfaces;

    using log4net;

    /// <summary>
    /// Singleton user cache. Strictly speaking it is not a requirement, so this is a way to experiment. 
    /// </summary>
    public sealed class UserCache : IDisposable
    {
        //// TODO: Concurrent running builds for the same project and build config and include a build ID for initial cache update

        #region Constants and Fields

        /// <summary>
        /// Local logger. 
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(typeof(UserCache));

        /// <summary>
        /// The period, that if exceeded, will cause a user's attention required will become a priority. 
        /// </summary>
        private readonly TimeSpan priorityPeriod;

        /// <summary>
        /// The timer to check for users whose attention have been required for more than the defined period. 
        /// </summary>
        private readonly Timer priorityTimer;

        /// <summary>
        /// The internal user cache container. 
        /// </summary>
        private readonly ConcurrentDictionary<string, User> userCache;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UserCache"/> class without a priority timer.
        /// </summary>
        public UserCache()
                : this(Timeout.Infinite, TimeSpan.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserCache"/> class.
        /// </summary>
        /// <param name="priorityTimerPeriod">How often, in ms, the timer must expire to check for high attention priorities.</param>
        /// <param name="priorityPeriod">The period, that if exceeded will cause high attention priority to be set.</param>
        public UserCache(int priorityTimerPeriod, TimeSpan priorityPeriod)
        {
            this.userCache = new ConcurrentDictionary<string, User>();
            this.priorityTimer = new Timer(this.PriorityCallback);
            this.priorityPeriod = priorityPeriod;
            this.priorityTimer.Change(priorityTimerPeriod, priorityTimerPeriod);
        }

        #endregion

        #region Delegates

        /// <summary>
        /// A delegate used when the cache got updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public delegate void UpdateEventHandler(object sender, EventArgs e);

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the cache got updated.
        /// </summary>
        public event UpdateEventHandler OnUpdate;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets all the users currently registered.
        /// </summary>
        /// <returns>A collection of registered users.</returns>
        public IEnumerable<User> GetRegisteredUsers()
        {
            return this.userCache.Values.Where(user => !string.IsNullOrEmpty(user.Hostname));
        }

        /// <summary>
        /// Gets a user from cache.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The user</returns>
        public User GetUser(string username)
        {
            return this.userCache[username];
        }

        /// <summary>
        /// Initialize the cache from the specified build server and VCS clients.
        /// </summary>
        /// <param name="buildServerClient">The build server client.</param>
        public void Initialize(IBuildServerClient buildServerClient)
        {
            // CONSIDERED: Retry if host down
            // This will add too much complexity right now and strange race
            // conditions between the start of the build server and the 
            // notification manager. For now, if you restart the build server
            // you must also restart the notification manager. Also, we don't
            // want to switch to a polling model, or implement any kind of
            // queueing. It's simply not worth the effort, given the current
            // known (estimated) likelihood of all these strange conditions
            // occurring. 
            foreach (IBuildServerNotification buildNotification in buildServerClient.GetAllBuildsThatRequireAttention())
            {
                this.Update(buildNotification);
            }

            foreach (IBuildServerNotification buildNotification in buildServerClient.GetAllActiveBuildNotifications())
            {
                this.Update(buildNotification);
            }
        }

        /// <summary>
        /// Registers the user.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Register(RegistrationRequest request)
        {
            User user = new User(request.Username) {
                                                           Hostname = request.Hostname
                                                   };
            this.userCache.AddOrUpdate(user.Username, user, (username, existingUser) =>
            {
                existingUser.Hostname = request.Hostname;
                return existingUser;
            });
            this.userCache.TryGetValue(user.Username, out user);
            this.NotifyUpdate(user);
        }

        /// <summary>
        /// Updates the cache with the specified notification.
        /// </summary>
        /// <param name="notification">The notification.</param>
        public void Update(IBuildServerNotification notification)
        {
            string buildKey = Utils.CreateBuildKey(notification);
            if (notification.GetType() == typeof(BuildNotification))
            {
                BuildNotification buildNotification = (BuildNotification)notification;
                Parallel.ForEach(buildNotification.Recipients, recipient =>
                {
                    User user = this.TryGetUser(recipient);
                    UpdateUserActiveBuilds(buildKey, buildNotification, ref user);
                    UpdateUserBuildsResponsibleFor(buildKey, buildNotification, ref user);
                    this.userCache.AddOrUpdate(user.Username, user, (key, value) => user);
                    this.NotifyUpdate(user);
                });
            }
            else if (notification.GetType() == typeof(ResponsibilityNotification))
            {
                ResponsibilityNotification responsibilityNotification = (ResponsibilityNotification)notification;
                User user = this.TryGetUser(responsibilityNotification.Recipient);
                this.UpdateUserBuildsResponsibleFor(buildKey, responsibilityNotification, ref user);
                this.userCache.AddOrUpdate(user.Username, user, (key, value) => user);
                this.NotifyUpdate(user);
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
            if (this.priorityTimer != null)
            {
                this.priorityTimer.Dispose();
            }
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Cancels the priority.
        /// </summary>
        /// <param name="user">The user.</param>
        private static void CancelPriority(User user)
        {
            user.AttentionFirstRequired = null;
            user.IsAttentionPriority = false;
        }

        /// <summary>
        /// Updates the user's active builds.
        /// </summary>
        /// <param name="buildKey">The build key.</param>
        /// <param name="buildNotification">The build notification.</param>
        /// <param name="user">The user.</param>
        private static void UpdateUserActiveBuilds(string buildKey, BuildNotification buildNotification, ref User user)
        {
            // Active and inactive is mutually exclusive, i.e. if a build is not building (active) it must consequently 
            // be inactive. 
            if (Utils.IsActiveBuild(buildNotification.Type))
            {
                if (!user.ActiveBuilds.Contains(buildKey))
                {
                    user.ActiveBuilds.Add(buildKey);
                }
            }
            else
            {
                user.ActiveBuilds.Remove(buildKey);
            }
        }

        /// <summary>
        /// Updates the builds the user is responsible for.
        /// </summary>
        /// <param name="buildKey">The build key.</param>
        /// <param name="buildNotification">The build notification.</param>
        /// <param name="user">The user.</param>
        private static void UpdateUserBuildsResponsibleFor(string buildKey, BuildNotification buildNotification, ref User user)
        {
            // Active vs. inactive build is mutually exclusive, but unfortunately not attention when it is
            // a build notification. Since IsAttentionRequired tests for build failures, it's negation doesn't
            // imply success. E.g. if the build is not failed, failed to start, failing or hanging, it doesn't
            // imply success. This is because we're mixing these with responsibility notifications, which
            // make it ambiguous. So, we need to actively test that attention can be cancelled. 
            if (Utils.IsAttentionRequired(buildNotification.Type))
            {
                // If this is the first notification of responsibility
                if (user.BuildsResponsibleFor.Count == 0)
                {
                    user.AttentionFirstRequired = DateTime.Now;
                }

                if (!user.BuildsResponsibleFor.Contains(buildKey))
                {
                    user.BuildsResponsibleFor.Add(buildKey);
                }
            }
            else if (Utils.IsNoAttentionRequired(buildNotification.Type))
            {
                user.BuildsResponsibleFor.Remove(buildKey);
                if (user.BuildsResponsibleFor.Count == 0)
                {
                    CancelPriority(user);
                }
            }
        }

        /// <summary>
        /// Notifies that there was an update.
        /// </summary>
        /// <param name="user">The user that was updated.</param>
        private void NotifyUpdate(User user)
        {
            if (this.OnUpdate != null)
            {
                this.OnUpdate(user, EventArgs.Empty);
            }
        }

        /// <summary>
        /// When the timer expires, sets priority for users whose priority period has expired.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void PriorityCallback(object sender)
        {
            log.Debug("Checking for high priorities");
            Parallel.ForEach(this.userCache.Values.Where(x => !x.IsAttentionPriority).Where(user => DateTime.Now - user.AttentionFirstRequired >= this.priorityPeriod), user =>
            {
                log.Info(string.Format("Setting priority for user {0} to high", user.Username));
                user.IsAttentionPriority = true;
                this.NotifyUpdate(user);
            });
        }

        /// <summary>
        /// Tries to get the user. If the user doesn't exist, it will be created. 
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The user if found or a new user</returns>
        private User TryGetUser(string username)
        {
            User user;
            if (!this.userCache.TryGetValue(username.ToLower(), out user) || user == null)
            {
                user = new User(username.ToLower());
            }

            return user;
        }

        /// <summary>
        /// Updates the builds the user is responsible for.
        /// </summary>
        /// <param name="buildKey">The build key.</param>
        /// <param name="responsibilityNotification">The responsibility notification.</param>
        /// <param name="user">The user.</param>
        private void UpdateUserBuildsResponsibleFor(string buildKey, ResponsibilityNotification responsibilityNotification, ref User user)
        {
            if (Utils.IsAttentionRequired(responsibilityNotification.Type, responsibilityNotification.State))
            {
                if (!user.BuildsResponsibleFor.Contains(buildKey))
                {
                    // Only one user can be responsible for a given build key
                    foreach (User otherUser in this.userCache.Values.Where(x => x.BuildsResponsibleFor.Contains(buildKey)))
                    {
                        otherUser.BuildsResponsibleFor.Remove(buildKey);
                        if (otherUser.BuildsResponsibleFor.Count == 0)
                        {
                            CancelPriority(otherUser);
                        }
                    }

                    if (user.BuildsResponsibleFor.Count == 0)
                    {
                        user.AttentionFirstRequired = DateTime.Now;
                    }

                    // We could be removing the build key we just added, so this needs to happen afterwards
                    user.BuildsResponsibleFor.Add(buildKey);
                }
            }
            else
            {
                user.BuildsResponsibleFor.Remove(buildKey);
                if (user.BuildsResponsibleFor.Count == 0)
                {
                    CancelPriority(user);
                }
            }
        }

        #endregion
    }
}