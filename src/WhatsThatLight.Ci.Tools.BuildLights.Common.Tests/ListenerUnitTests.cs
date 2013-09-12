// <copyright file="ListenerUnitTests.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using WhatsThatLight.Ci.Tools.BuildLights.Common.Constants;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Notifications;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Protocol;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Requests;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Transport;

    using NUnit.Framework;

    /// <summary>
    /// Test the server socket listener.
    /// </summary>
    [TestFixture]
    public class ListenerUnitTests
    {
        #region Public Methods

        /// <summary>
        /// Tests that handle command raises the command received event.
        /// </summary>
        [Test]
        public void TestHandleCommandForBuildNotificationRaisesCommandReceivedEvent()
        {
            // Setup
            Listener listener = new Listener(IPAddress.Any, 0);
            IBuildServerNotification actualNotification = null;
            listener.OnCommandReceived += delegate(object sender, EventArgs args) { actualNotification = (IBuildServerNotification)sender; };
            BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
            string projectId = "project1";
            string buildConfigId = "buildconfig1";
            string username1 = "user1";
            string username2 = "user2";
            string[] recipients = { username1, username2 };
            BuildNotification expectedBuildNotification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
            string command = Parser.Encode(expectedBuildNotification);

            // Fire
            listener.HandleCommand(command);

            // Test
            Assert.That(actualNotification, Is.Not.Null);
            Assert.That(actualNotification, Is.InstanceOf(typeof(BuildNotification)));
            BuildNotification actualBuildNotification = (BuildNotification)actualNotification;
            Assert.That(actualBuildNotification.Type, Is.EqualTo(notificationType));
            Assert.That(actualBuildNotification.ProjectId.Equals(projectId));
            Assert.That(actualBuildNotification.BuildConfigId.Equals(buildConfigId));
            Assert.That(actualBuildNotification.Recipients.Length, Is.EqualTo(2));
            Assert.That(actualBuildNotification.Recipients, Is.EqualTo(recipients));
        }

        /// <summary>
        /// Tests the handle command for multiple commands received event.
        /// </summary>
        [Test]
        public void TestHandleCommandForMultipleCommandsReceivedEvent()
        {
            // Setup
            int maxCommands = 200;
            int numberOfCommandsReceived = 0;
            object runLock = new object();
            int timeout = 30000;
            string hostname = "localhost";
            int port = 39191;
            Listener listener = new Listener(IPAddress.Any, port);
            ConcurrentQueue<IBuildServerNotification> actualNotifications = new ConcurrentQueue<IBuildServerNotification>();

            listener.OnCommandReceived += (sender, args) =>
            {
                lock (runLock)
                {
                    actualNotifications.Enqueue((IBuildServerNotification)sender);
                    Interlocked.Increment(ref numberOfCommandsReceived);
                    Console.WriteLine("Number of commands received: {0}/{1}", numberOfCommandsReceived, maxCommands);
                    if (numberOfCommandsReceived == maxCommands)
                    {
                        Monitor.Pulse(runLock);
                    }
                }
            };

            BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
            string projectId = "project1";
            string buildConfigId = "buildconfig1";
            string username1 = "user1";
            string username2 = "user2";
            string[] recipients = { username1, username2 };
            BuildNotification expectedBuildNotification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);
            string command = Parser.Encode(expectedBuildNotification);
            listener.Start();
            Assert.That(listener.Running, Is.True);
            Console.WriteLine("Port: {0}", port);

            // Fire
            List<bool> sendResults = new List<bool>(maxCommands);
            Thread[] threads = new Thread[maxCommands];
            Console.WriteLine("Creating {0} threads", maxCommands);
            for (int i = 0; i < maxCommands; i++)
            {
                threads[i] = new Thread(() => sendResults.Add(Utils.SendCommand(hostname, port, command)));

                threads[i].Name = i.ToString(CultureInfo.InvariantCulture);
            }

            Console.WriteLine("Starting {0} threads", maxCommands);
            Parallel.ForEach(threads, thread =>
            {
                Console.WriteLine("Starting thread {0}", thread.Name);
                thread.Start();
            });

            lock (runLock)
            {
                Console.WriteLine("Waiting to receive all commands");
                Monitor.Wait(runLock, timeout);
            }

            listener.Stop();
            Assert.That(listener.Running, Is.False);
            Console.WriteLine("Total notifications received: {0}/{1}", actualNotifications.Count, maxCommands);
            Assert.That(sendResults.Contains(false), Is.False, "One or more commands could not be sent");
            Assert.That(actualNotifications.Count, Is.EqualTo(maxCommands), "Not all commands were received - test might have timed out");

            foreach (IBuildServerNotification actualNotification in actualNotifications)
            {
                Assert.That(actualNotification, Is.Not.Null);
                Assert.That(actualNotification, Is.InstanceOf(typeof(BuildNotification)));
                BuildNotification actualBuildNotification = (BuildNotification)actualNotification;
                Assert.That(actualBuildNotification.Type, Is.EqualTo(notificationType));
                Assert.That(actualBuildNotification.ProjectId.Equals(projectId));
                Assert.That(actualBuildNotification.BuildConfigId.Equals(buildConfigId));
                Assert.That(actualBuildNotification.Recipients.Length, Is.EqualTo(2));
                Assert.That(actualBuildNotification.Recipients, Is.EqualTo(recipients));
            }
        }

        /// <summary>
        /// Tests the handle command for a registration request.
        /// </summary>
        [Test]
        public void TestHandleCommandForRegistrationRequestRaisesCommandReceivedEvent()
        {
            // Setup
            Listener listener = new Listener(IPAddress.Any, 0);
            IRequest actualRequest = null;
            listener.OnCommandReceived += delegate(object sender, EventArgs args) { actualRequest = (IRequest)sender; };
            string username = "user1";
            string hostname = username + "-ws";
            RegistrationRequest actualRegistrationRequest = new RegistrationRequest(hostname, username);
            string command = Parser.Encode(actualRegistrationRequest);

            // Fire
            listener.HandleCommand(command);

            // Test
            Assert.That(actualRequest, Is.Not.Null);
            Assert.That(actualRequest, Is.InstanceOf(typeof(RegistrationRequest)));
            RegistrationRequest expectedRegistrationRequest = (RegistrationRequest)actualRequest;
            Assert.That(expectedRegistrationRequest.Username, Is.EqualTo(username));
            Assert.That(expectedRegistrationRequest.Hostname, Is.EqualTo(hostname));
        }

        /// <summary>
        /// Tests that handle command raises the command received event.
        /// </summary>
        [Test]
        public void TestHandleCommandForResponsibilityNotificationRaisesCommandReceivedEvent()
        {
            // Setup
            Listener listener = new Listener(IPAddress.Any, 0);
            IBuildServerNotification actualNotification = null;
            listener.OnCommandReceived += delegate(object sender, EventArgs args) { actualNotification = (IBuildServerNotification)sender; };
            BuildServerNotificationType notificationType = BuildServerNotificationType.BuildResponsibilityAssigned;
            string projectId = "project1";
            string buildConfigId = "buildconfig1";
            string username = "user1";
            string state = BuildServerResponsibilityState.Taken;
            ResponsibilityNotification actualResponsibilityNotification = new ResponsibilityNotification(notificationType, projectId, buildConfigId, username, state);
            string command = Parser.Encode(actualResponsibilityNotification);

            // Fire
            listener.HandleCommand(command);

            // Test
            Assert.That(actualNotification, Is.Not.Null);
            Assert.That(actualNotification, Is.InstanceOf(typeof(ResponsibilityNotification)));
            ResponsibilityNotification expectedResponsibilityNotification = (ResponsibilityNotification)actualNotification;
            Assert.That(expectedResponsibilityNotification.Type, Is.EqualTo(notificationType));
            Assert.That(expectedResponsibilityNotification.ProjectId.Equals(projectId));
            Assert.That(expectedResponsibilityNotification.BuildConfigId.Equals(buildConfigId));
            Assert.That(expectedResponsibilityNotification.Recipient, Is.EqualTo(username));
            Assert.That(expectedResponsibilityNotification.State, Is.EqualTo(state));
        }

        /// <summary>
        /// Tests that a running listener's Start method can be invoked twice without failure.
        /// </summary>
        [Test]
        [Timeout(5000)]
        public void TestStartAndStartAgain()
        {
            Listener listener = new Listener(IPAddress.Any, 0);
            listener.Start();
            Assert.That(listener.Running, Is.True);
            listener.Start();
            Assert.That(listener.Running, Is.True);
            listener.Stop();
            Assert.That(listener.Running, Is.False);
        }

        /// <summary>
        /// Tests the run and stop methods.
        /// </summary>
        [Test]
        [Timeout(5000)]
        public void TestStartAndStop()
        {
            Listener listener = new Listener(IPAddress.Any, 0);
            listener.Start();
            Assert.That(listener.Running, Is.True);
            listener.Stop();
            Assert.That(listener.Running, Is.False);
        }

        /// <summary>
        /// Tests that a not running listener's Stop method can be invoked twice without failure.
        /// </summary>
        [Test]
        [Timeout(5000)]
        public void TestStopAndStopAgain()
        {
            Listener listener = new Listener(IPAddress.Any, 0);
            listener.Start();
            Assert.That(listener.Running, Is.True);
            listener.Stop();
            Assert.That(listener.Running, Is.False);
            listener.Stop();
            Assert.That(listener.Running, Is.False);
        }

        /// <summary>
        /// Tests that the server doesn't blow up when stopped twice without starting.
        /// </summary>
        [Test]
        public void TestStopTwiceWithoutStart()
        {
            Listener listener = new Listener(IPAddress.Any, 0);
            listener.Stop();
            Assert.That(listener.Running, Is.False);
            listener.Stop();
            Assert.That(listener.Running, Is.False);
        }

        #endregion
    }
}