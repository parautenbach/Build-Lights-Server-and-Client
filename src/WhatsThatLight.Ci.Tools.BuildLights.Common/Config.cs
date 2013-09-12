// <copyright file="Config.cs" company="What's That Light?">
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
    using System.ComponentModel;
    using System.Configuration;
    using System.Globalization;

    using WhatsThatLight.Ci.Tools.BuildLights.Common.Constants;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;

    /// <summary>
    /// A common settings class. 
    /// </summary>
    public class Config
    {
        #region Constants and Fields

        /// <summary>
        /// Default build server password.
        /// </summary>
        private static readonly string defaultBuildServerPassword;

        /// <summary>
        /// Default build server URL.
        /// </summary>
        private static readonly string defaultBuildServerUrl;

        /// <summary>
        /// Default build server username.
        /// </summary>
        private static readonly string defaultBuildServerUsername;

        /// <summary>
        /// The default hostname. This default value will cause <see cref="Utils.GetHostname"/>
        /// to auto detect the hostname.
        /// </summary>
        private static readonly string defaultHostname;

        /// <summary>
        /// Default initialization option. 
        /// </summary>
        private static readonly bool defaultInitializationEnabled;

        /// <summary>
        /// Default lights manager port.
        /// </summary>
        private static readonly int defaultLightsManagerPort;

        /// <summary>
        /// Default notification manager host. 
        /// </summary>
        private static readonly string defaultNotificationManagerHost;

        /// <summary>
        /// Default notification manager port.
        /// </summary>
        private static readonly int defaultNotificationManagerPort;

        /// <summary>
        /// Default priority period in hours. 
        /// </summary>
        private static readonly int defaultPriorityPeriodHours;

        /// <summary>
        /// Default priority timer period in ms. 
        /// </summary>
        private static readonly int defaultPriorityTimerPeriodMillis;

        /// <summary>
        /// Default retry period for the lights manager to retry to connect to the notification manager. 
        /// </summary>
        private static readonly int defaultRegistrationRetryPeriodMillis;

        /// <summary>
        /// Default USB control transfer type.
        /// </summary>
        private static readonly UsbControlTransferType defaultUsbControlTransferType;

        /// <summary>
        /// Default USB product ID. 
        /// </summary>
        private static readonly ushort defaultUsbProductId;

        /// <summary>
        /// Default USB protocol to use.
        /// </summary>
        private static readonly UsbProtocolType defaultUsbProtocolType;

        /// <summary>
        /// Default USB usage. 
        /// </summary>
        private static readonly ushort defaultUsbUsage;

        /// <summary>
        /// Default USB usage page. 
        /// </summary>
        private static readonly ushort defaultUsbUsagePage;

        /// <summary>
        /// Default USB vendor ID. 
        /// </summary>
        private static readonly ushort defaultUsbVendorId;

        /// <summary>
        /// The default username. This default value will cause <see cref="Utils.GetUsername"/>
        /// to auto detect the logged in user.
        /// </summary>
        private static readonly string defaultUsername;

        /// <summary>
        /// Default VCS server password. 
        /// </summary>
        private static readonly string defaultVcsServerPassword;

        /// <summary>
        /// Default VCS server URL. 
        /// </summary>
        private static readonly string defaultVcsServerUrl;

        /// <summary>
        /// Default VCS server username. 
        /// </summary>
        private static readonly string defaultVcsServerUsername;

        /// <summary>
        /// Default retry period when enumerating for the USB device.  
        /// </summary>
        private static readonly int defaultWaitForDeviceRetryPeriodMillis;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes static members of the <see cref="Config"/> class.
        /// </summary>
        static Config()
        {
            // Build server
            defaultBuildServerUsername = string.Empty;
            defaultBuildServerPassword = string.Empty;
            defaultBuildServerUrl = "localhost";

            // Own settings
            defaultUsername = string.Empty;
            defaultHostname = string.Empty;
            defaultInitializationEnabled = false;
            defaultLightsManagerPort = 9192;
            defaultNotificationManagerHost = "localhost";
            defaultNotificationManagerPort = 9191;
            defaultRegistrationRetryPeriodMillis = 5000;

            // Priority settings
            defaultPriorityPeriodHours = 24;
            defaultPriorityTimerPeriodMillis = 60000;

            // USB device settings
            defaultUsbProductId = 0x0486;
            defaultUsbUsage = 0x0004;
            defaultUsbUsagePage = 0xFFC9;
            defaultUsbVendorId = 0x16C0;
            defaultWaitForDeviceRetryPeriodMillis = 1000;
            defaultUsbControlTransferType = UsbControlTransferType.FeatureReport;
            defaultUsbProtocolType = UsbProtocolType.Blink1;

            // VCS settings
            defaultVcsServerPassword = string.Empty;
            defaultVcsServerUrl = "localhost";
            defaultVcsServerUsername = string.Empty;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the build server's password.
        /// </summary>
        /// <returns>The build server's password</returns>
        public static string GetBuildServerPassword()
        {
            return GetConfigValue(ConfigKey.BuildServerPassword, defaultBuildServerPassword);
        }

        /// <summary>
        /// Gets the build server's URL.
        /// </summary>
        /// <returns>The build server's URL.</returns>
        public static string GetBuildServerUrl()
        {
            return GetConfigValue(ConfigKey.BuildServerUrl, defaultBuildServerUrl);
        }

        /// <summary>
        /// Gets the build server's username.
        /// </summary>
        /// <returns>The build server's username.</returns>
        public static string GetBuildServerUsername()
        {
            return GetConfigValue(ConfigKey.BuildServerUsername, defaultBuildServerUsername);
        }

        /// <summary>
        /// Gets the hostname of the computer that the lights device is connected to. This is 
        /// an overriding configuration if the hostname can't be detected or resolved by the
        /// notification manager when connecting back to push updates.
        /// </summary>
        /// <returns>The hostname.</returns>
        public static string GetHostname()
        {
            return GetConfigValue(ConfigKey.Hostname, defaultHostname);
        }

        /// <summary>
        /// Gets the initialization enabled option.
        /// </summary>
        /// <returns><c>true</c> if initialization is enabled.</returns>
        public static bool GetInitializationEnabled()
        {
            return GetConfigValue(ConfigKey.InitializationEnabled, defaultInitializationEnabled);
        }

        /// <summary>
        /// Gets the lights manager port.
        /// </summary>
        /// <returns>The lights manager port.</returns>
        public static int GetLightsManagerPort()
        {
            return GetConfigValue(ConfigKey.LightsManagerPort, defaultLightsManagerPort);
        }

        /// <summary>
        /// Gets the notification manager host.
        /// </summary>
        /// <returns>The notification manager host.</returns>
        public static string GetNotificationManagerHost()
        {
            return GetConfigValue(ConfigKey.NotificationManagerHost, defaultNotificationManagerHost);
        }

        /// <summary>
        /// Gets the notification manager port.
        /// </summary>
        /// <returns>The notification manager port.</returns>
        public static int GetNotificationManagerPort()
        {
            return GetConfigValue(ConfigKey.NotificationManagerPort, defaultNotificationManagerPort);
        }

        /// <summary>
        /// Gets the priority period. Config value must be specified in hours. 
        /// </summary>
        /// <returns>The priority period.</returns>
        public static TimeSpan GetPriorityPeriod()
        {
            return new TimeSpan(GetConfigValue(ConfigKey.PriorityPeriodHours, defaultPriorityPeriodHours), 0, 0);
        }

        /// <summary>
        /// Gets the priority timer period (in ms).
        /// </summary>
        /// <returns>The priority timer period (in ms).</returns>
        public static int GetPriorityTimerPeriod()
        {
            return GetConfigValue(ConfigKey.PriorityTimerPeriodMillis, defaultPriorityTimerPeriodMillis);
        }

        /// <summary>
        /// Gets the registration retry period.
        /// </summary>
        /// <returns>The registration retry period.</returns>
        public static int GetRegistrationRetryPeriod()
        {
            return GetConfigValue(ConfigKey.RegistrationRetryPeriodMillis, defaultRegistrationRetryPeriodMillis);
        }

        /// <summary>
        /// Gets the type of the USB control transfer (i.e. whether it's raw, feature reports or some other mechanism).
        /// </summary>
        /// <returns>The type of the USB control transfer.</returns>
        public static UsbControlTransferType GetUsbControlTransferType()
        {
            return GetConfigValue(ConfigKey.UsbControlTransferType, defaultUsbControlTransferType);
        }

        /// <summary>
        /// Gets the USB product ID.
        /// </summary>
        /// <returns>The USB product ID.</returns>
        public static ushort GetUsbProductId()
        {
            return GetConfigValue(ConfigKey.UsbProductId, defaultUsbProductId);
        }

        /// <summary>
        /// Gets the type of the USB protocol to communicate to the device.
        /// </summary>
        /// <returns>The USB protocol type</returns>
        public static UsbProtocolType GetUsbProtocolType()
        {
            return GetConfigValue(ConfigKey.UsbProtocolType, defaultUsbProtocolType);
        }

        /// <summary>
        /// Gets the USB usage.
        /// </summary>
        /// <returns>The USB usage.</returns>
        public static ushort GetUsbUsage()
        {
            return GetConfigValue(ConfigKey.UsbUsage, defaultUsbUsage);
        }

        /// <summary>
        /// Gets the USB usage page.
        /// </summary>
        /// <returns>The USB usage page.</returns>
        public static ushort GetUsbUsagePage()
        {
            return GetConfigValue(ConfigKey.UsbUsagePage, defaultUsbUsagePage);
        }

        /// <summary>
        /// Gets the USB vendor ID.
        /// </summary>
        /// <returns>The USB vendor ID.</returns>
        public static ushort GetUsbVendorId()
        {
            return GetConfigValue(ConfigKey.UsbVendorId, defaultUsbVendorId);
        }

        /// <summary>
        /// Gets the username of the user that uses the lights device. This is an overriding
        /// configuration if the username can't be detected or the detected user differs
        /// from the user using the build server or code repository.
        /// </summary>
        /// <returns>The username.</returns>
        public static string GetUsername()
        {
            return GetConfigValue(ConfigKey.Username, defaultUsername);
        }

        /// <summary>
        /// Gets the VCS server password.
        /// </summary>
        /// <returns>The VCS server password.</returns>
        public static string GetVcsServerPassword()
        {
            return GetConfigValue(ConfigKey.VcsServerPassword, defaultVcsServerPassword);
        }

        /// <summary>
        /// Gets the VCS server URL.
        /// </summary>
        /// <returns>The VCS server URL.</returns>
        public static string GetVcsServerUrl()
        {
            return GetConfigValue(ConfigKey.VcsServerUrl, defaultVcsServerUrl);
        }

        /// <summary>
        /// Gets the VCS server username.
        /// </summary>
        /// <returns>The VCS server username.</returns>
        public static string GetVcsServerUsername()
        {
            return GetConfigValue(ConfigKey.VcsServerUsername, defaultVcsServerUsername);
        }

        /// <summary>
        /// Gets the wait for device retry period.
        /// </summary>
        /// <returns>The wait for device retry period in ms.</returns>
        public static int GetWaitForDeviceRetryPeriod()
        {
            return GetConfigValue(ConfigKey.WaitForDeviceRetryPeriodMillis, defaultWaitForDeviceRetryPeriodMillis);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the config value.
        /// </summary>
        /// <param name="configKey">The config key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>A config value.</returns>
        private static string GetConfigValue(string configKey, string defaultValue)
        {
            string value = ConfigurationManager.AppSettings[configKey];
            return !string.IsNullOrWhiteSpace(value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets the config value.
        /// </summary>
        /// <param name="configKey">The config key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>A config value.</returns>
        private static int GetConfigValue(string configKey, int defaultValue)
        {
            string rawValue = ConfigurationManager.AppSettings[configKey];
            int value;
            if (!string.IsNullOrWhiteSpace(rawValue) && int.TryParse(rawValue, out value))
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets the config value.
        /// </summary>
        /// <param name="configKey">The config key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>A config value.</returns>
        private static ushort GetConfigValue(string configKey, ushort defaultValue)
        {
            string rawValue = ConfigurationManager.AppSettings[configKey];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultValue;
            }

            rawValue = rawValue.StartsWith("0x") ? rawValue.Substring(2, rawValue.Length - 2) : rawValue;
            ushort value;
            return ushort.TryParse(rawValue, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets the config value.
        /// </summary>
        /// <typeparam name="T">A type that essentially has a Parse or TryParse method.</typeparam>
        /// <param name="configKey">The config key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>
        /// A config value.
        /// </returns>
        private static T GetConfigValue<T>(string configKey, T defaultValue)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
            string rawValue = ConfigurationManager.AppSettings[configKey];
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultValue;
            }

            try
            {
                return (T)converter.ConvertFromString(rawValue);
            }
            catch (NotSupportedException)
            {
                return defaultValue;
            }
        }

        #endregion
    }
}