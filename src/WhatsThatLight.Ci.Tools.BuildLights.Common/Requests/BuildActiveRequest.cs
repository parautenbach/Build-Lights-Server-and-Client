// <copyright file="BuildActiveRequest.cs" company="What's That Light?">
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
    /// A request indicating that one or more builds are active. 
    /// </summary>
    public class BuildActiveRequest : IRequest
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildActiveRequest"/> class.
        /// </summary>
        /// <param name="isBuildsActive">if set to <c>true</c> one or more builds are active.</param>
        public BuildActiveRequest(bool isBuildsActive)
        {
            this.Type = RequestType.BuildActive;
            this.IsBuildsActive = isBuildsActive;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this <see cref="BuildActiveRequest"/> indicates one or more active builds.
        /// </summary>
        /// <value>
        ///   <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        public bool IsBuildsActive { get; private set; }

        /// <summary>
        /// Gets the request type.
        /// </summary>
        public RequestType Type { get; private set; }

        #endregion
    }
}