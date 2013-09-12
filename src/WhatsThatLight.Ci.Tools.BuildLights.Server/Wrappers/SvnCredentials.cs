// <copyright file="SvnCredentials.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Server.Wrappers
{
    using WhatsThatLight.Ci.Tools.BuildLights.Server.Interfaces;

    /// <summary>
    /// TeamCity server credentials.
    /// </summary>
    public class SvnCredentials : ICredentials
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SvnCredentials"/> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public SvnCredentials(string url, string username, string password)
        {
            this.Url = url;
            this.Username = username;
            this.Password = password;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password { get; private set; }

        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        public string Url { get; private set; }

        /// <summary>
        /// Gets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        public string Username { get; private set; }

        #endregion
    }
}