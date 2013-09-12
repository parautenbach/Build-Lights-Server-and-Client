// <copyright file="StatusRequest.cs" company="What's That Light?">
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
    /// A request indicating whether the notification manager is up or shutting down.
    /// </summary>
    public class StatusRequest : IRequest
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusRequest"/> class.
        /// </summary>
        /// <param name="status">if set to <c>false</c> the server is shutting down.</param>
        public StatusRequest(bool status)
        {
            this.Type = RequestType.ServerStatus;
            this.Status = status;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether the server is up or down.
        /// </summary>
        /// <value>
        ///   <c>true</c> if up; <c>false</c> if going down.
        /// </value>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets the request type.
        /// </summary>
        public RequestType Type { get; private set; }

        #endregion
    }
}