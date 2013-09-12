// <copyright file="NotificationManagerUnitTests.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Server.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;

    using WhatsThatLight.Ci.Tools.BuildLights.Common;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Constants;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Entities;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Notifications;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Protocol;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Requests;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Transport;

    using NUnit.Framework;

    /// <summary>
    /// Test the server manager.
    /// </summary>
    [TestFixture]
    public class NotificationManagerUnitTests
    {
        #region Public Methods

        /// <summary>
        /// Tests the handling of a build notification.
        /// </summary>
        [Test]
        public void TestHandleBuildNotification()
        {
            UserCache userCache = null;
            NotificationManager notificationManager = null;
            try
            {
                userCache = new UserCache();
                notificationManager = new NotificationManager(0, 0, userCache, false);
                string username = "user1";
                string[] recipients = { username };
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                IBuildServerNotification buildServerNotification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
                string buildKey = Utils.CreateBuildKey(buildServerNotification);
                notificationManager.HandleCommand(buildServerNotification, EventArgs.Empty);
                User user = userCache.GetUser(username);
                Assert.That(user.Username, Is.EqualTo(username));
                Assert.That(user.ActiveBuilds.Contains(buildKey));
            }
            finally
            {
                if (notificationManager != null)
                {
                    notificationManager.Dispose();
                }

                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the handling of a cache update.
        /// </summary>
        [Test]
        public void TestHandleCommandForPriorityAttentionRequest()
        {
            UserCache userCache = null;
            NotificationManager notificationManager = null;

            try
            {
                // Setup the notification manager
                object syncRoot = new object();
                int serverPort = 20000;
                int clientPort = 20001;

                // If the timer's expiry (trigger) period is too fast, the test will fail in step 2. 
                // We set this (hopefully) sufficiently slow so that we can properly test the 
                // sequence of events. 
                int priorityTimerPeriod = 10000;

                // Any user's attention required will immediately be set as a priority. 
                TimeSpan priorityPeriod = TimeSpan.Zero;

                // Note: The cache's timer starts upon construction
                userCache = new UserCache(priorityTimerPeriod, priorityPeriod);
                notificationManager = new NotificationManager(serverPort, clientPort, userCache, false);

                // A dummy client which the notification manager will notify
                Listener listener = new Listener(IPAddress.Any, clientPort);

                // We only want the attention requests
                AttentionRequest attentionRequest = null;
                listener.OnCommandReceived += (sender, args) =>
                {
                    if (sender.GetType() == typeof(AttentionRequest))
                    {
                        attentionRequest = (AttentionRequest)sender;
                    }

                    lock (syncRoot)
                    {
                        Monitor.Pulse(syncRoot);
                    }
                };

                // Create the requests and notifications
                string username = "user1";
                string hostname = "localhost";
                string projectId = "project1";
                string buildConfigId = "buildConfig1";
                string state = BuildServerResponsibilityState.Taken;
                RegistrationRequest registrationRequest = new RegistrationRequest(hostname, username);
                ResponsibilityNotification responsibilityNotification = new ResponsibilityNotification(BuildServerNotificationType.BuildResponsibilityAssigned, projectId, buildConfigId, username, state);

                // Start the server and the client
                notificationManager.Start();
                Assert.That(notificationManager.Running, Is.True);
                listener.Start();
                Assert.That(listener.Running, Is.True);

                // STEP 1
                // Register the dummy client with the server
                lock (syncRoot)
                {
                    // This will cause one attention request and one build active request
                    Utils.SendCommand(hostname, serverPort, Parser.Encode(registrationRequest));
                    Monitor.Wait(syncRoot);
                }

                Assert.That(attentionRequest, Is.Not.Null);
                Assert.That(attentionRequest.IsAttentionRequired, Is.False);
                Assert.That(attentionRequest.IsPriority, Is.False);

                // STEP 2
                attentionRequest = null;
                lock (syncRoot)
                {
                    // This will cause another attention request and another build active request 
                    notificationManager.HandleCommand(responsibilityNotification, EventArgs.Empty);
                    Monitor.Wait(syncRoot);
                }

                Assert.That(attentionRequest, Is.Not.Null);
                Assert.That(attentionRequest != null && attentionRequest.IsAttentionRequired, Is.True);
                Assert.That(attentionRequest != null && attentionRequest.IsPriority, Is.False);

                // STEP 3
                // The next two pulses (again for an attention and a build active request) must
                // be because of the timer that expired
                attentionRequest = null;
                lock (syncRoot)
                {
                    Monitor.Wait(syncRoot);
                }

                // Shut down
                notificationManager.Stop();
                Assert.That(notificationManager.Running, Is.False);
                listener.Stop();
                Assert.That(listener.Running, Is.False);

                // Test
                Assert.That(attentionRequest, Is.Not.Null);
                Assert.That(attentionRequest != null && attentionRequest.IsAttentionRequired, Is.True);
                Assert.That(attentionRequest != null && attentionRequest.IsPriority, Is.True);
            }
            finally
            {
                if (notificationManager != null)
                {
                    notificationManager.Dispose();
                }

                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the handling of a registration request.
        /// </summary>
        [Test]
        public void TestHandleRegistrationRequest()
        {
            UserCache userCache = null;
            NotificationManager notificationManager = null;
            try
            {
                userCache = new UserCache();
                notificationManager = new NotificationManager(0, 0, userCache, false);
                string username = "user1";
                string hostname = username + "-ws";
                IRequest registrationRequest = new RegistrationRequest(hostname, username);

                // Check that the user doesn't exist prior to the registration request
                try
                {
                    userCache.GetUser(username);
                    Assert.Fail();
                }
                catch (KeyNotFoundException)
                {
                }

                notificationManager.HandleCommand(registrationRequest, EventArgs.Empty);
                User user = userCache.GetUser(username);
                Assert.That(user.Username, Is.EqualTo(username));
                Assert.That(user.Hostname, Is.EqualTo(hostname));

                // Registering the same user a second time shouldn't fail
                notificationManager.HandleCommand(registrationRequest, EventArgs.Empty);
            }
            finally
            {
                if (notificationManager != null)
                {
                    notificationManager.Dispose();
                }

                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the handling of a registration request after receiving build notification.
        /// </summary>
        [Test]
        public void TestHandleRegistrationRequestAfterBuildNotification()
        {
            UserCache userCache = null;
            NotificationManager notificationManager = null;
            try
            {
                // Setup
                userCache = new UserCache();
                notificationManager = new NotificationManager(0, 0, userCache, false);
                string username = "user1";
                string[] recipients = { username };
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                IBuildServerNotification buildServerNotification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
                string buildKey = Utils.CreateBuildKey(buildServerNotification);

                // Check that the user doesn't exist prior to the notification
                try
                {
                    userCache.GetUser(username);
                    Assert.Fail();
                }
                catch (KeyNotFoundException)
                {
                    // Expected
                }

                // This will create a user in the cache, but without a hostname
                notificationManager.HandleCommand(buildServerNotification, EventArgs.Empty);
                User user = userCache.GetUser(username);
                Assert.That(user.Username, Is.EqualTo(username));
                Assert.That(user.ActiveBuilds.Contains(buildKey));
                Assert.That(string.IsNullOrEmpty(user.Hostname));

                // Now we create a registration request, which should update the existing user with a hostname
                string hostname = username + "-ws";
                IRequest registrationRequest = new RegistrationRequest(hostname, username);
                notificationManager.HandleCommand(registrationRequest, EventArgs.Empty);
                user = userCache.GetUser(username);
                Assert.That(user.Username, Is.EqualTo(username));
                Assert.That(user.Hostname, Is.EqualTo(hostname));
            }
            finally
            {
                if (notificationManager != null)
                {
                    notificationManager.Dispose();
                }

                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the handling of a responsibility change notification.
        /// </summary>
        [Test]
        public void TestHandleResponsibilityNotification()
        {
            UserCache userCache = null;
            NotificationManager notificationManager = null;
            try
            {
                userCache = new UserCache();
                notificationManager = new NotificationManager(0, 0, userCache, false);
                string username = "user1";
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildResponsibilityAssigned;
                string state = BuildServerResponsibilityState.Taken;
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                IBuildServerNotification buildServerNotification = new ResponsibilityNotification(notificationType, projectId, buildConfigId, username, state);
                string buildKey = Utils.CreateBuildKey(buildServerNotification);
                notificationManager.HandleCommand(buildServerNotification, EventArgs.Empty);
                User user = userCache.GetUser(username);
                Assert.That(user.Username, Is.EqualTo(username));
                Assert.That(user.BuildsResponsibleFor.Contains(buildKey));
            }
            finally
            {
                if (notificationManager != null)
                {
                    notificationManager.Dispose();
                }

                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the handling of a cache update.
        /// </summary>
        [Test]
        public void TestHandleUpdate()
        {
            NotificationManager notificationManager = null;
            UserCache userCache = null;
            try
            {
                // Setup the basics
                object syncRoot = new object();
                int serverPort = 30001;
                int clientPort = 30002;
                userCache = new UserCache();
                notificationManager = new NotificationManager(serverPort, clientPort, userCache, false);
                string buildKey1 = "buildkey1";
                string buildKey2 = "buildkey2";
                string username = "user1";
                string hostname = "localhost";

                // Create a user that has one build building and is responsible for one other build
                User user = new User(username) {
                                                       Hostname = hostname
                                               };
                user.ActiveBuilds.Add(buildKey1);
                user.BuildsResponsibleFor.Add(buildKey2);

                // A dummy client which the notification manager will notify
                Listener listener = new Listener(IPAddress.Any, clientPort);
                Dictionary<RequestType, IRequest> requests = new Dictionary<RequestType, IRequest>();
                listener.OnCommandReceived += (sender, args) =>
                {
                    requests.Add(((IRequest)sender).Type, (IRequest)sender);
                    lock (syncRoot)
                    {
                        Monitor.Pulse(syncRoot);
                    }
                };
                listener.Start();
                Assert.That(listener.Running, Is.True);

                // Raise an update for this user and wait until all events caused have been raised
                lock (syncRoot)
                {
                    notificationManager.HandleUpdate(user, EventArgs.Empty);
                    ////Monitor.Wait(syncRoot, 5000);
                    Monitor.Wait(syncRoot, 5000);
                }

                listener.Stop();
                Assert.That(listener.Running, Is.False);

                // Test
                Assert.That(requests.ContainsKey(RequestType.BuildActive));
                BuildActiveRequest buildActiveRequest = (BuildActiveRequest)requests[RequestType.BuildActive];
                Assert.That(buildActiveRequest.IsBuildsActive, Is.True);
            }
            finally
            {
                if (notificationManager != null)
                {
                    notificationManager.Dispose();
                }

                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the initialisation with a simple integration test.
        /// </summary>
        /// <remarks>
        /// We can't test the cache size as a valid condition that the test passed, 
        /// since all builds might be green at the time of initialization and the 
        /// cache will be empty. 
        /// </remarks>
        [Test]
        [Timeout(120000)]
        [Category("LongRunning")]
        public void TestInitialize()
        {
            UserCache userCache = null;
            NotificationManager notificationManager = null;
            try
            {
                userCache = new UserCache();
                notificationManager = new NotificationManager(0, 0, userCache, true);
                notificationManager.Start();
                Assert.That(notificationManager.Running, Is.True);
                notificationManager.Stop();
                Assert.That(notificationManager.Running, Is.False);
            }
            finally
            {
                if (notificationManager != null)
                {
                    notificationManager.Dispose();
                }

                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that a running listener's Start method can be invoked twice without failure.
        /// </summary>
        [Test]
        [Timeout(5000)]
        public void TestStartAndStartAgain()
        {
            NotificationManager notificationManager = null;
            try
            {
                notificationManager = new NotificationManager(0, 0, new UserCache(), false);
                notificationManager.Start();
                Assert.That(notificationManager.Running, Is.True);
                notificationManager.Start();
                Assert.That(notificationManager.Running, Is.True);
                notificationManager.Stop();
                Assert.That(notificationManager.Running, Is.False);
            }
            finally
            {
                if (notificationManager != null)
                {
                    notificationManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that a running listener's Start and Stop methods can be invoked without failure.
        /// </summary>
        [Test]
        [Timeout(5000)]
        public void TestStartAndStop()
        {
            NotificationManager notificationManager = null;
            try
            {
                notificationManager = new NotificationManager(0, 0, new UserCache(), false);
                notificationManager.Start();
                Assert.That(notificationManager.Running, Is.True);
                notificationManager.Stop();
                Assert.That(notificationManager.Running, Is.False);
            }
            finally
            {
                if (notificationManager != null)
                {
                    notificationManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that a running listener's Start method can be invoked twice without failure.
        /// </summary>
        [Test]
        [Timeout(5000)]
        public void TestStopAndStopAgain()
        {
            NotificationManager notificationManager = null;
            try
            {
                notificationManager = new NotificationManager(0, 0, new UserCache(), false);
                notificationManager.Start();
                Assert.That(notificationManager.Running, Is.True);
                notificationManager.Stop();
                Assert.That(notificationManager.Running, Is.False);
                notificationManager.Stop();
                Assert.That(notificationManager.Running, Is.False);
            }
            finally
            {
                if (notificationManager != null)
                {
                    notificationManager.Dispose();
                }
            }
        }

        #endregion
    }
}