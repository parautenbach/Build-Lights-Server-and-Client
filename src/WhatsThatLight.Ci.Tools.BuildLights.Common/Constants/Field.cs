// <copyright file="Field.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common.Constants
{
    /// <summary>
    /// Protocol field names.
    /// </summary>
    public class Field
    {
        #region Constants and Fields

        /// <summary>
        /// The name of the key to indicate an attention request is priority.
        /// </summary>
        public const string AttentionPriority = "priority";

        /// <summary>
        /// The name of the key to indicate a user's attention is required. 
        /// </summary>
        public const string AttentionRequired = "attention";

        /// <summary>
        /// The name of the key that identifies the build config.
        /// </summary>
        public const string BuildConfigId = "buildconfigid";

        /// <summary>
        /// The name of the key that identifies the recipients for build notifications.
        /// </summary>
        public const string BuildRecipients = "recipients";

        /// <summary>
        /// The name of the key that indicates whether any builds are active.
        /// </summary>
        public const string BuildsActive = "buildsactive";

        /// <summary>
        /// The name of the key that identifies the green LED.
        /// </summary>
        public const string GreenLed = "green";

        /// <summary>
        /// The name of the key that identifies the hostname of a client.
        /// </summary>
        public const string Hostname = "hostname";

        /// <summary>
        /// The name of the key that identifies the notification type. 
        /// </summary>
        public const string NotificationTypeId = "notificationtypeid";

        /// <summary>
        /// The name of the key that identifies the project.
        /// </summary>
        public const string ProjectId = "projectid";

        /// <summary>
        /// The name of the key that identifies the red LED.
        /// </summary>
        public const string RedLed = "red";

        /// <summary>
        /// The name of the key that identifies the request type. 
        /// </summary>
        public const string RequestTypeId = "requesttypeid";

        /// <summary>
        /// The name of the key that identifies the responsibility state.
        /// </summary>
        public const string ResponsibilityState = "state";

        /// <summary>
        /// The name of the key that identifies the username that's responsible for the build. 
        /// </summary>
        public const string ResponsibleUsername = "username";

        /// <summary>
        /// The name of the key to indicate the server's status. 
        /// </summary>
        public const string ServerStatus = "status";

        /// <summary>
        /// The name of the key that identifies a user by their username.
        /// </summary>
        public const string Username = "username";

        /// <summary>
        /// The name of the key that identifies the yellow LED.
        /// </summary>
        public const string YellowLed = "yellow";

        #endregion
    }
}