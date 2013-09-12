// <copyright file="Packet.cs" company="What's That Light?">
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
    /// Constants used in the protocol. 
    /// </summary>
    public class Packet
    {
        #region Constants and Fields

        /// <summary>
        /// The separator that splits key-value pairs from others.
        /// </summary>
        public const char CommandSeparator = ';';

        /// <summary>
        /// The separator that splits a key and value.
        /// </summary>
        public const char FieldSeparator = '=';

        /// <summary>
        /// The separator that splits items in the value of a key-value pair. 
        /// </summary>
        public const char ListSeparator = ',';

        /// <summary>
        /// The maximum packet size which can be handled. 
        /// </summary>
        public const int MaximumPacketSize = 1024;

        /// <summary>
        /// The alternative terminator which identifies when a command has been received (USB commands). 
        /// </summary>
        public const char PacketAltTerminator = '\n';

        /// <summary>
        /// The terminator which identifies when a command has been received (client-server commands). 
        /// </summary>
        public const char PacketTerminator = '!';

        #endregion
    }
}