// <copyright file="ILightsDeviceController.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Client.Interfaces
{
    using HidLibrary;

    using WhatsThatLight.Ci.Tools.BuildLights.Client.Enums;

    /// <summary>
    /// Generic interface for a device controller.
    /// </summary>
    public interface ILightsDeviceController
    {
        #region Events

        /// <summary>
        /// Occurs when a device was inserted.
        /// </summary>
        event LightsDeviceController.DeviceInsertedEventHandler OnDeviceInserted;

        /// <summary>
        /// Occurs when a device was removed.
        /// </summary>
        event LightsDeviceController.DeviceRemovedEventHandler OnDeviceRemoved;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the USB lights device. 
        /// </summary>
        /// <value>
        /// The USB device.
        /// </value>
        /// <remarks>
        /// I don't like creating this backdoor, but right now its my only option
        /// without make a tremendous effort to test at least a part of this. 
        /// </remarks>
        IHidDevice Device { get; set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="LightsDeviceController"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        bool Running { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the length (number of bytes) the feature report must be.
        /// </summary>
        /// <returns>The length (number of bytes) the feature report must be</returns>
        short GetFeatureReportByteLength();

        /// <summary>
        /// Sends a command to the device.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>The status of sending the command.</returns>
        LightsDeviceResult SendCommand(byte[] command);

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