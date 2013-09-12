// <copyright file="Parser.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common.Protocol
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    using WhatsThatLight.Ci.Tools.BuildLights.Common.Constants;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Exceptions;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Notifications;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Requests;

    /// <summary>
    /// For parsing packets. 
    /// </summary>
    public class Parser
    {
        #region Constants and Fields

        /// <summary>
        /// The fade delay for a blink(1) to change colour.
        /// </summary>
        private const ulong FadeMilliseconds = 1000;

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses the notification.
        /// </summary>
        /// <param name="commandDictionary">The command dictionary.</param>
        /// <returns>
        /// A notification
        /// </returns>
        /// <exception cref="InvalidBuildServerNotificationException">When the command could not be parsed as a notification.</exception>
        public static IBuildServerNotification DecodeBuildServerNotification(Dictionary<string, string> commandDictionary)
        {
            BuildServerNotificationType notificationType = DecodeBuildServerNotificationType(commandDictionary);

            // Any of the build events
            if (IsBuildNotification(notificationType))
            {
                try
                {
                    string[] recipients = commandDictionary[Field.BuildRecipients].Split(Packet.ListSeparator);
                    return new BuildNotification(notificationType, commandDictionary[Field.ProjectId], commandDictionary[Field.BuildConfigId], recipients);
                }
                catch (KeyNotFoundException e)
                {
                    throw new InvalidBuildServerNotificationException(string.Format("The notification command could not be decoded as a build notification, as one of the keys could not be found: {0}", e.Message));
                }
            }

            // Any of the responsibility events
            if (IsResponsibilityNotification(notificationType))
            {
                if (!VerifyResponsibilityState(commandDictionary[Field.ResponsibilityState]))
                {
                    throw new InvalidBuildServerResponsibilityStateException(string.Format("The responsibility state {0} is not a valid state", commandDictionary[Field.ResponsibilityState]));
                }

                try
                {
                    return new ResponsibilityNotification(notificationType, commandDictionary[Field.ProjectId], commandDictionary[Field.BuildConfigId], commandDictionary[Field.ResponsibleUsername], commandDictionary[Field.ResponsibilityState]);
                }
                catch (KeyNotFoundException e)
                {
                    throw new InvalidBuildServerNotificationException(string.Format("The notification command could not be decoded as a responsibility notification, as one of the keys could not be found: {0}", e.Message));
                }
            }

            throw new InvalidBuildServerNotificationException(string.Format("The notification command of type {0} could not be decoded", notificationType));
        }

        /// <summary>
        /// Parses the command.
        /// </summary>
        /// <param name="commandDictionary">The command.</param>
        /// <returns>The command's notification type.</returns>
        public static BuildServerNotificationType DecodeBuildServerNotificationType(Dictionary<string, string> commandDictionary)
        {
            BuildServerNotificationType notificationType;
            if (Enum.TryParse(commandDictionary[Field.NotificationTypeId], false, out notificationType) && Enum.IsDefined(typeof(BuildServerNotificationType), notificationType))
            {
                return notificationType;
            }

            throw new InvalidBuildServerNotificationTypeException(string.Format("The value {0} is not a valid notification type", commandDictionary[Field.NotificationTypeId]));
        }

        /// <summary>
        /// Parses the command.
        /// </summary>
        /// <param name="commandString">The command string.</param>
        /// <returns>A dictionary with key value pairs.</returns>
        public static Dictionary<string, string> DecodeCommand(string commandString)
        {
            if (!IsCommandTerminatedProperly(commandString))
            {
                throw new InvalidCommandException("The command is not terminated properly");
            }

            Dictionary<string, string> commandDictionary = new Dictionary<string, string>();
            foreach (var commandPairs in commandString.TrimEnd(Packet.PacketTerminator).Split(Packet.CommandSeparator).Select(commandPart => commandPart.Split(Packet.FieldSeparator)))
            {
                if (commandPairs.Length != 2)
                {
                    throw new InvalidCommandException("No valid key-value pairs found");
                }

                commandDictionary.Add(commandPairs[0], commandPairs[1]);
            }

            return commandDictionary;
        }

        /// <summary>
        /// Decodes the request.
        /// </summary>
        /// <param name="commandDictionary">The command dictionary.</param>
        /// <returns>A request</returns>
        public static IRequest DecodeRequest(Dictionary<string, string> commandDictionary)
        {
            RequestType requestType = DecodeRequestType(commandDictionary);
            try
            {
                if (IsRegistrationRequest(requestType))
                {
                    return new RegistrationRequest(commandDictionary[Field.Hostname], commandDictionary[Field.Username]);
                }

                if (IsBuildActiveRequest(requestType))
                {
                    return new BuildActiveRequest(Convert.ToBoolean(Convert.ToInt16(commandDictionary[Field.BuildsActive])));
                }

                if (IsAttentionRequest(requestType))
                {
                    return new AttentionRequest(Convert.ToBoolean(Convert.ToInt16(commandDictionary[Field.AttentionRequired])), Convert.ToBoolean(Convert.ToInt16(commandDictionary[Field.AttentionPriority])));
                }

                if (IsStatusRequest(requestType))
                {
                    return new StatusRequest(Convert.ToBoolean(Convert.ToInt16(commandDictionary[Field.ServerStatus])));
                }
            }
            catch (KeyNotFoundException e)
            {
                throw new InvalidRequestException(string.Format("The request command of type {0} could not be decoded as one of the required keys are not present: {1}", requestType, e.Message));
            }

            throw new InvalidRequestException(string.Format("The request command of type {0} could not be decoded", requestType));
        }

        /// <summary>
        /// Decodes the type of the request.
        /// </summary>
        /// <param name="commandDictionary">The command dictionary.</param>
        /// <returns>The request's type</returns>
        public static RequestType DecodeRequestType(Dictionary<string, string> commandDictionary)
        {
            RequestType requestType;
            if (Enum.TryParse(commandDictionary[Field.RequestTypeId], false, out requestType) && Enum.IsDefined(typeof(RequestType), requestType))
            {
                return requestType;
            }

            throw new InvalidRequestTypeException(string.Format("The value {0} is not a valid request type", commandDictionary[Field.RequestTypeId]));
        }

        /// <summary>
        /// Encodes the specified build notification.
        /// </summary>
        /// <param name="notification">The build notification.</param>
        /// <returns>The encoded notification</returns>
        public static string Encode(IBuildServerNotification notification)
        {
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

            // Common encoding
            string notificationTypeString = ((int)notification.Type).ToString(CultureInfo.InvariantCulture);
            list.Add(CreatePair(Field.NotificationTypeId, notificationTypeString));
            list.Add(CreatePair(Field.ProjectId, notification.ProjectId));
            list.Add(CreatePair(Field.BuildConfigId, notification.BuildConfigId));

            // Notification-specific encoding
            if (notification.GetType() == typeof(BuildNotification))
            {
                BuildNotification buildNotification = (BuildNotification)notification;
                string recipientsList = string.Join(Packet.ListSeparator.ToString(CultureInfo.InvariantCulture), buildNotification.Recipients);
                list.Add(CreatePair(Field.BuildRecipients, recipientsList));
            }
            else if (notification.GetType() == typeof(ResponsibilityNotification))
            {
                ResponsibilityNotification responsibilityNotification = (ResponsibilityNotification)notification;
                list.Add(CreatePair(Field.ResponsibleUsername, responsibilityNotification.Recipient));
                list.Add(CreatePair(Field.ResponsibilityState, responsibilityNotification.State));
            }

            return AssembleCommand(list);
        }

        /// <summary>
        /// Encodes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The encoded request</returns>
        public static string Encode(IRequest request)
        {
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            string requestTypeString = ((int)request.Type).ToString(CultureInfo.InvariantCulture);
            list.Add(CreatePair(Field.RequestTypeId, requestTypeString));
            if (request.GetType() == typeof(RegistrationRequest))
            {
                RegistrationRequest registrationRequest = (RegistrationRequest)request;
                list.Add(CreatePair(Field.Hostname, registrationRequest.Hostname));
                list.Add(CreatePair(Field.Username, registrationRequest.Username));
            }
            else if (request.GetType() == typeof(BuildActiveRequest))
            {
                BuildActiveRequest buildActiveRequest = (BuildActiveRequest)request;
                list.Add(CreatePair(Field.BuildsActive, Convert.ToInt16(buildActiveRequest.IsBuildsActive).ToString(CultureInfo.InvariantCulture)));
            }
            else if (request.GetType() == typeof(AttentionRequest))
            {
                AttentionRequest attentionRequest = (AttentionRequest)request;
                list.Add(CreatePair(Field.AttentionRequired, Convert.ToInt16(attentionRequest.IsAttentionRequired).ToString(CultureInfo.InvariantCulture)));
                list.Add(CreatePair(Field.AttentionPriority, Convert.ToInt16(attentionRequest.IsPriority).ToString(CultureInfo.InvariantCulture)));
            }
            else if (request.GetType() == typeof(StatusRequest))
            {
                StatusRequest statusRequest = (StatusRequest)request;
                list.Add(CreatePair(Field.ServerStatus, Convert.ToInt16(statusRequest.Status).ToString(CultureInfo.InvariantCulture)));
            }

            return AssembleCommand(list);
        }

        /// <summary>
        /// Determines whether this is an attention request given the specified request type.
        /// </summary>
        /// <param name="requestType">Type of the request.</param>
        /// <returns>
        ///   <c>true</c> if this is an attention reques; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAttentionRequest(RequestType requestType)
        {
            return requestType == RequestType.Attention;
        }

        /// <summary>
        /// Determines whether the request is a build active request for the specified request type.
        /// </summary>
        /// <param name="requestType">Type of the request.</param>
        /// <returns>
        ///   <c>true</c> if the request is a build active request; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBuildActiveRequest(RequestType requestType)
        {
            return requestType == RequestType.BuildActive;
        }

        /// <summary>
        /// Determines whether the specified notification type is a build notification.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        /// <returns>
        ///   <c>true</c> if the specified notification type is a build notification; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBuildNotification(BuildServerNotificationType notificationType)
        {
            return notificationType == BuildServerNotificationType.BuildBuilding || notificationType == BuildServerNotificationType.BuildFailed || notificationType == BuildServerNotificationType.BuildFailedToStart || notificationType == BuildServerNotificationType.BuildHanging || notificationType == BuildServerNotificationType.BuildSuccessful || notificationType == BuildServerNotificationType.BuildFailing;
        }

        /// <summary>
        /// Determines whether the specified command dictionary is a notification.
        /// </summary>
        /// <param name="commandDictionary">The command dictionary.</param>
        /// <returns>
        ///   <c>true</c> if the specified command dictionary is a notification; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBuildServerNotification(Dictionary<string, string> commandDictionary)
        {
            return commandDictionary.ContainsKey(Field.NotificationTypeId);
        }

        /// <summary>
        /// Determines whether the command is terminated properly given the specified command string.
        /// </summary>
        /// <param name="commandString">The command string.</param>
        /// <returns>
        ///   <c>true</c> if the command is terminated properly; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsCommandTerminatedProperly(string commandString)
        {
            return commandString.EndsWith(Packet.PacketTerminator.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Determines whether the request is a registration request.
        /// </summary>
        /// <param name="requestType">Type of the request.</param>
        /// <returns>
        ///   <c>true</c> if the request is a registration request; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRegistrationRequest(RequestType requestType)
        {
            return requestType == RequestType.Register;
        }

        /// <summary>
        /// Determines whether the specified command dictionary is a request.
        /// </summary>
        /// <param name="commandDictionary">The command dictionary.</param>
        /// <returns>
        ///   <c>true</c> if the specified command dictionary is a request; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRequest(Dictionary<string, string> commandDictionary)
        {
            return commandDictionary.ContainsKey(Field.RequestTypeId);
        }

        /// <summary>
        /// Determines whether the specified notification type is a responsibility notification.
        /// </summary>
        /// <param name="notificationType">Type of the notification.</param>
        /// <returns>
        ///   <c>true</c> if the specified notification type is a responsibility notification; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsResponsibilityNotification(BuildServerNotificationType notificationType)
        {
            return notificationType == BuildServerNotificationType.BuildResponsibilityAssigned || notificationType == BuildServerNotificationType.TestResponsibilityAssigned;
        }

        /// <summary>
        /// Translates a for blink1.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="featureReportByteLength">Length of the feature report byte.</param>
        /// <returns>
        /// An array of byte arrays (commands).
        /// </returns>
        public static byte[] TranslateForBlink1(BuildActiveRequest request, short featureReportByteLength)
        {
            if (request.IsBuildsActive)
            {
                // Yellow
                return PackBlink1FadeToColorBytes(255, 210, 0, FadeMilliseconds, featureReportByteLength);
            }

            throw new InvalidTranslationRequestException(string.Format("The request type is not one that can be translated: {0}", request.GetType()));
        }

        /// <summary>
        /// Translates a for blink1.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="featureReportByteLength">Length of the feature report byte.</param>
        /// <returns>
        /// An array of byte arrays (commands).
        /// </returns>
        public static byte[] TranslateForBlink1(AttentionRequest request, short featureReportByteLength)
        {
            if (request.IsAttentionRequired)
            {
                // Red
                // TODO: Figure out a way to handle priority requests
                return PackBlink1FadeToColorBytes(255, 0, 0, FadeMilliseconds, featureReportByteLength);
            }

            // Green
            return PackBlink1FadeToColorBytes(0, 255, 0, FadeMilliseconds, featureReportByteLength);
        }

        /// <summary>
        /// Translates a for blink1.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="featureReportByteLength">Length of the feature report byte.</param>
        /// <returns>
        /// An array of byte arrays (commands).
        /// </returns>
        public static byte[] TranslateForBlink1(StatusRequest request, short featureReportByteLength)
        {
            if (!request.Status)
            {
                // Blue
                return PackBlink1FadeToColorBytes(0, 0, 255, FadeMilliseconds, featureReportByteLength);
            }

            throw new InvalidTranslationRequestException(string.Format("The request type is not one that can be translated: {0}", request.GetType()));
        }

        /// <summary>
        /// Translates the specified request to a USB byte command (DasBlinkenlichten protocol).
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>An array of byte arrays (commands).</returns>
        public static byte[] TranslateForDasBlinkenlichten(IRequest request)
        {
            if (request.GetType() == typeof(BuildActiveRequest))
            {
                BuildActiveRequest buildActiveRequest = (BuildActiveRequest)request;
                List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>> {
                                                                                                         buildActiveRequest.IsBuildsActive ? CreatePair(Field.YellowLed, LedState.On) : CreatePair(Field.YellowLed, LedState.Off)
                                                                                                 };

                return Encoding.ASCII.GetBytes(AssembleCommand(list, Packet.PacketAltTerminator));
            }

            if (request.GetType() == typeof(AttentionRequest) || request.GetType() == typeof(StatusRequest))
            {
                // We need to construct two commands, since the USB device only accepts one
                // light command in a packet and the red and green lights works in a pair.
                // But, nothing stops us from sending two packets in one go... 
                List<KeyValuePair<string, string>> listRed = new List<KeyValuePair<string, string>>();
                List<KeyValuePair<string, string>> listGreen = new List<KeyValuePair<string, string>>();
                List<KeyValuePair<string, string>> listYellow = new List<KeyValuePair<string, string>>();
                if (request.GetType() == typeof(AttentionRequest) && ((AttentionRequest)request).IsAttentionRequired)
                {
                    AttentionRequest attentionRequest = (AttentionRequest)request;
                    listRed.Add(attentionRequest.IsPriority ? CreatePair(Field.RedLed, LedState.Sos) : CreatePair(Field.RedLed, LedState.On));
                    listGreen.Add(CreatePair(Field.GreenLed, LedState.Off));
                }
                else if (request.GetType() == typeof(AttentionRequest) && !((AttentionRequest)request).IsAttentionRequired)
                {
                    listRed.Add(CreatePair(Field.RedLed, LedState.Off));
                    listGreen.Add(CreatePair(Field.GreenLed, LedState.On));
                }
                else if (request.GetType() == typeof(StatusRequest) && !((StatusRequest)request).Status)
                {
                    listRed.Add(CreatePair(Field.RedLed, LedState.On));
                    listGreen.Add(CreatePair(Field.GreenLed, LedState.On));
                    listYellow.Add(CreatePair(Field.YellowLed, LedState.On));
                }
                else if (request.GetType() == typeof(StatusRequest) && ((StatusRequest)request).Status)
                {
                    throw new InvalidTranslationRequestException("Cannot translate a server up request; only a down request");
                }

                StringBuilder commandBuilder = new StringBuilder();
                commandBuilder.Append(AssembleCommand(listRed, Packet.PacketAltTerminator));
                commandBuilder.Append(AssembleCommand(listGreen, Packet.PacketAltTerminator));
                if (listYellow.Count > 0)
                {
                    commandBuilder.Append(AssembleCommand(listYellow, Packet.PacketAltTerminator));
                }

                return Encoding.ASCII.GetBytes(commandBuilder.ToString());
            }

            throw new InvalidTranslationRequestException(string.Format("The request type is not one that can be translated: {0}", request.GetType()));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Assembles the command in the order of the key-value pairs received.
        /// </summary>
        /// <param name="commandList">The command list.</param>
        /// <param name="packetTerminator">The packet terminator. Client-to-server communications uses <see cref="Packet.PacketTerminator"/> whereas the client-to-USB communications uses <see cref="Packet.PacketAltTerminator"/>. </param>
        /// <returns>
        /// The assembled command
        /// </returns>
        private static string AssembleCommand(IEnumerable<KeyValuePair<string, string>> commandList, char packetTerminator = Packet.PacketTerminator)
        {
            KeyValuePair<string, string>[] arrayList = commandList.ToArray();
            StringBuilder commandBuilder = new StringBuilder();
            for (int i = 0; i < arrayList.Length; i++)
            {
                commandBuilder.Append(arrayList[i].Key);
                commandBuilder.Append(Packet.FieldSeparator);
                commandBuilder.Append(arrayList[i].Value);

                // If it's the last item in the list, we must add the terminator and not a separator
                if (i < arrayList.Length - 1)
                {
                    commandBuilder.Append(Packet.CommandSeparator);
                }
            }

            commandBuilder.Append(packetTerminator);
            return commandBuilder.ToString();
        }

        /// <summary>
        /// Creates a new key-value pair -- purely for code readability.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A new key-value pair with the given field and value.</returns>
        private static KeyValuePair<string, string> CreatePair(string field, string value)
        {
            return new KeyValuePair<string, string>(field, value);
        }

        /// <summary>
        /// Determines whether this is a status request.
        /// </summary>
        /// <param name="requestType">Type of the request.</param>
        /// <returns>
        ///   <c>true</c> if this is a status request; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsStatusRequest(RequestType requestType)
        {
            return requestType == RequestType.ServerStatus;
        }

        /// <summary>
        /// Packs the blink1 bytes in to an array.
        /// </summary>
        /// <param name="red">The red color component.</param>
        /// <param name="green">The green color component.</param>
        /// <param name="blue">The blue color component.</param>
        /// <param name="fadeMilliseconds">The milliseconds to cross-fade.</param>
        /// <param name="featureReportByteLength">Length of the feature report byte.</param>
        /// <returns>
        /// An array of byte arrays (commands).
        /// </returns>
        private static byte[] PackBlink1FadeToColorBytes(ushort red, ushort green, ushort blue, ulong fadeMilliseconds, short featureReportByteLength)
        {
            byte[] buffer = new byte[featureReportByteLength];
            buffer[0] = Convert.ToByte(1);
            buffer[1] = Convert.ToByte('c');
            buffer[2] = Convert.ToByte(Utils.AdjustBlink1GammaLevel(red));
            buffer[3] = Convert.ToByte(Utils.AdjustBlink1GammaLevel(green));
            buffer[4] = Convert.ToByte(Utils.AdjustBlink1GammaLevel(blue));
            buffer[5] = Convert.ToByte((fadeMilliseconds / 10) >> 8);
            buffer[6] = Convert.ToByte((fadeMilliseconds / 10) % 0xff);
            return buffer;
        }

        /// <summary>
        /// Verifies the state of the responsibility.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns><c>true</c> if the state of the responsibility is valid; otherwise, <c>false</c>.</returns>
        private static bool VerifyResponsibilityState(string state)
        {
            return state == BuildServerResponsibilityState.None || state == BuildServerResponsibilityState.Taken || state == BuildServerResponsibilityState.Fixed || state == BuildServerResponsibilityState.GivenUp;
        }

        #endregion
    }
}