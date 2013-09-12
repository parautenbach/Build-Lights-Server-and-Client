// <copyright file="ConfigKey.cs" company="What's That Light?">
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
    /// Default configuration values. 
    /// </summary>
    public class ConfigKey
    {
        #region Constants and Fields

        /// <summary>
        /// The password for the build server's username. 
        /// </summary>
        public const string BuildServerPassword = "buildServerPassword";

        /// <summary>
        /// The build server's location. 
        /// </summary>
        public const string BuildServerUrl = "buildServerUrl";

        /// <summary>
        /// The build server's username. 
        /// </summary>
        public const string BuildServerUsername = "buildServerUsername";

        /// <summary>
        /// Gets the hostname of the computer that the lights device is connected to. This is 
        /// an overriding configuration if the hostname can't be detected or resolved by the
        /// notification manager when connecting back to push updates.
        /// </summary>
        public const string Hostname = "hostname";

        /// <summary>
        /// Whether the manager must initialize. 
        /// </summary>
        public const string InitializationEnabled = "initializationEnabled";

        /// <summary>
        /// The port on which a lights manager's listener must listen. 
        /// </summary>
        public const string LightsManagerPort = "lightsManagerPort";

        /// <summary>
        /// The hostname of the notification server. 
        /// </summary>
        public const string NotificationManagerHost = "notificationManagerHost";

        /// <summary>
        /// The port on which a notification manager's listener must listen. 
        /// </summary>
        public const string NotificationManagerPort = "notificationManagerPort";

        /// <summary>
        /// The period after which a user's attention will become priority.
        /// </summary>
        public const string PriorityPeriodHours = "priorityPeriodHours";

        /// <summary>
        /// The expiry period for the priority timer. 
        /// </summary>
        public const string PriorityTimerPeriodMillis = "priorityTimerPeriodMillis";

        /// <summary>
        /// The retry period for the lights manager to retry to connect to the notification manager. 
        /// </summary>
        public const string RegistrationRetryPeriodMillis = "registrationRetryPeriodMillis";

        /// <summary>
        /// The control transfer type for USB transfers.
        /// </summary>
        public const string UsbControlTransferType = "usbControlTransferType";

        /// <summary>
        /// The USB lights device's PID. 
        /// </summary>
        public const string UsbProductId = "usbProductId";

        /// <summary>
        /// The USB protocol to communicate to the device.
        /// </summary>
        public const string UsbProtocolType = "usbProtocolType";

        /// <summary>
        /// The USB lights device's usage.
        /// </summary>
        public const string UsbUsage = "usbUsage";

        /// <summary>
        /// The USB lights device's usage page.
        /// </summary>
        public const string UsbUsagePage = "usbUsagePage";

        /// <summary>
        /// The USB lights device's VID. 
        /// </summary>
        public const string UsbVendorId = "usbVendorId";

        /// <summary>
        /// The username of the user that uses the lights device. This is an overriding 
        /// configuration if the username can't be detected or the detected user differs 
        /// from the user using the build server or code repository. 
        /// </summary>
        public const string Username = "username";

        /// <summary>
        /// The VCS server's password.
        /// </summary>
        public const string VcsServerPassword = "vcsServerPassword";

        /// <summary>
        /// The VCS server's location.
        /// </summary>
        public const string VcsServerUrl = "vcsServerUrl";

        /// <summary>
        /// The VCS server's username.
        /// </summary>
        public const string VcsServerUsername = "vcsServerUsername";

        /// <summary>
        /// The period to wait between retries of enumerating for the USB device. 
        /// </summary>
        public const string WaitForDeviceRetryPeriodMillis = "waitForDeviceRetryPeriodMillis";

        #endregion
    }
}