// <copyright file="RequestType.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Common.Enums
{
    /// <summary>
    /// Request types.
    /// </summary>
    public enum RequestType
    {
        /// <summary>
        /// Unknown request.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Registration request.
        /// </summary>
        Register = 1,

        /// <summary>
        /// Attention request.
        /// </summary>
        Attention = 2,

        /// <summary>
        /// Build active request.
        /// </summary>
        BuildActive = 3,

        /// <summary>
        /// Server status request.
        /// </summary>
        ServerStatus = 4,
    }
}