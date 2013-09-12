// <copyright file="UtilsUnitTests.cs" company="What's That Light?">
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
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Constants;

    using NUnit.Framework;

    /// <summary>
    /// Unit tests for the Utils class.
    /// </summary>
    public class UtilsUnitTests
    {
        #region Public Methods

        /// <summary>
        /// Tests the get hostname.
        /// </summary>
        [Test]
        public void TestGetHostname()
        {
            string expectedHostname = "hostname1";

            // The absence of this key must result in an auto detected hostname
            ConfigUnitTests.RemoveConfigKey(ConfigKey.Hostname);
            Assert.That(string.IsNullOrEmpty(Utils.GetHostname()), Is.False);
            Assert.That(expectedHostname, Is.Not.EqualTo(Utils.GetHostname()));

            // Now we must get our expected hostname
            ConfigUnitTests.AddConfigKey(ConfigKey.Hostname, expectedHostname);
            Assert.That(expectedHostname, Is.EqualTo(Utils.GetHostname()));

            // Clean-up
            ConfigUnitTests.RemoveConfigKey(ConfigKey.Hostname);
        }

        /// <summary>
        /// Tests the get username.
        /// </summary>
        [Test]
        public void TestGetUsername()
        {
            string expectedUsername = "user1";

            // The absence of this key must result in an auto detected username
            ConfigUnitTests.RemoveConfigKey(ConfigKey.Username);
            Assert.That(expectedUsername, Is.Not.EqualTo(Utils.GetUsername()));

            // Now we must get our expected username
            ConfigUnitTests.AddConfigKey(ConfigKey.Username, expectedUsername);
            Assert.That(expectedUsername, Is.EqualTo(Utils.GetUsername()));

            // Clean-up
            ConfigUnitTests.RemoveConfigKey(ConfigKey.Username);
        }

        /// <summary>
        /// Tests the get username manually. Use your local username and check 
        /// whether the utils class resolves it correctly. 
        /// </summary>
        [Test]
        [Explicit]
        public void TestManualGetUsername()
        {
            Assert.That("rautenp", Is.EqualTo(Utils.GetUsername()));
        }

        #endregion
    }
}