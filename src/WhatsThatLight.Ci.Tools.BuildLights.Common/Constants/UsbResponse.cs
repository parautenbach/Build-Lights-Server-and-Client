// <copyright file="UsbResponse.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common.Constants
{
    /// <summary>
    /// LED states.
    /// </summary>
    public class UsbResponse
    {
        #region Constants and Fields

        /// <summary>
        /// Acknowledged: Device understood and honoured the command.
        /// </summary>
        public const string Ack = "ack";

        /// <summary>
        /// Not acknowledged: Device doesn't understand the command.
        /// </summary>
        public const string Nak = "nak";

        #endregion
    }
}