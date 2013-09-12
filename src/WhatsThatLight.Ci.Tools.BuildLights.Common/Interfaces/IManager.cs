// <copyright file="IManager.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces
{
    using System;

    /// <summary>
    /// A generic manager interface for stopping and starting a system. 
    /// </summary>
    public interface IManager
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether this <see cref="IManager"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        bool Running { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Handles a command received on the listener.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void HandleCommand(object sender, EventArgs eventArgs);

        /// <summary>
        /// Starts this instance. 
        /// </summary>
        void Start();

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();

        #endregion
    }
}