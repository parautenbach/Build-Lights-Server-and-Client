// <copyright file="AttentionRequest.cs" company="What's That Light?">
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
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Exceptions;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;

    /// <summary>
    /// A request indicating whether one or more builds requires attention.
    /// </summary>
    public class AttentionRequest : IRequest
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AttentionRequest"/> class.
        /// </summary>
        /// <param name="isAttentionRequired">if set to <c>true</c> if attention is required.</param>
        public AttentionRequest(bool isAttentionRequired)
                : this(isAttentionRequired, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttentionRequest"/> class.
        /// </summary>
        /// <param name="isAttentionRequired">if set to <c>true</c> if attention is required.</param>
        /// <param name="isPriority">if set to <c>true</c> if it is a priority.</param>
        public AttentionRequest(bool isAttentionRequired, bool isPriority)
        {
            if (isPriority && !isAttentionRequired)
            {
                throw new InvalidRequestException("Priority can only set if attention is required");
            }

            // TODO: Create base class and move type there
            this.Type = RequestType.Attention;
            this.IsAttentionRequired = isAttentionRequired;
            this.IsPriority = isPriority;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance is attention required.
        /// </summary>
        /// <value>
        ///     <c>true</c> if attention is required; otherwise, <c>false</c>.
        /// </value>
        public bool IsAttentionRequired { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="AttentionRequest"/> is a priority.
        /// </summary>
        /// <value>
        ///   <c>true</c> if a priority; otherwise, <c>false</c>.
        /// </value>
        public bool IsPriority { get; private set; }

        /// <summary>
        /// Gets the request type.
        /// </summary>
        public RequestType Type { get; private set; }

        #endregion
    }
}