// <copyright file="BuildServerClientFactory.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Server.Factories
{
    using System;

    using WhatsThatLight.Ci.Tools.BuildLights.Server.Interfaces;

    /// <summary>
    /// A build server factory for constructing a build server client. 
    /// </summary>
    public class BuildServerClientFactory
    {
        #region Public Methods

        /// <summary>
        /// Creates the specified build server client with the given credentials.
        /// </summary>
        /// <typeparam name="T">A class that implements <see cref="IBuildServerClient"/></typeparam>
        /// <param name="credentials">The credentials.</param>
        /// <returns>A <see cref="IBuildServerClient"/> instance.</returns>
        public static IBuildServerClient Create<T>(ICredentials credentials) where T : IBuildServerClient
        {
            return (IBuildServerClient)Activator.CreateInstance(typeof(T), credentials);
        }

        /// <summary>
        /// Creates the specified build server credentials.
        /// </summary>
        /// <typeparam name="T">A class that implements <see cref="IBuildServerClient"/></typeparam>
        /// <param name="buildServerCredentials">The build server's credentials.</param>
        /// <param name="vcsClient">The VCS's credentials. This is for build servers that don't have all the appropriate information to <see cref="UserCache.Initialize"/> the <see cref="UserCache"/></param>
        /// <returns>A <see cref="IBuildServerClient"/> instance.</returns>
        public static IBuildServerClient Create<T>(ICredentials buildServerCredentials, IVcsClient vcsClient) where T : IBuildServerClient
        {
            return (IBuildServerClient)Activator.CreateInstance(typeof(T), buildServerCredentials, vcsClient);
        }

        #endregion
    }
}