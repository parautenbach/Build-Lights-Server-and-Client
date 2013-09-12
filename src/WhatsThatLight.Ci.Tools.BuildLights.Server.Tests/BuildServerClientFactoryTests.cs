// <copyright file="BuildServerClientFactoryTests.cs" company="What's That Light?">
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
    using WhatsThatLight.Ci.Tools.BuildLights.Server.Factories;
    using WhatsThatLight.Ci.Tools.BuildLights.Server.Interfaces;

    using NUnit.Framework;

    /// <summary>
    /// BuildServerClientFactory tests.
    /// </summary>
    public class BuildServerClientFactoryTests
    {
        #region Public Methods

        /// <summary>
        /// Tests the create method.
        /// </summary>
        [Test]
        public void TestSimpleCreate()
        {
            ICredentials credentials = new MockCredentials();
            IBuildServerClient client = BuildServerClientFactory.Create<MockBuildServerClient>(credentials);
            Assert.That(client, Is.Not.Null);
            Assert.That(client, Is.InstanceOf(typeof(MockBuildServerClient)));
        }

        #endregion
    }
}