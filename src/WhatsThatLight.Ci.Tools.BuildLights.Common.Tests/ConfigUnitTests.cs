// <copyright file="ConfigUnitTests.cs" company="What's That Light?">
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
    using System.Configuration;
    using System.Globalization;

    using WhatsThatLight.Ci.Tools.BuildLights.Common.Constants;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;

    using NUnit.Framework;

    /// <summary>
    /// Config unit tests.
    /// </summary>
    [TestFixture]
    public class ConfigUnitTests
    {
        #region Public Methods

        /// <summary>
        /// Tests the get build server password.
        /// </summary>
        [Test]
        public void TestGetBuildServerPassword()
        {
            RemoveConfigKey(ConfigKey.BuildServerPassword);
            Assert.That(string.IsNullOrEmpty(Config.GetBuildServerPassword()));
            string testValue = "testBuildServerPassword";
            AddConfigKey(ConfigKey.BuildServerPassword, testValue);
            Assert.That(Config.GetBuildServerPassword(), Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get build server URL.
        /// </summary>
        [Test]
        public void TestGetBuildServerUrl()
        {
            RemoveConfigKey(ConfigKey.BuildServerUrl);
            Assert.That(Config.GetBuildServerUrl(), Is.EqualTo("localhost"));
            string testValue = "testBuildServerUrl";
            AddConfigKey(ConfigKey.BuildServerUrl, testValue);
            Assert.That(Config.GetBuildServerUrl(), Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get build server username.
        /// </summary>
        [Test]
        public void TestGetBuildServerUsername()
        {
            RemoveConfigKey(ConfigKey.BuildServerUsername);
            Assert.That(string.IsNullOrEmpty(Config.GetBuildServerUsername()));
            string testValue = "testBuildServerUsername";
            AddConfigKey(ConfigKey.BuildServerUsername, testValue);
            Assert.That(Config.GetBuildServerUsername(), Is.EqualTo("testBuildServerUsername"));
        }

        /// <summary>
        /// Tests the get hostname.
        /// </summary>
        [Test]
        public void TestGetHostname()
        {
            RemoveConfigKey(ConfigKey.Hostname);
            Assert.That(string.IsNullOrEmpty(Config.GetHostname()));
            string testValue = "testHostname";
            AddConfigKey(ConfigKey.Hostname, testValue);
            Assert.That(Config.GetHostname(), Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get initialization enabled option.
        /// </summary>
        [Test]
        public void TestGetInitializationEnabled()
        {
            RemoveConfigKey(ConfigKey.InitializationEnabled);
            Assert.That(Config.GetInitializationEnabled(), Is.False);
            AddConfigKey(ConfigKey.InitializationEnabled, "True");
            Assert.That(Config.GetInitializationEnabled(), Is.EqualTo(true));
            RemoveConfigKey(ConfigKey.InitializationEnabled);
            AddConfigKey(ConfigKey.InitializationEnabled, "true");
            Assert.That(Config.GetInitializationEnabled(), Is.EqualTo(true));
        }

        /// <summary>
        /// Tests the get lights manager port.
        /// </summary>
        [Test]
        public void TestGetLightsManagerPort()
        {
            RemoveConfigKey(ConfigKey.LightsManagerPort);
            Assert.That(Config.GetLightsManagerPort(), Is.EqualTo(9192));
            int testValue = (new Random()).Next();
            AddConfigKey(ConfigKey.LightsManagerPort, testValue.ToString(CultureInfo.InvariantCulture));
            int value = Config.GetLightsManagerPort();
            Assert.That(value, Is.TypeOf(typeof(int)));
            Assert.That(value, Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get notification manager host.
        /// </summary>
        [Test]
        public void TestGetNotificationManagerHost()
        {
            RemoveConfigKey(ConfigKey.NotificationManagerHost);
            Assert.That(Config.GetNotificationManagerHost(), Is.EqualTo("localhost"));
            string testValue = "testBuildServerUrl";
            AddConfigKey(ConfigKey.NotificationManagerHost, testValue);
            Assert.That(Config.GetNotificationManagerHost(), Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get notification manager port.
        /// </summary>
        [Test]
        public void TestGetNotificationManagerPort()
        {
            RemoveConfigKey(ConfigKey.NotificationManagerPort);
            Assert.That(Config.GetNotificationManagerPort(), Is.EqualTo(9191));
            int testValue = (new Random()).Next();
            AddConfigKey(ConfigKey.NotificationManagerPort, testValue.ToString(CultureInfo.InvariantCulture));
            int value = Config.GetNotificationManagerPort();
            Assert.That(value, Is.TypeOf(typeof(int)));
            Assert.That(value, Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get priority period.
        /// </summary>
        [Test]
        public void TestGetPriorityPeriod()
        {
            RemoveConfigKey(ConfigKey.PriorityPeriodHours);
            Assert.That(Config.GetPriorityPeriod().TotalMilliseconds, Is.EqualTo((new TimeSpan(24, 0, 0)).TotalMilliseconds));
            TimeSpan testValue = new TimeSpan(16, 0, 0);
            AddConfigKey(ConfigKey.PriorityPeriodHours, testValue.Hours.ToString(CultureInfo.InvariantCulture));
            TimeSpan value = Config.GetPriorityPeriod();
            Assert.That(value, Is.TypeOf(typeof(TimeSpan)));
            Assert.That(value.TotalMilliseconds, Is.EqualTo(testValue.TotalMilliseconds));
        }

        /// <summary>
        /// Tests the get priority timer period.
        /// </summary>
        [Test]
        public void TestGetPriorityTimerPeriod()
        {
            RemoveConfigKey(ConfigKey.PriorityTimerPeriodMillis);
            Assert.That(Config.GetPriorityTimerPeriod(), Is.EqualTo(60000));
            int testValue = (new Random()).Next();
            AddConfigKey(ConfigKey.PriorityTimerPeriodMillis, testValue.ToString(CultureInfo.InvariantCulture));
            int value = Config.GetPriorityTimerPeriod();
            Assert.That(value, Is.TypeOf(typeof(int)));
            Assert.That(value, Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get retry registration period.
        /// </summary>
        [Test]
        public void TestGetRegistrationRetryPeriod()
        {
            RemoveConfigKey(ConfigKey.RegistrationRetryPeriodMillis);
            Assert.That(Config.GetRegistrationRetryPeriod(), Is.EqualTo(5000));
            int testValue = (new Random()).Next();
            AddConfigKey(ConfigKey.RegistrationRetryPeriodMillis, testValue.ToString(CultureInfo.InvariantCulture));
            int value = Config.GetRegistrationRetryPeriod();
            Assert.That(value, Is.TypeOf(typeof(int)));
            Assert.That(value, Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get control transfer type.
        /// </summary>
        [Test]
        public void TestGetUsbControlTransferType()
        {
            // Test default
            RemoveConfigKey(ConfigKey.UsbControlTransferType);
            Assert.That(Config.GetUsbControlTransferType(), Is.EqualTo(UsbControlTransferType.FeatureReport));

            UsbControlTransferType testValue = UsbControlTransferType.Raw;

            // Test upper (normal) case
            AddConfigKey(ConfigKey.UsbControlTransferType, "Raw");
            Assert.That(Config.GetUsbControlTransferType(), Is.EqualTo(testValue));

            // Test lower case
            RemoveConfigKey(ConfigKey.UsbControlTransferType);
            Assert.That(Config.GetUsbControlTransferType(), Is.EqualTo(UsbControlTransferType.FeatureReport));
            AddConfigKey(ConfigKey.UsbControlTransferType, "raw");
            Assert.That(Config.GetUsbControlTransferType(), Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get USB product ID.
        /// </summary>
        [Test]
        public void TestGetUsbProductId()
        {
            RemoveConfigKey(ConfigKey.UsbProductId);
            Assert.That(Config.GetUsbProductId(), Is.EqualTo(0x0486));
            ushort testValue = 0xA;
            AddConfigKey(ConfigKey.UsbProductId, string.Format("0x{0:x4}", testValue));
            ushort value = Config.GetUsbProductId();
            Assert.That(value, Is.TypeOf(typeof(ushort)));
            Assert.That(value, Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get control transfer type.
        /// </summary>
        [Test]
        public void TestGetUsbProtocolType()
        {
            // Test default
            RemoveConfigKey(ConfigKey.UsbProtocolType);
            Assert.That(Config.GetUsbProtocolType(), Is.EqualTo(UsbProtocolType.Blink1));

            UsbProtocolType testValue = UsbProtocolType.DasBlinkenlichten;

            // Test upper (normal) case
            AddConfigKey(ConfigKey.UsbProtocolType, "DasBlinkenlichten");
            Assert.That(Config.GetUsbProtocolType(), Is.EqualTo(testValue));

            // Reset and test lower case
            RemoveConfigKey(ConfigKey.UsbProtocolType);
            Assert.That(Config.GetUsbProtocolType(), Is.EqualTo(UsbProtocolType.Blink1));
            AddConfigKey(ConfigKey.UsbProtocolType, "dasblinkenlichten");
            Assert.That(Config.GetUsbProtocolType(), Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get USB usage.
        /// </summary>
        [Test]
        public void TestGetUsbUsage()
        {
            RemoveConfigKey(ConfigKey.UsbUsage);
            Assert.That(Config.GetUsbUsage(), Is.EqualTo(0x004));
            ushort testValue = 0xC;
            AddConfigKey(ConfigKey.UsbUsage, string.Format("0x{0:x4}", testValue));
            ushort value = Config.GetUsbUsage();
            Assert.That(value, Is.TypeOf(typeof(ushort)));
            Assert.That(value, Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get USB usage page.
        /// </summary>
        [Test]
        public void TestGetUsbUsagePage()
        {
            RemoveConfigKey(ConfigKey.UsbUsagePage);
            Assert.That(Config.GetUsbUsagePage(), Is.EqualTo(0xFFC9));
            ushort testValue = 0xD;
            AddConfigKey(ConfigKey.UsbUsagePage, string.Format("0x{0:x4}", testValue));
            ushort value = Config.GetUsbUsagePage();
            Assert.That(value, Is.TypeOf(typeof(ushort)));
            Assert.That(value, Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get USB vendor ID.
        /// </summary>
        [Test]
        public void TestGetUsbVendorId()
        {
            RemoveConfigKey(ConfigKey.UsbVendorId);
            Assert.That(Config.GetUsbVendorId(), Is.EqualTo(0x16C0));
            ushort testValue = 0xB;
            AddConfigKey(ConfigKey.UsbVendorId, string.Format("0x{0:x4}", testValue));
            ushort value = Config.GetUsbVendorId();
            Assert.That(value, Is.TypeOf(typeof(ushort)));
            Assert.That(value, Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get username.
        /// </summary>
        [Test]
        public void TestGetUsername()
        {
            RemoveConfigKey(ConfigKey.Username);
            Assert.That(string.IsNullOrEmpty(Config.GetUsername()));
            string testValue = "testUsername";
            AddConfigKey(ConfigKey.Username, testValue);
            Assert.That(Config.GetUsername(), Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get VCS server password.
        /// </summary>
        [Test]
        public void TestGetVcsServerPassword()
        {
            RemoveConfigKey(ConfigKey.VcsServerPassword);
            Assert.That(string.IsNullOrEmpty(Config.GetVcsServerPassword()));
            string testValue = "testVcsServerPassword";
            AddConfigKey(ConfigKey.VcsServerPassword, testValue);
            Assert.That(Config.GetVcsServerPassword(), Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get VCS server URL.
        /// </summary>
        [Test]
        public void TestGetVcsServerUrl()
        {
            RemoveConfigKey(ConfigKey.VcsServerUrl);
            Assert.That(Config.GetVcsServerUrl(), Is.EqualTo("localhost"));
            string testValue = "testVcsServerUrl";
            AddConfigKey(ConfigKey.VcsServerUrl, testValue);
            Assert.That(Config.GetVcsServerUrl(), Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get VCS server username.
        /// </summary>
        [Test]
        public void TestGetVcsServerUsername()
        {
            RemoveConfigKey(ConfigKey.VcsServerUsername);
            Assert.That(string.IsNullOrEmpty(Config.GetVcsServerUsername()));
            string testValue = "testVcsServerUsername";
            AddConfigKey(ConfigKey.VcsServerUsername, testValue);
            Assert.That(Config.GetVcsServerUsername(), Is.EqualTo(testValue));
        }

        /// <summary>
        /// Tests the get wait for device retry period.
        /// </summary>
        [Test]
        public void TestGetWaitForDeviceRetryPeriod()
        {
            RemoveConfigKey(ConfigKey.WaitForDeviceRetryPeriodMillis);
            Assert.That(Config.GetWaitForDeviceRetryPeriod(), Is.EqualTo(1000));
            int testValue = (new Random()).Next();
            AddConfigKey(ConfigKey.WaitForDeviceRetryPeriodMillis, testValue.ToString(CultureInfo.InvariantCulture));
            int value = Config.GetWaitForDeviceRetryPeriod();
            Assert.That(value, Is.TypeOf(typeof(int)));
            Assert.That(value, Is.EqualTo(testValue));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the config key to the appSettings. Helper method for testing default values.
        /// </summary>
        /// <param name="configKey">The config key.</param>
        /// <param name="configValue">The config value.</param>
        internal static void AddConfigKey(string configKey, string configValue)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Add(new KeyValueConfigurationElement(configKey, configValue));
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        /// <summary>
        /// Removes the config key from the appSettings. Helper method for testing default values. 
        /// </summary>
        /// <param name="configKey">The config key.</param>
        internal static void RemoveConfigKey(string configKey)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Remove(configKey);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        #endregion
    }
}