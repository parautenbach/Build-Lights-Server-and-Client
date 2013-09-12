// <copyright file="User.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common.Entities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A user class.
    /// </summary>
    public class User
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// </summary>
        /// <param name="username">The username.</param>
        public User(string username)
        {
            this.Username = username;
            this.ActiveBuilds = new HashSet<string>();
            this.BuildsResponsibleFor = new HashSet<string>();
            this.IsAttentionPriority = false;
            this.AttentionFirstRequired = null;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the active builds.
        /// </summary>
        /// <value>
        /// The builds active.
        /// </value>
        public HashSet<string> ActiveBuilds { get; private set; }

        /// <summary>
        /// Gets or sets when attention was first required.
        /// </summary>
        /// <value>
        /// The attention first required.
        /// </value>
        public DateTime? AttentionFirstRequired { get; set; }

        /// <summary>
        /// Gets the builds responsible for.
        /// </summary>
        /// <value>
        /// The builds responsible for.
        /// </value>
        public HashSet<string> BuildsResponsibleFor { get; private set; }

        /// <summary>
        /// Gets or sets the user's hostname.
        /// </summary>
        /// <value>
        /// The hostname.
        /// </value>
        public string Hostname { get; set; }

        /// <summary>
        /// Gets the username.
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this user's attention is required, as a priority.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this user's attention is required, as a priority; otherwise, <c>false</c>.
        /// </value>
        public bool IsAttentionPriority { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines whether whether a build or test requires attention.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if any attention is required; otherwise, <c>false</c>.
        /// </returns>
        public bool IsAttentionRequired()
        {
            return this.BuildsResponsibleFor.Count > 0;
        }

        /// <summary>
        /// Determines whether a build is active.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if any builds are active; otherwise, <c>false</c>.
        /// </returns>
        public bool IsBuildActive()
        {
            return this.ActiveBuilds.Count > 0;
        }

        #endregion
    }
}