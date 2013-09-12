// <copyright file="RegistrationRequest.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common.Requests
{
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;

    /// <summary>
    /// A class that identifies a request from a client to register with the notification manager.
    /// </summary>
    public class RegistrationRequest : IRequest
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationRequest"/> class.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="username">The username.</param>
        public RegistrationRequest(string hostname, string username)
        {
            this.Type = RequestType.Register;
            this.Hostname = hostname;
            this.Username = username;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the hostname.
        /// </summary>
        /// <value>
        /// The hostname.
        /// </value>
        public string Hostname { get; set; }

        /// <summary>
        /// Gets the request type.
        /// </summary>
        public RequestType Type { get; private set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        public string Username { get; set; }

        #endregion
    }
}