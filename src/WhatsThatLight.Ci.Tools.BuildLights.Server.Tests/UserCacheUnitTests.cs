// <copyright file="UserCacheUnitTests.cs" company="What's That Light?">
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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    using WhatsThatLight.Ci.Tools.BuildLights.Common;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Constants;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Entities;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Notifications;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Requests;
    using WhatsThatLight.Ci.Tools.BuildLights.Server.Interfaces;

    using NMock;

    using NUnit.Framework;

    /// <summary>
    /// Test the user cache.
    /// </summary>
    [TestFixture]
    public sealed class UserCacheUnitTests : IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// Mock factory. 
        /// </summary>
        private MockFactory mockFactory;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets up this instance.
        /// </summary>
        [TestFixtureSetUp]
        public void Setup()
        {
            this.mockFactory = new MockFactory();
        }

        /// <summary>
        /// Tests the get registered users.
        /// </summary>
        [Test]
        public void TestGetRegisteredUsers()
        {
            UserCache userCache = null;
            try
            {
                userCache = new UserCache();
                List<User> userList = new List<User>(userCache.GetRegisteredUsers());
                Assert.That(userList.Count, NUnit.Framework.Is.EqualTo(0));
                string username1 = "user1";
                string hostname1 = username1 + "-ws";
                string username2 = "user2";
                string hostname2 = string.Empty;
                RegistrationRequest request1 = new RegistrationRequest(hostname1, username1);
                RegistrationRequest request2 = new RegistrationRequest(hostname2, username2);
                userCache.Register(request1);
                userCache.Register(request2);
                userList = new List<User>(userCache.GetRegisteredUsers());
                Assert.That(userList.Count, NUnit.Framework.Is.EqualTo(1));
                User user = userList[0];
                Assert.That(user.Hostname, NUnit.Framework.Is.EqualTo(hostname1));
                Assert.That(user.Username, NUnit.Framework.Is.EqualTo(username1));
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Test initialization.
        /// </summary>
        [Test]
        public void TestInitializeWithMocks()
        {
            UserCache userCache = null;
            try
            {
                // Setup
                string username = "user";
                string projectId = "project1";
                string buildConfigId = "config1";
                string[] recipients = { username };
                IBuildServerNotification buildBuildingNotification = new BuildNotification(BuildServerNotificationType.BuildBuilding, projectId, buildConfigId, recipients);
                IBuildServerNotification buildFailedNotification = new BuildNotification(BuildServerNotificationType.BuildFailed, projectId, buildConfigId, recipients);
                string buildKey = Utils.CreateBuildKey(buildFailedNotification);

                // Mocks
                Mock<IBuildServerClient> mockBuildServerClient = this.mockFactory.CreateMock<IBuildServerClient>();
                List<IBuildServerNotification> activeBuilds = new List<IBuildServerNotification> {
                                                                                                         buildBuildingNotification
                                                                                                 };
                mockBuildServerClient.Expects.One.Method(x => x.GetAllActiveBuildNotifications()).WillReturn(activeBuilds);
                List<IBuildServerNotification> buildsThatRequireAttention = new List<IBuildServerNotification> {
                                                                                                                       buildFailedNotification
                                                                                                               };
                mockBuildServerClient.Expects.One.Method(x => x.GetAllBuildsThatRequireAttention()).WillReturn(buildsThatRequireAttention);

                // Execute
                userCache = new UserCache();
                userCache.Initialize(mockBuildServerClient.MockObject);

                // Test
                User actualUser = userCache.GetUser(username);
                Assert.That(actualUser.ActiveBuilds.Count, NUnit.Framework.Is.EqualTo(1));
                Assert.That(actualUser.ActiveBuilds.Contains(buildKey));
                Assert.That(actualUser.BuildsResponsibleFor.Count, NUnit.Framework.Is.EqualTo(1));
                Assert.That(actualUser.BuildsResponsibleFor.Contains(buildKey));
                this.mockFactory.VerifyAllExpectationsHaveBeenMet();
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the priority timer with build failure.
        /// </summary>
        [Test]
        public void TestPriorityTimerWithBuildFailureFollowedByBuildSuccess()
        {
            UserCache userCache = null;
            try
            {
                object syncRoot = new object();
                int numberOfEvents = 0;
                int priorityTimerPeriod = 1000;
                TimeSpan priorityPeriod = TimeSpan.Zero;
                string projectId = "project1";
                string buildConfigId = "buildConfig1";
                string username = "user1";
                string[] recipients = { username };
                userCache = new UserCache(priorityTimerPeriod, priorityPeriod);
                User user = null;
                userCache.OnUpdate += (sender, args) =>
                {
                    numberOfEvents++;
                    if (numberOfEvents != 2)
                    {
                        return;
                    }

                    user = (User)sender;
                    lock (syncRoot)
                    {
                        Monitor.Pulse(syncRoot);
                    }
                };

                // We fail a build and after the priority timer period expires this user's
                // priority attention will be required
                BuildNotification notification = new BuildNotification(BuildServerNotificationType.BuildFailed, projectId, buildConfigId, recipients);
                userCache.Update(notification);
                lock (syncRoot)
                {
                    Monitor.Wait(syncRoot, 2 * priorityTimerPeriod);
                }

                Assert.That(numberOfEvents, NUnit.Framework.Is.EqualTo(2));
                Assert.That(user, NUnit.Framework.Is.Not.Null);
                Assert.That(user.BuildsResponsibleFor.Count, NUnit.Framework.Is.EqualTo(1));
                Assert.That(user.ActiveBuilds.Count, NUnit.Framework.Is.EqualTo(0));
                Assert.That(user.IsAttentionPriority, NUnit.Framework.Is.True);

                // A successful build must now correctly reset the priority properties for the user
                notification = new BuildNotification(BuildServerNotificationType.BuildSuccessful, projectId, buildConfigId, recipients);
                userCache.Update(notification);
                lock (syncRoot)
                {
                    Monitor.Wait(syncRoot, 2 * priorityTimerPeriod);
                }

                Assert.That(numberOfEvents, NUnit.Framework.Is.EqualTo(3));
                Assert.That(user, NUnit.Framework.Is.Not.Null);
                Assert.That(user.BuildsResponsibleFor.Count, NUnit.Framework.Is.EqualTo(0));
                Assert.That(user.ActiveBuilds.Count, NUnit.Framework.Is.EqualTo(0));
                Assert.That(user.IsAttentionPriority, NUnit.Framework.Is.False);
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the priority timer with a responsibility notification.
        /// </summary>
        [Test]
        public void TestPriorityTimerWithResponsibilityNotification()
        {
            UserCache userCache = null;
            try
            {
                object syncRoot = new object();
                int numberOfEvents = 0;
                int priorityTimerPeriod = 1000;
                TimeSpan priorityPeriod = TimeSpan.Zero;
                string projectId = "project1";
                string buildConfigId = "buildConfig1";
                string username = "user1";
                string state = BuildServerResponsibilityState.Taken;
                userCache = new UserCache(priorityTimerPeriod, priorityPeriod);
                User user = null;
                userCache.OnUpdate += (sender, args) =>
                {
                    numberOfEvents++;
                    if (numberOfEvents != 2)
                    {
                        return;
                    }

                    user = (User)sender;
                    lock (syncRoot)
                    {
                        Monitor.Pulse(syncRoot);
                    }
                };
                ResponsibilityNotification notification = new ResponsibilityNotification(BuildServerNotificationType.BuildResponsibilityAssigned, projectId, buildConfigId, username, state);
                userCache.Update(notification);
                lock (syncRoot)
                {
                    Monitor.Wait(syncRoot, 2 * priorityTimerPeriod);
                }

                Assert.That(numberOfEvents, NUnit.Framework.Is.EqualTo(2));
                Assert.That(user, NUnit.Framework.Is.Not.Null);
                Assert.That(user.BuildsResponsibleFor.Count, NUnit.Framework.Is.EqualTo(1));
                Assert.That(user.ActiveBuilds.Count, NUnit.Framework.Is.EqualTo(0));
                Assert.That(user.IsAttentionPriority, NUnit.Framework.Is.True);
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the registration of a new user in the cache via the event mechanism.
        /// </summary>
        [Test]
        public void TestRegistrationUpdateEvent()
        {
            UserCache userCache = null;
            try
            {
                userCache = new UserCache();
                User user = null;
                userCache.OnUpdate += delegate(object sender, EventArgs args) { user = (User)sender; };
                string username = "user1";
                string hostname = username + "-ws";
                RegistrationRequest request = new RegistrationRequest(hostname, username);
                userCache.Register(request);
                Assert.That(user, NUnit.Framework.Is.Not.Null);
                Assert.That(user.Username, NUnit.Framework.Is.EqualTo(username));
                Assert.That(user.Hostname, NUnit.Framework.Is.EqualTo(hostname));
                Assert.That(user.ActiveBuilds.Count, NUnit.Framework.Is.EqualTo(0));
                Assert.That(user.BuildsResponsibleFor.Count, NUnit.Framework.Is.EqualTo(0));
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that the build key doesn't get removed then trying to cause a duplicate.
        /// </summary>
        [Test]
        public void TestUpdateDuplicateBuildKeyForActiveBuilds()
        {
            UserCache userCache = null;
            try
            {
                // Shared
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                userCache = new UserCache();

                // Events
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
                string username = "user1";
                string[] recipients = { username };
                IBuildServerNotification notification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
                string buildKey = Utils.CreateBuildKey(notification);
                userCache.Update(notification);
                userCache.Update(notification);

                // Test
                Assert.That(userCache.GetUser(username).ActiveBuilds.Contains(buildKey));
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that the build key doesn't get removed then trying to cause a duplicate and then one of the builds complete.
        /// </summary>
        [Test]
        [Ignore("Due to the absence of a build cancelled event on the TeamCity APIs, this cannot be implemented.")]
        public void TestUpdateDuplicateBuildKeyForActiveBuildsOneMustRemainAfterTheOtherFinished()
        {
            UserCache userCache = null;
            try
            {
                // Shared
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                userCache = new UserCache();

                // Events
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
                string username = "user1";
                string[] recipients = { username };
                IBuildServerNotification notification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
                string buildKey = Utils.CreateBuildKey(notification);
                userCache.Update(notification);
                userCache.Update(notification);
                notificationType = BuildServerNotificationType.BuildSuccessful;
                notification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
                userCache.Update(notification);

                // Test
                Assert.That(userCache.GetUser(username).ActiveBuilds.Contains(buildKey));
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that the build key doesn't get removed then trying to cause a duplicate.
        /// </summary>
        [Test]
        public void TestUpdateDuplicateBuildKeyForResponsibleUser()
        {
            UserCache userCache = null;
            try
            {
                // Shared
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                userCache = new UserCache();

                // Events
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildResponsibilityAssigned;
                string username = "user1";
                IBuildServerNotification notification = new ResponsibilityNotification(notificationType, projectId, buildConfigId, username, BuildServerResponsibilityState.Taken);
                string buildKey = Utils.CreateBuildKey(notification);
                userCache.Update(notification);
                userCache.Update(notification);

                // Test
                Assert.That(userCache.GetUser(username).BuildsResponsibleFor.Contains(buildKey));
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that the build key doesn't get removed then trying to cause a duplicate.
        /// </summary>
        [Test]
        public void TestUpdateDuplicateBuildKeyOnActiveBuildForResponsibleUser()
        {
            UserCache userCache = null;
            try
            {
                // Shared
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                userCache = new UserCache();

                // Events
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildFailed;
                string username = "user1";
                string[] recipients = { username };
                IBuildServerNotification notification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
                string buildKey = Utils.CreateBuildKey(notification);
                userCache.Update(notification);
                userCache.Update(notification);

                // Test
                Assert.That(userCache.GetUser(username).BuildsResponsibleFor.Contains(buildKey));
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that multiple users have the correct builds building for multiple running builds.
        /// </summary>
        [Test]
        public void TestUpdateMultipleUsersMultipleBuildsBuilding()
        {
            UserCache userCache = null;
            try
            {
                // Shared
                string projectId1 = "project1";
                string buildConfigId1 = "buildconfig1";
                string projectId2 = "project2";
                string buildConfigId2 = "buildconfig2";
                userCache = new UserCache();

                // Events
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
                string username1 = "user1";
                string username2 = "user2";
                string[] recipients = { username1, username2 };
                IBuildServerNotification notification1 = new BuildNotification(notificationType, projectId1, buildConfigId1, recipients);
                string buildKey1 = Utils.CreateBuildKey(notification1);
                userCache.Update(notification1);
                IBuildServerNotification notification2 = new BuildNotification(notificationType, projectId2, buildConfigId2, recipients);
                string buildKey2 = Utils.CreateBuildKey(notification2);
                userCache.Update(notification2);

                // Both must have one build building
                User user1 = userCache.GetUser(username1);
                Assert.That(user1.IsBuildActive());
                Assert.That(user1.ActiveBuilds.Count, NUnit.Framework.Is.EqualTo(2));
                Assert.That(user1.ActiveBuilds.Contains(buildKey1));
                Assert.That(user1.ActiveBuilds.Contains(buildKey2));
                User user2 = userCache.GetUser(username2);
                Assert.That(user2.IsBuildActive());
                Assert.That(user2.ActiveBuilds.Count, NUnit.Framework.Is.EqualTo(2));
                Assert.That(user2.ActiveBuilds.Contains(buildKey1));
                Assert.That(user2.ActiveBuilds.Contains(buildKey2));
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that multiple users with multiple builds that completed have none active.
        /// </summary>
        [Test]
        public void TestUpdateMultipleUsersMultipleBuildsBuiltSuccessful()
        {
            UserCache userCache = null;
            try
            {
                // Shared
                string projectId1 = "project1";
                string buildConfigId1 = "buildconfig1";
                string projectId2 = "project2";
                string buildConfigId2 = "buildconfig2";
                userCache = new UserCache();

                // Events -- all building
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
                string username1 = "user1";
                string username2 = "user2";
                string[] recipients = { username1, username2 };
                IBuildServerNotification notification1 = new BuildNotification(notificationType, projectId1, buildConfigId1, recipients);
                userCache.Update(notification1);
                IBuildServerNotification notification2 = new BuildNotification(notificationType, projectId2, buildConfigId2, recipients);
                userCache.Update(notification2);

                // Events -- all successful
                notificationType = BuildServerNotificationType.BuildSuccessful;
                notification1 = new BuildNotification(notificationType, projectId1, buildConfigId1, recipients);
                userCache.Update(notification1);
                notification2 = new BuildNotification(notificationType, projectId2, buildConfigId2, recipients);
                userCache.Update(notification2);

                // Nobody must have anything building
                User user1 = userCache.GetUser(username1);
                Assert.That(user1.IsBuildActive(), NUnit.Framework.Is.False);
                Assert.That(user1.ActiveBuilds.Count, NUnit.Framework.Is.EqualTo(0));
                User user2 = userCache.GetUser(username2);
                Assert.That(user2.IsBuildActive(), NUnit.Framework.Is.False);
                Assert.That(user2.ActiveBuilds.Count, NUnit.Framework.Is.EqualTo(0));
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that multiple users with the same 2 builds each have only 1 active when the other one finished.
        /// </summary>
        [Test]
        public void TestUpdateMultipleUsersMultipleBuildsOneBuildingOneSuccessful()
        {
            UserCache userCache = null;
            try
            {
                // Shared
                string projectId1 = "project1";
                string buildConfigId1 = "buildconfig1";
                string projectId2 = "project2";
                string buildConfigId2 = "buildconfig2";
                userCache = new UserCache();

                // Events -- all building
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
                string username1 = "user1";
                string username2 = "user2";
                string[] recipients = { username1, username2 };
                IBuildServerNotification notification1 = new BuildNotification(notificationType, projectId1, buildConfigId1, recipients);
                string buildKey1 = Utils.CreateBuildKey(notification1);
                userCache.Update(notification1);
                IBuildServerNotification notification2 = new BuildNotification(notificationType, projectId2, buildConfigId2, recipients);
                string buildKey2 = Utils.CreateBuildKey(notification2);
                userCache.Update(notification2);

                // Events -- one successful
                notificationType = BuildServerNotificationType.BuildSuccessful;
                notification1 = new BuildNotification(notificationType, projectId1, buildConfigId1, recipients);
                userCache.Update(notification1);

                // Both users must have only 1 build active
                User user1 = userCache.GetUser(username1);
                Assert.That(user1.IsBuildActive());
                Assert.That(user1.ActiveBuilds.Count, NUnit.Framework.Is.EqualTo(1));
                Assert.That(user1.ActiveBuilds.Contains(buildKey1), NUnit.Framework.Is.False);
                Assert.That(user1.ActiveBuilds.Contains(buildKey2));
                User user2 = userCache.GetUser(username2);
                Assert.That(user2.IsBuildActive());
                Assert.That(user2.ActiveBuilds.Count, NUnit.Framework.Is.EqualTo(1));
                Assert.That(user2.ActiveBuilds.Contains(buildKey1), NUnit.Framework.Is.False);
                Assert.That(user2.ActiveBuilds.Contains(buildKey2));
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests that multiple users are not responsible for a build.
        /// </summary>
        [Test]
        public void TestUpdateNoMultipleUsersResponsibleForBuild()
        {
            UserCache userCache = null;
            try
            {
                // Shared
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                userCache = new UserCache();

                // User 1 gets assigned
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildResponsibilityAssigned;
                string username1 = "user1";
                IBuildServerNotification notification1 = new ResponsibilityNotification(notificationType, projectId, buildConfigId, username1, BuildServerResponsibilityState.Taken);
                userCache.Update(notification1);

                // User 2 gets assigned
                string username2 = "user2";
                IBuildServerNotification notification2 = new ResponsibilityNotification(notificationType, projectId, buildConfigId, username2, BuildServerResponsibilityState.Taken);
                userCache.Update(notification2);

                // User 2 but not user 1 must be responsible
                User user1 = userCache.GetUser(username1);
                Assert.That(user1.IsAttentionRequired(), NUnit.Framework.Is.False);
                User user2 = userCache.GetUser(username2);
                Assert.That(user2.IsAttentionRequired());
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the update notification when there's an active build.
        /// </summary>
        [Test]
        public void TestUpdateNotificationForActiveBuild()
        {
            UserCache userCache = null;
            try
            {
                // Setup
                userCache = new UserCache();
                ConcurrentDictionary<string, User> userDictionary = new ConcurrentDictionary<string, User>();
                userCache.OnUpdate += delegate(object sender, EventArgs args)
                {
                    User user = (User)sender;
                    userDictionary[user.Username] = user;
                };
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                string username1 = "user11";
                string username2 = "user21";
                string[] recipients = { username1, username2 };
                IBuildServerNotification expectedNotification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);

                // Execute
                userCache.Update(expectedNotification);

                // Test
                foreach (string recipient in recipients)
                {
                    Assert.That(userDictionary.ContainsKey(recipient));
                    User actualUser = userDictionary[recipient];
                    Assert.That(actualUser.Username, NUnit.Framework.Is.EqualTo(recipient));
                    Assert.That(actualUser.IsBuildActive(), NUnit.Framework.Is.True);
                    Assert.That(actualUser.IsAttentionRequired(), NUnit.Framework.Is.False);
                }
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions != null)
                {
                    foreach (Exception exception in ex.InnerExceptions)
                    {
                        Console.WriteLine(exception.Message);
                        Console.WriteLine(exception.StackTrace);
                    }
                }

                throw;
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the update notification when there's a responsibility change.
        /// </summary>
        [Test]
        public void TestUpdateNotificationForResponsibleForBuild()
        {
            UserCache userCache = null;
            try
            {
                // Setup
                userCache = new UserCache();
                User actualUser = null;
                userCache.OnUpdate += delegate(object sender, EventArgs args) { actualUser = (User)sender; };
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildResponsibilityAssigned;
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                string username = "user1";
                string state = BuildServerResponsibilityState.Taken;
                IBuildServerNotification expectedNotification = new ResponsibilityNotification(notificationType, projectId, buildConfigId, username, state);

                // Execute
                userCache.Update(expectedNotification);

                // Test
                Assert.That(actualUser, NUnit.Framework.Is.Not.Null);
                Assert.That(actualUser.Username, NUnit.Framework.Is.EqualTo(username));
                Assert.That(actualUser.IsBuildActive(), NUnit.Framework.Is.False);
                Assert.That(actualUser.IsAttentionRequired(), NUnit.Framework.Is.True);
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the update same user made responsible for two different builds.
        /// </summary>
        [Test]
        public void TestUpdateSameUserMadeResponsibleForTwoDifferentBuilds()
        {
            UserCache userCache = null;
            try
            {
                // Shared
                string projectId = "project1";
                string buildConfigId1 = "buildconfig1";
                string buildConfigId2 = "buildconfig2";
                userCache = new UserCache();

                // Events
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildResponsibilityAssigned;
                string username = "user1";
                IBuildServerNotification notification1 = new ResponsibilityNotification(notificationType, projectId, buildConfigId1, username, BuildServerResponsibilityState.Taken);
                string buildKey1 = Utils.CreateBuildKey(notification1);
                userCache.Update(notification1);
                IBuildServerNotification notification2 = new ResponsibilityNotification(notificationType, projectId, buildConfigId2, username, BuildServerResponsibilityState.Taken);
                string buildKey2 = Utils.CreateBuildKey(notification2);
                userCache.Update(notification2);

                // Test
                Assert.That(userCache.GetUser(username).BuildsResponsibleFor.Count, NUnit.Framework.Is.EqualTo(2));
                Assert.That(userCache.GetUser(username).BuildsResponsibleFor.Contains(buildKey1));
                Assert.That(userCache.GetUser(username).BuildsResponsibleFor.Contains(buildKey2));
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the update same user made responsible twice for same build.
        /// </summary>
        [Test]
        public void TestUpdateSameUserMadeResponsibleTwiceForSameBuild()
        {
            UserCache userCache = null;
            try
            {
                // Shared
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                userCache = new UserCache();

                // Events
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildResponsibilityAssigned;
                string username = "user1";
                IBuildServerNotification notification = new ResponsibilityNotification(notificationType, projectId, buildConfigId, username, BuildServerResponsibilityState.Taken);
                string buildKey = Utils.CreateBuildKey(notification);
                userCache.Update(notification);
                userCache.Update(notification);

                // Test
                Assert.That(userCache.GetUser(username).BuildsResponsibleFor.Count, NUnit.Framework.Is.EqualTo(1));
                Assert.That(userCache.GetUser(username).BuildsResponsibleFor.Contains(buildKey));
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the update that the user is both responsible and has an active build.
        /// </summary>
        [Test]
        public void TestUpdateUserBothResponsibleAndHasActiveBuild()
        {
            UserCache userCache = null;
            try
            {
                // Setup
                userCache = new UserCache();
                User actualUser = null;
                userCache.OnUpdate += delegate(object sender, EventArgs args) { actualUser = (User)sender; };
                string username = "user1";
                string projectId = "project1";

                // First notification
                BuildServerNotificationType notificationType1 = BuildServerNotificationType.BuildResponsibilityAssigned;
                string buildConfigId1 = "buildconfig1";
                string state = BuildServerResponsibilityState.Taken;
                IBuildServerNotification notification1 = new ResponsibilityNotification(notificationType1, projectId, buildConfigId1, username, state);
                userCache.Update(notification1);

                // Second notification
                BuildServerNotificationType notificationType2 = BuildServerNotificationType.BuildBuilding;
                string buildConfigId2 = "buildconfig2";
                string[] recipients = { username };
                IBuildServerNotification notification2 = new BuildNotification(notificationType2, projectId, buildConfigId2, recipients);
                userCache.Update(notification2);

                // Test
                Assert.That(actualUser, NUnit.Framework.Is.Not.Null);
                Assert.That(actualUser.Username, NUnit.Framework.Is.EqualTo(username));
                Assert.That(actualUser.IsBuildActive(), NUnit.Framework.Is.True);
                Assert.That(actualUser.IsAttentionRequired(), NUnit.Framework.Is.True);
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the user cache's update method.
        /// </summary>
        [Test]
        public void TestUpdateWithBuildBuildingNotificationForMultipleUsersNoneExist()
        {
            UserCache userCache = null;
            try
            {
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                string username1 = "user1";
                string username2 = "user2";
                string[] recipients = { username1, username2 };
                IBuildServerNotification notification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
                userCache = new UserCache();
                userCache.Update(notification);
                User user1 = userCache.GetUser(username1);
                Assert.That(user1.IsBuildActive());
                User user2 = userCache.GetUser(username2);
                Assert.That(user2.IsBuildActive());
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the user cache's update method.
        /// </summary>
        [Test]
        public void TestUpdateWithBuildBuildingNotificationForSingleUserDoesExist()
        {
            UserCache userCache = null;
            try
            {
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildSuccessful;
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                string username = "user1";
                string[] recipients = { username };
                IBuildServerNotification notification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
                userCache = new UserCache();
                userCache.Update(notification);
                notificationType = BuildServerNotificationType.BuildBuilding;
                notification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
                userCache.Update(notification);
                User user = userCache.GetUser(username);
                Assert.That(user.IsBuildActive());
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the user cache's update method.
        /// </summary>
        [Test]
        public void TestUpdateWithBuildBuildingNotificationForSingleUserDoesNotExist()
        {
            UserCache userCache = null;
            try
            {
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                string username = "user1";
                string[] recipients = { username };
                IBuildServerNotification notification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
                userCache = new UserCache();
                userCache.Update(notification);
                User user = userCache.GetUser(username);
                Assert.That(user.IsBuildActive());
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the update user cache with build failing notification.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        [Test]
        [Sequential]
        public void TestUpdateWithBuildFailingNotification([Values(BuildServerNotificationType.BuildFailed, BuildServerNotificationType.BuildFailedToStart, BuildServerNotificationType.BuildFailing, BuildServerNotificationType.BuildHanging)] BuildServerNotificationType notificationType)
        {
            UserCache userCache = null;
            try
            {
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                string username = "user1";
                string[] recipients = { username };
                IBuildServerNotification notification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
                userCache = new UserCache();
                userCache.Update(notification);
                User user = userCache.GetUser(username);
                Assert.That(user.IsAttentionRequired());
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the update user cache with responsibility taken.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        [Test]
        [Sequential]
        public void TestUpdateWithResponsibilityTaken([Values(BuildServerNotificationType.BuildResponsibilityAssigned, BuildServerNotificationType.TestResponsibilityAssigned)] BuildServerNotificationType notificationType)
        {
            UserCache userCache = null;
            try
            {
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                string username = "user1";
                string recipients = username;
                string state = "TAKEN";
                IBuildServerNotification notification = new ResponsibilityNotification(notificationType, projectId, buildConfigId, recipients, state);
                userCache = new UserCache();
                userCache.Update(notification);
                User user = userCache.GetUser(username);
                Assert.That(user.IsAttentionRequired());
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the user cache's update method.
        /// </summary>
        [Test]
        public void TestUpdateWithResponsibilityTakenUserExists()
        {
            UserCache userCache = null;
            try
            {
                BuildServerNotificationType notificationType = BuildServerNotificationType.BuildResponsibilityAssigned;
                string projectId = "project1";
                string buildConfigId = "buildconfig1";
                string recipient = "user1";
                string state = BuildServerResponsibilityState.Taken;
                IBuildServerNotification notification = new ResponsibilityNotification(notificationType, projectId, buildConfigId, recipient, state);
                userCache = new UserCache();
                userCache.Update(notification);
                state = BuildServerResponsibilityState.Fixed;
                notification = new ResponsibilityNotification(notificationType, projectId, buildConfigId, recipient, state);
                userCache.Update(notification);
                User user = userCache.GetUser(recipient);
                Assert.That(user.IsAttentionRequired(), NUnit.Framework.Is.False);
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
            }
        }

        /// <summary>
        /// Tests the username key is lower case.
        /// </summary>
        [Test]
        public void TestUsernameKeyIsLowerCase()
        {
            UserCache userCache = null;
            try
            {
                // Setup
                string username = "User1";
                string projectId = "project1";
                string buildConfigId = "buildConfigId";
                string[] recipients = { username };
                userCache = new UserCache();
                BuildNotification buildNotification = new BuildNotification(BuildServerNotificationType.BuildBuilding, projectId, buildConfigId, recipients);
                userCache.Update(buildNotification);

                // User1 must not exist
                try
                {
                    userCache.GetUser(username);
                    Assert.Fail();
                }
                catch (KeyNotFoundException)
                {
                }

                // But user1 must
                User user = userCache.GetUser(username.ToLower());

                // Check the details
                Assert.That(user, NUnit.Framework.Is.Not.Null);
                Assert.That(user.Username, NUnit.Framework.Is.EqualTo(username.ToLower()));
            }
            finally
            {
                if (userCache != null)
                {
                    userCache.Dispose();
                }
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
            if (this.mockFactory != null)
            {
                this.mockFactory.Dispose();
            }
        }

        #endregion

        #endregion
    }
}