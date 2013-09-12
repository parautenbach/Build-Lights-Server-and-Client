// <copyright file="ParserUnitTests.cs" company="What's That Light?">
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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    using WhatsThatLight.Ci.Tools.BuildLights.Common.Constants;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Exceptions;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Notifications;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Protocol;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Requests;

    using NUnit.Framework;

    /// <summary>
    /// The class for testing the server listener. 
    /// </summary>
    [TestFixture]
    public class ParserUnitTests
    {
        #region Public Methods

        /// <summary>
        /// Tests that the attention request cannot be constructing when the two states are in conflict. 
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidRequestException))]
        public void TestConstructAttentionRequestWithPriorityWithoutAttentionRequiredSet()
        {
            new AttentionRequest(false, true);
            Assert.Fail();
        }

        /// <summary>
        /// Tests that the construction of a build notification throws an exception for an invalid build server notification type.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidBuildServerNotificationException))]
        public void TestConstructBuildNotificationThrowsExceptionForInvalidBuildServerNotificationType()
        {
            new BuildNotification(BuildServerNotificationType.BuildResponsibilityAssigned, string.Empty, string.Empty, new string[0]);
        }

        /// <summary>
        /// Tests that the construction of a responsibility notification throws an exception for an invalid build server notification type.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidBuildServerNotificationException))]
        public void TestConstructResponsibilityNotificationThrowsExceptionForInvalidBuildServerNotificationType()
        {
            new ResponsibilityNotification(BuildServerNotificationType.BuildBuilding, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Tests the decoding of an attention request for both priorities.
        /// </summary>
        [Test]
        public void TestDecodeAttentionRequestForBothPriorities()
        {
            List<KeyValuePair<bool, bool>> possibilities = new List<KeyValuePair<bool, bool>>();
            possibilities.Add(new KeyValuePair<bool, bool>(false, false));
            possibilities.Add(new KeyValuePair<bool, bool>(true, false));
            possibilities.Add(new KeyValuePair<bool, bool>(true, true));
            RequestType requestType = RequestType.Attention;
            string requestTypeId = ((int)requestType).ToString(CultureInfo.InvariantCulture);
            string requestTypePart = Field.RequestTypeId + Packet.FieldSeparator + requestTypeId + Packet.CommandSeparator;
            foreach (KeyValuePair<bool, bool> possibility in possibilities)
            {
                string attentionPart = Field.AttentionRequired + Packet.FieldSeparator + (possibility.Key ? "1" : "0") + Packet.CommandSeparator;
                string priorityPart = Field.AttentionPriority + Packet.FieldSeparator + (possibility.Value ? "1" : "0") + Packet.PacketTerminator;
                string command = requestTypePart + attentionPart + priorityPart;
                Dictionary<string, string> commandDictionary = Parser.DecodeCommand(command);
                IRequest request = Parser.DecodeRequest(commandDictionary);
                Assert.That(request, Is.TypeOf(typeof(AttentionRequest)));
                AttentionRequest attentionRequest = (AttentionRequest)request;
                Assert.That(attentionRequest.IsAttentionRequired, Is.EqualTo(possibility.Key));
                Assert.That(attentionRequest.IsPriority, Is.EqualTo(possibility.Value));
            }
        }

        /// <summary>
        /// Tests the decode attention request for priority set without attention required set.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidRequestException))]
        public void TestDecodeAttentionRequestForPrioritySetWithoutAttentionRequiredSet()
        {
            RequestType requestType = RequestType.Attention;
            string requestTypeId = ((int)requestType).ToString(CultureInfo.InvariantCulture);
            string requestTypePart = Field.RequestTypeId + Packet.FieldSeparator + requestTypeId + Packet.CommandSeparator;
            string attentionPart = Field.AttentionRequired + Packet.FieldSeparator + "0" + Packet.CommandSeparator;
            string priorityPart = Field.AttentionPriority + Packet.FieldSeparator + "1" + Packet.PacketTerminator;
            string command = requestTypePart + attentionPart + priorityPart;
            Dictionary<string, string> commandDictionary = Parser.DecodeCommand(command);
            Parser.DecodeRequest(commandDictionary);
        }

        /// <summary>
        /// Tests the parse command with empty command.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidCommandException))]
        public void TestDecodeCommandForEmptyCommand()
        {
            Parser.DecodeCommand(Packet.PacketTerminator.ToString(CultureInfo.InvariantCulture));
            Assert.Fail();
        }

        /// <summary>
        /// Tests the parse command with empty string.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidCommandException))]
        public void TestDecodeCommandForEmptyString()
        {
            Parser.DecodeCommand(string.Empty);
            Assert.Fail();
        }

        /// <summary>
        /// Tests the parse command for invalid command terminator.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidCommandException))]
        public void TestDecodeCommandForInvalidCommandTerminator()
        {
            string command = string.Format("foo=0;bar=1;baz=2?");
            Parser.DecodeCommand(command);
            Assert.Fail();
        }

        /// <summary>
        /// Tests the parse command with empty command.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidCommandException))]
        public void TestDecodeCommandForInvalidKeyValuePairCommand()
        {
            Parser.DecodeCommand("foo!");
            Assert.Fail();
        }

        /// <summary>
        /// Tests the parse command for no command terminator.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidCommandException))]
        public void TestDecodeCommandForNoCommandTerminator()
        {
            string command = "foo=0;bar=1;baz=2";
            Parser.DecodeCommand(command);
            Assert.Fail();
        }

        /// <summary>
        /// Tests the parse command for valid command.
        /// </summary>
        [Test]
        public void TestDecodeCommandForValidCommand()
        {
            string command = "foo=0;bar=1;baz=2!";
            Dictionary<string, string> expectedCommandDictionary = new Dictionary<string, string> { { "foo", "0" }, { "bar", "1" }, { "baz", "2" } };
            Dictionary<string, string> actualCommandDictionary = Parser.DecodeCommand(command);
            CollectionAssert.AreEqual(actualCommandDictionary, expectedCommandDictionary);
        }

        /// <summary>
        /// Tests the parse notification is an invalid build notification.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidBuildServerNotificationException))]
        public void TestDecodeNotificationIsInvalidBuildNotification()
        {
            BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.NotificationTypeId, ((int)notificationType).ToString(CultureInfo.InvariantCulture) } };
            Parser.DecodeBuildServerNotification(commandDictionary);
        }

        /// <summary>
        /// Tests the parse notification is an invalid notification that throws an exception.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidBuildServerNotificationException))]
        public void TestDecodeNotificationIsInvalidNotification()
        {
            BuildServerNotificationType notificationType = BuildServerNotificationType.None;
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.NotificationTypeId, ((int)notificationType).ToString(CultureInfo.InvariantCulture) } };
            Parser.DecodeBuildServerNotification(commandDictionary);
        }

        /// <summary>
        /// Tests the parse notification is an invalid responsibility notification.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidBuildServerNotificationException))]
        public void TestDecodeNotificationIsInvalidResponsibilityNotification()
        {
            BuildServerNotificationType notificationType = BuildServerNotificationType.BuildResponsibilityAssigned;
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.NotificationTypeId, ((int)notificationType).ToString(CultureInfo.InvariantCulture) }, { Field.ResponsibilityState, BuildServerResponsibilityState.Taken } };
            Parser.DecodeBuildServerNotification(commandDictionary);
        }

        /// <summary>
        /// Tests the parse notification is a valid build notification.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        [Test]
        [Sequential]
        public void TestDecodeNotificationIsValidBuildNotification([Values(BuildServerNotificationType.BuildBuilding, BuildServerNotificationType.BuildFailed, BuildServerNotificationType.BuildFailedToStart, BuildServerNotificationType.BuildFailing, BuildServerNotificationType.BuildHanging, BuildServerNotificationType.BuildSuccessful)] BuildServerNotificationType notificationType)
        {
            string projectId = "project1";
            string buildConfigId = "buildconfig1";
            string username1 = "user1";
            string username2 = "user2";
            string recipients = username1 + Packet.ListSeparator + username2;
            string[] recipientsArray = { username1, username2 };
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.NotificationTypeId, ((int)notificationType).ToString(CultureInfo.InvariantCulture) }, { Field.ProjectId, projectId }, { Field.BuildConfigId, buildConfigId }, { Field.BuildRecipients, recipients } };
            IBuildServerNotification notification = Parser.DecodeBuildServerNotification(commandDictionary);
            Assert.That(notification, Is.TypeOf(typeof(BuildNotification)));
            BuildNotification buildNotification = (BuildNotification)notification;
            Assert.That(buildNotification.Type, Is.EqualTo(notificationType));
            Assert.That(buildNotification.ProjectId, Is.EqualTo(projectId));
            Assert.That(buildNotification.BuildConfigId, Is.EqualTo(buildConfigId));
            Assert.That(buildNotification.Recipients, Is.EqualTo(recipientsArray));
        }

        /// <summary>
        /// Tests the parse notification is a valid responsibility notification.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        [Test]
        [Sequential]
        public void TestDecodeNotificationIsValidResponsibilityNotification([Values(BuildServerNotificationType.BuildResponsibilityAssigned, BuildServerNotificationType.TestResponsibilityAssigned)] BuildServerNotificationType notificationType)
        {
            string projectId = "project1";
            string buildConfigId = "buildconfig1";
            string responsibleUser = "user1";
            string state = "TAKEN";
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.NotificationTypeId, ((int)notificationType).ToString(CultureInfo.InvariantCulture) }, { Field.ProjectId, projectId }, { Field.BuildConfigId, buildConfigId }, { Field.ResponsibleUsername, responsibleUser }, { Field.ResponsibilityState, state } };
            IBuildServerNotification notification = Parser.DecodeBuildServerNotification(commandDictionary);
            Assert.That(notification, Is.TypeOf(typeof(ResponsibilityNotification)));
            ResponsibilityNotification responsibilityNotification = (ResponsibilityNotification)notification;
            Assert.That(responsibilityNotification.Type, Is.EqualTo(notificationType));
            Assert.That(responsibilityNotification.ProjectId, Is.EqualTo(projectId));
            Assert.That(responsibilityNotification.BuildConfigId, Is.EqualTo(buildConfigId));
            Assert.That(responsibilityNotification.Recipient, Is.EqualTo(responsibleUser));
            Assert.That(responsibilityNotification.State, Is.EqualTo(BuildServerResponsibilityState.Taken));
        }

        /// <summary>
        /// Tests the parse notification is an invalid responsibility notification.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidBuildServerResponsibilityStateException))]
        public void TestDecodeNotificationIsValidResponsibilityNotification()
        {
            BuildServerNotificationType notificationType = BuildServerNotificationType.BuildResponsibilityAssigned;
            string projectId = "project1";
            string buildConfigId = "buildconfig1";
            string responsibleUser = "user1";
            string state = "FOO";
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.NotificationTypeId, ((int)notificationType).ToString(CultureInfo.InvariantCulture) }, { Field.ProjectId, projectId }, { Field.BuildConfigId, buildConfigId }, { Field.ResponsibleUsername, responsibleUser }, { Field.ResponsibilityState, state } };
            Parser.DecodeBuildServerNotification(commandDictionary);
            Assert.Fail();
        }

        /// <summary>
        /// Tests the parse notification type invalid type must evaluate but be undefined.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidBuildServerNotificationTypeException))]
        public void TestDecodeNotificationTypeInvalidTypeMustThrowException()
        {
            int expectedNotificationTypeValue = 99;
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.NotificationTypeId, expectedNotificationTypeValue.ToString(CultureInfo.InvariantCulture) } };
            Parser.DecodeBuildServerNotificationType(commandDictionary);
            Assert.Fail();
        }

        /// <summary>
        /// Tests the type of the parse notification type valid.
        /// </summary>
        [Test]
        public void TestDecodeNotificationTypeValidType()
        {
            BuildServerNotificationType expectedNotificationType = BuildServerNotificationType.BuildSuccessful;
            string expectedNotificationTypeString = ((int)expectedNotificationType).ToString(CultureInfo.InvariantCulture);
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.NotificationTypeId, expectedNotificationTypeString } };
            BuildServerNotificationType actualNotificationType = Parser.DecodeBuildServerNotificationType(commandDictionary);
            Assert.That(actualNotificationType, Is.EqualTo(expectedNotificationType));
        }

        /// <summary>
        /// Tests the decoding of a request for a valid request.
        /// </summary>
        [Test]
        public void TestDecodeRequestForValidRequest()
        {
            RequestType requestType = RequestType.Register;
            string username = "user1";
            string hostname = username + "-ws";
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.RequestTypeId, ((int)requestType).ToString(CultureInfo.InvariantCulture) }, { Field.Hostname, hostname }, { Field.Username, username } };
            IRequest request = Parser.DecodeRequest(commandDictionary);
            Assert.That(request, Is.InstanceOf(typeof(RegistrationRequest)));
            RegistrationRequest registrationRequest = (RegistrationRequest)request;
            Assert.That(registrationRequest.Type, Is.EqualTo(requestType));
            Assert.That(registrationRequest.Hostname, Is.EqualTo(hostname));
            Assert.That(registrationRequest.Username, Is.EqualTo(username));
        }

        /// <summary>
        /// Tests the decoding of a request is not a valid request.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidRequestException))]
        public void TestDecodeRequestInvalidRequest()
        {
            RequestType requestType = RequestType.Unknown;
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.RequestTypeId, ((int)requestType).ToString(CultureInfo.InvariantCulture) } };
            Parser.DecodeRequest(commandDictionary);
            Assert.Fail();
        }

        /// <summary>
        /// Tests the decoding of a request missing one of the required keys.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidRequestException))]
        public void TestDecodeRequestMissingKey()
        {
            RequestType requestType = RequestType.Register;
            string username = "user1";
            string hostname = username + "-ws";
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.RequestTypeId, ((int)requestType).ToString(CultureInfo.InvariantCulture) }, { Field.Hostname, hostname } };
            Parser.DecodeRequest(commandDictionary);
            Assert.Fail();
        }

        /// <summary>
        /// Tests the decoding of a request type for an invalid type.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidRequestTypeException))]
        public void TestDecodeRequestTypeIsInvalidType()
        {
            int requestType = -1;
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.RequestTypeId, requestType.ToString(CultureInfo.InvariantCulture) } };
            Parser.DecodeRequestType(commandDictionary);
            Assert.Fail();
        }

        /// <summary>
        /// Tests the decode status request.
        /// </summary>
        /// <param name="status">if set to <c>true</c> server is up; else down.</param>
        [Test]
        public void TestDecodeStatusRequest([Values(true, false)] bool status)
        {
            RequestType requestType = RequestType.ServerStatus;
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.RequestTypeId, ((int)requestType).ToString(CultureInfo.InvariantCulture) }, { Field.ServerStatus, Convert.ToInt16(status).ToString(CultureInfo.InvariantCulture) } };
            IRequest request = Parser.DecodeRequest(commandDictionary);
            Assert.That(request, Is.InstanceOf(typeof(StatusRequest)));
            StatusRequest statusRequest = (StatusRequest)request;
            Assert.That(statusRequest.Type, Is.EqualTo(requestType));
            Assert.That(statusRequest.Status, Is.EqualTo(status));
        }

        /// <summary>
        /// Tests that the attention request is encoded correctly. 
        /// </summary>
        /// <param name="attentionRequired">if set to <c>true</c> attention is required.</param>
        [Test]
        [Sequential]
        public void TestEncodeAttentionRequest([Values(true, false)] bool attentionRequired)
        {
            IRequest request = new AttentionRequest(attentionRequired);
            string typePart = Field.RequestTypeId + Packet.FieldSeparator + ((int)RequestType.Attention).ToString(CultureInfo.InvariantCulture) + Packet.CommandSeparator;
            string attentionPart = Field.AttentionRequired + Packet.FieldSeparator + Convert.ToInt16(attentionRequired).ToString(CultureInfo.InvariantCulture) + Packet.CommandSeparator;
            string priorityPart = Field.AttentionPriority + Packet.FieldSeparator + Convert.ToInt16(false).ToString(CultureInfo.InvariantCulture);
            string expectedCommand = typePart + attentionPart + priorityPart + Packet.PacketTerminator;
            string actualCommand = Parser.Encode(request);
            Assert.That(actualCommand, Is.EqualTo(expectedCommand));
        }

        /// <summary>
        /// Tests that the attention request is encoded correctly.
        /// </summary>
        /// <param name="attentionPriority">if set to <c>true</c> [attention priority].</param>
        [Test]
        [Sequential]
        public void TestEncodeAttentionRequestWithPriority([Values(true, false)] bool attentionPriority)
        {
            IRequest request = new AttentionRequest(true, attentionPriority);
            string typePart = Field.RequestTypeId + Packet.FieldSeparator + ((int)RequestType.Attention).ToString(CultureInfo.InvariantCulture) + Packet.CommandSeparator;
            string attentionPart = Field.AttentionRequired + Packet.FieldSeparator + Convert.ToInt16(true).ToString(CultureInfo.InvariantCulture) + Packet.CommandSeparator;
            string priorityPart = Field.AttentionPriority + Packet.FieldSeparator + Convert.ToInt16(attentionPriority).ToString(CultureInfo.InvariantCulture);
            string expectedCommand = typePart + attentionPart + priorityPart + Packet.PacketTerminator;
            string actualCommand = Parser.Encode(request);
            Assert.That(actualCommand, Is.EqualTo(expectedCommand));
        }

        /// <summary>
        /// Tests that the attention request is encoded correctly. 
        /// </summary>
        /// <param name="buildsActive">if set to <c>true</c> [builds active].</param>
        [Test]
        [Sequential]
        public void TestEncodeBuildActiveRequest([Values(true, false)] bool buildsActive)
        {
            IRequest request = new BuildActiveRequest(buildsActive);
            string typePart = Field.RequestTypeId + Packet.FieldSeparator + ((int)RequestType.BuildActive).ToString(CultureInfo.InvariantCulture) + Packet.CommandSeparator;
            string buildActivePart = Field.BuildsActive + Packet.FieldSeparator + Convert.ToInt16(buildsActive).ToString(CultureInfo.InvariantCulture);
            string expectedCommand = typePart + buildActivePart + Packet.PacketTerminator;
            string actualCommand = Parser.Encode(request);
            Assert.That(actualCommand, Is.EqualTo(expectedCommand));
        }

        /// <summary>
        /// Tests the encode build notification.
        /// </summary>
        [Test]
        public void TestEncodeBuildNotification()
        {
            // Construct notification object
            BuildServerNotificationType notificationType = BuildServerNotificationType.BuildBuilding;
            string projectId = "project1";
            string buildConfigId = "buildconfig1";
            string username1 = "user1";
            string username2 = "user2";
            string[] recipients = { username1, username2 };
            BuildNotification notification = new BuildNotification(notificationType, projectId, buildConfigId, recipients);

            // Manually construct a packet
            string notificationPart = Field.NotificationTypeId + Packet.FieldSeparator + ((int)notificationType).ToString(CultureInfo.InvariantCulture) + Packet.CommandSeparator;
            string projectPart = Field.ProjectId + Packet.FieldSeparator + projectId + Packet.CommandSeparator;
            string buildConfigPart = Field.BuildConfigId + Packet.FieldSeparator + buildConfigId + Packet.CommandSeparator;
            string recipientList = string.Join(Packet.ListSeparator.ToString(CultureInfo.InvariantCulture), recipients);
            string recipientsPart = Field.BuildRecipients + Packet.FieldSeparator + recipientList + Packet.PacketTerminator;
            string expectedCommand = notificationPart + projectPart + buildConfigPart + recipientsPart;

            // Test
            string actualCommand = Parser.Encode(notification);
            Assert.That(actualCommand.Equals(expectedCommand));
        }

        /// <summary>
        /// Tests the encode responsibility notification.
        /// </summary>
        [Test]
        public void TestEncodeResponsibilityNotification()
        {
            // Construct notification object
            BuildServerNotificationType notificationType = BuildServerNotificationType.BuildResponsibilityAssigned;
            string projectId = "project1";
            string buildConfigId = "buildconfig1";
            string username = "user1";
            string state = BuildServerResponsibilityState.Taken;
            ResponsibilityNotification notification = new ResponsibilityNotification(notificationType, projectId, buildConfigId, username, state);

            // Manually construct a packet
            string notificationPart = Field.NotificationTypeId + Packet.FieldSeparator + ((int)notificationType).ToString(CultureInfo.InvariantCulture) + Packet.CommandSeparator;
            string projectPart = Field.ProjectId + Packet.FieldSeparator + projectId + Packet.CommandSeparator;
            string buildConfigPart = Field.BuildConfigId + Packet.FieldSeparator + buildConfigId + Packet.CommandSeparator;
            string recipientPart = Field.ResponsibleUsername + Packet.FieldSeparator + username + Packet.CommandSeparator;
            string statePart = Field.ResponsibilityState + Packet.FieldSeparator + state + Packet.PacketTerminator;
            string expectedCommand = notificationPart + projectPart + buildConfigPart + recipientPart + statePart;

            // Test
            string actualCommand = Parser.Encode(notification);
            Assert.That(actualCommand.Equals(expectedCommand));
        }

        /// <summary>
        /// Tests the encode server request.
        /// </summary>
        /// <param name="serverStatus">if set to <c>true</c> [server status].</param>
        [Test]
        [Sequential]
        public void TestEncodeServerRequest([Values(true, false)] bool serverStatus)
        {
            StatusRequest request = new StatusRequest(serverStatus);
            string typePart = Field.RequestTypeId + Packet.FieldSeparator + ((int)RequestType.ServerStatus).ToString(CultureInfo.InvariantCulture) + Packet.CommandSeparator;
            string statusPart = Field.ServerStatus + Packet.FieldSeparator + Convert.ToInt16(serverStatus).ToString(CultureInfo.InvariantCulture);
            string expectedCommand = typePart + statusPart + Packet.PacketTerminator;
            string actualCommand = Parser.Encode(request);
            Assert.That(actualCommand, Is.EqualTo(expectedCommand));
        }

        /// <summary>
        /// Tests that the command is terminated properly when input is empty.
        /// </summary>
        [Test]
        public void TestIsCommandTerminatedProperlyForEmptyString()
        {
            Assert.That(Parser.IsCommandTerminatedProperly(string.Empty), Is.False);
        }

        /// <summary>
        /// Tests that the command is terminated properly for only a packet terminator.
        /// </summary>
        [Test]
        public void TestIsCommandTerminatedProperlyForOnlyTerminator()
        {
            Assert.That(Parser.IsCommandTerminatedProperly(Packet.PacketTerminator.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Tests that the command is terminated properly for random packet contents with no terminator.
        /// </summary>
        [Test]
        public void TestIsCommandTerminatedProperlyForRandomDataAndNoTerminator()
        {
            Assert.That(Parser.IsCommandTerminatedProperly("foo"), Is.False);
        }

        /// <summary>
        /// Tests that the command is terminated properly for random packet contents and a valid terminator.
        /// </summary>
        [Test]
        public void TestIsCommandTerminatedProperlyForRandomDataAndValidTerminator()
        {
            Assert.That(Parser.IsCommandTerminatedProperly("foo" + Packet.PacketTerminator.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Test that the command is not a notification.
        /// </summary>
        [Test]
        public void TestIsNotificationForInvalidNotification()
        {
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { "foo", "bar" } };
            Assert.IsFalse(Parser.IsBuildServerNotification(commandDictionary));
        }

        /// <summary>
        /// Test that the command is a notification.
        /// </summary>
        [Test]
        public void TestIsNotificationForValidNotification()
        {
            Dictionary<string, string> commandDictionary = new Dictionary<string, string> { { Field.NotificationTypeId, BuildServerNotificationType.None.ToString() } };
            Assert.IsTrue(Parser.IsBuildServerNotification(commandDictionary));
        }

        /// <summary>
        /// Tests that an attention request translates to switching the RGB LED to yellow.
        /// </summary>
        [Test]
        public void TestTranslateForBlink1BuildsActive()
        {
            short length = 10;
            BuildActiveRequest request = new BuildActiveRequest(true);
            byte[] expectedBytes = { 0x01, 0x63, 0xFF, 0x65, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00 };
            byte[] actualBytes = Parser.TranslateForBlink1(request, length);
            Assert.That(actualBytes.Length, Is.EqualTo(length));
            Assert.That(actualBytes, Is.EqualTo(expectedBytes).AsCollection);
        }

        /// <summary>
        /// Tests that an attention request translates to an exception.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidTranslationRequestException))]
        public void TestTranslateForBlink1NoBuildsActive()
        {
            Parser.TranslateForBlink1(new BuildActiveRequest(false), 10);
            Assert.Fail();
        }

        /// <summary>
        /// Tests that an attention request translates to switching the RGB LED to red or green appropriately.
        /// </summary>
        [Test]
        public void TestTranslateForBlink1AttentionRequestAttentionRequired()
        {
            short length = 10;

            // Attention is required
            AttentionRequest request = new AttentionRequest(true);
            byte[] expectedBytesForRed = { 0x01, 0x63, 0xFF, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00 };
            byte[] actualBytesForRed = Parser.TranslateForBlink1(request, length);
            Assert.That(actualBytesForRed.Length, Is.EqualTo(length));
            Console.WriteLine(BitConverter.ToString(expectedBytesForRed));
            Console.WriteLine(BitConverter.ToString(actualBytesForRed));
            Assert.That(actualBytesForRed, Is.EqualTo(expectedBytesForRed).AsCollection);
            
            // Priority red will currently look the same as normal priority (plain attention required)
            request = new AttentionRequest(true, true);
            actualBytesForRed = Parser.TranslateForBlink1(request, length);
            Assert.That(actualBytesForRed.Length, Is.EqualTo(length));
            Assert.That(actualBytesForRed, Is.EqualTo(expectedBytesForRed).AsCollection);

            // No attention required
            request = new AttentionRequest(false);
            byte[] expectedBytesForGreen = { 0x01, 0x63, 0x00, 0xFF, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00 };
            byte[] actualBytesForGreen = Parser.TranslateForBlink1(request, length);
            Assert.That(actualBytesForGreen.Length, Is.EqualTo(length));
            Assert.That(actualBytesForGreen, Is.EqualTo(expectedBytesForGreen).AsCollection);
        }

        /// <summary>
        /// Tests that a status request for server down translates to switching the RGB LED to blue.
        /// </summary>
        [Test]
        public void TestTranslateForBlink1StatusRequestServerDown()
        {
            short length = 10;
            StatusRequest request = new StatusRequest(false);
            byte[] expectedBytes = { 0x01, 0x63, 0x00, 0x00, 0xFF, 0x00, 0x64, 0x00, 0x00, 0x00 };
            byte[] actualBytes = Parser.TranslateForBlink1(request, length);
            Console.WriteLine(BitConverter.ToString(expectedBytes));
            Assert.That(actualBytes.Length, Is.EqualTo(length));
            Assert.That(actualBytes, Is.EqualTo(expectedBytes).AsCollection);
        }

        /// <summary>
        /// Tests that a status request for server up is not allowed.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidTranslationRequestException))]
        public void TestTranslateForBlink1StatusRequestServerUp()
        {
            StatusRequest request = new StatusRequest(true);
            Parser.TranslateForBlink1(request, 10);
        }

        /// <summary>
        /// Tests that a attention request translates to switching the red LED on and the green LED off.
        /// </summary>
        [Test]
        public void TestTranslateAttentionRequestAttentionNotRequired()
        {
            IRequest request = new AttentionRequest(false);
            string expectedRedCommand = "red=off\n";
            string expectedGreenCommand = "green=on\n";

            byte[] expectedRedBytes = Encoding.ASCII.GetBytes(expectedRedCommand);
            byte[] expectedGreenBytes = Encoding.ASCII.GetBytes(expectedGreenCommand);
            string expectedRedString = Encoding.ASCII.GetString(expectedRedBytes);
            string expectedGreenString = Encoding.ASCII.GetString(expectedGreenBytes);
            int numberOfExpectedBytes = (expectedRedString + expectedGreenString).Length;
            byte[] actualBytes = Parser.TranslateForDasBlinkenlichten(request);
            string actualBytesString = Encoding.ASCII.GetString(actualBytes);

            Assert.That(actualBytesString.Length, Is.EqualTo(numberOfExpectedBytes));
            Assert.That(actualBytesString.Contains(expectedRedString));
            Assert.That(actualBytesString.Contains(expectedGreenString));
        }

        /// <summary>
        /// Tests that a attention request translates to switching the red LED on and the green LED off.
        /// </summary>
        [Test]
        public void TestTranslateAttentionRequestAttentionRequired()
        {
            IRequest request = new AttentionRequest(true);
            string expectedRedCommand = "red=on\n";
            string expectedGreenCommand = "green=off\n";

            byte[] expectedRedBytes = Encoding.ASCII.GetBytes(expectedRedCommand);
            byte[] expectedGreenBytes = Encoding.ASCII.GetBytes(expectedGreenCommand);
            string expectedRedString = Encoding.ASCII.GetString(expectedRedBytes);
            string expectedGreenString = Encoding.ASCII.GetString(expectedGreenBytes);
            int numberOfExpectedBytes = (expectedRedString + expectedGreenString).Length;
            byte[] actualBytes = Parser.TranslateForDasBlinkenlichten(request);
            string actualBytesString = Encoding.ASCII.GetString(actualBytes);

            Assert.That(actualBytesString.Length, Is.EqualTo(numberOfExpectedBytes));
            Assert.That(actualBytesString.Contains(expectedRedString));
            Assert.That(actualBytesString.Contains(expectedGreenString));
        }

        /// <summary>
        /// Tests that a attention request translates to switching the red LED off and the green LED off.
        /// </summary>
        [Test]
        public void TestTranslateAttentionRequestAttentionRequiredNoPriority()
        {
            IRequest request = new AttentionRequest(false, false);
            string expectedRedCommand = "red=off\n";
            string expectedGreenCommand = "green=on\n";

            byte[] expectedRedBytes = Encoding.ASCII.GetBytes(expectedRedCommand);
            byte[] expectedGreenBytes = Encoding.ASCII.GetBytes(expectedGreenCommand);
            string expectedRedString = Encoding.ASCII.GetString(expectedRedBytes);
            string expectedGreenString = Encoding.ASCII.GetString(expectedGreenBytes);
            int numberOfExpectedBytes = (expectedRedString + expectedGreenString).Length;
            byte[] actualBytes = Parser.TranslateForDasBlinkenlichten(request);
            string actualBytesString = Encoding.ASCII.GetString(actualBytes);

            Assert.That(actualBytesString.Length, Is.EqualTo(numberOfExpectedBytes));
            Assert.That(actualBytesString.Contains(expectedRedString));
            Assert.That(actualBytesString.Contains(expectedGreenString));
        }

        /// <summary>
        /// Tests that a attention request translates to switching the red LED to SOS mode and the green LED off.
        /// </summary>
        [Test]
        public void TestTranslateAttentionRequestAttentionRequiredWithPriority()
        {
            IRequest request = new AttentionRequest(true, true);
            string expectedRedCommand = "red=sos\n";
            string expectedGreenCommand = "green=off\n";

            byte[] expectedRedBytes = Encoding.ASCII.GetBytes(expectedRedCommand);
            byte[] expectedGreenBytes = Encoding.ASCII.GetBytes(expectedGreenCommand);
            string expectedRedString = Encoding.ASCII.GetString(expectedRedBytes);
            string expectedGreenString = Encoding.ASCII.GetString(expectedGreenBytes);
            int numberOfExpectedBytes = (expectedRedString + expectedGreenString).Length;
            byte[] actualBytes = Parser.TranslateForDasBlinkenlichten(request);
            string actualBytesString = Encoding.ASCII.GetString(actualBytes);

            Assert.That(actualBytesString.Length, Is.EqualTo(numberOfExpectedBytes));
            Assert.That(actualBytesString.Contains(expectedRedString));
            Assert.That(actualBytesString.Contains(expectedGreenString));
        }

        /// <summary>
        /// Tests that a build active request translates to switching on the yellow LED.
        /// </summary>
        [Test]
        public void TestTranslateBuildActiveRequestIsActive()
        {
            IRequest request = new BuildActiveRequest(true);
            string expectedCommand = "yellow=on\n";
            byte[] expectedBytes = Encoding.ASCII.GetBytes(expectedCommand);
            byte[] actualBytes = Parser.TranslateForDasBlinkenlichten(request);
            Assert.That(actualBytes, Is.EqualTo(expectedBytes));
        }

        /// <summary>
        /// Tests that a build active request translates to switching off the yellow LED.
        /// </summary>
        [Test]
        public void TestTranslateBuildActiveRequestIsInactive()
        {
            IRequest request = new BuildActiveRequest(false);
            string expectedCommand = "yellow=off\n";
            byte[] expectedBytes = Encoding.ASCII.GetBytes(expectedCommand);
            byte[] actualBytes = Parser.TranslateForDasBlinkenlichten(request);
            Assert.That(actualBytes, Is.EqualTo(expectedBytes));
        }

        /// <summary>
        /// Tests that a server down request translates to switching on all LEDs.
        /// </summary>
        [Test]
        public void TestTranslateServerRequestGoingDown()
        {
            IRequest request = new StatusRequest(false);
            string expectedGreenCommand = "green=on\n";
            string expectedRedCommand = "red=on\n";
            string expectedYellowCommand = "yellow=on\n";
            string expectedCommand = expectedGreenCommand + expectedRedCommand + expectedYellowCommand;
            string actualCommand = Encoding.ASCII.GetString(Parser.TranslateForDasBlinkenlichten(request));
            Assert.That(actualCommand.Length, Is.EqualTo(expectedCommand.Length));
            Assert.That(actualCommand.Contains(expectedGreenCommand));
            Assert.That(actualCommand.Contains(expectedRedCommand));
            Assert.That(actualCommand.Contains(expectedYellowCommand));
        }

        /// <summary>
        /// Tests that a server down request translates to switching on all LEDs.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidTranslationRequestException))]
        public void TestTranslateServerRequestIsUp()
        {
            IRequest request = new StatusRequest(true);
            Parser.TranslateForDasBlinkenlichten(request);
            Assert.Fail();
        }

        /// <summary>
        /// Tests the translate throws the correct exception for an unknown request.
        /// </summary>
        [Test]
        [ExpectedException(typeof(InvalidTranslationRequestException))]
        public void TestTranslateThrowsExceptionForUnknownRequest()
        {
            IRequest request = new RegistrationRequest(string.Empty, string.Empty);
            Parser.TranslateForDasBlinkenlichten(request);
            Assert.Fail();
        }

        #endregion
    }
}