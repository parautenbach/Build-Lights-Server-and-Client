// <copyright file="LightsDeviceController.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Client
{
    using System;
    using System.Text;
    using System.Threading;

    using HidLibrary;

    using WhatsThatLight.Ci.Tools.BuildLights.Client.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Client.Exceptions;
    using WhatsThatLight.Ci.Tools.BuildLights.Client.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Constants;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;

    using log4net;

    /// <summary>
    /// A class that controls the lights device.
    /// </summary>
    public class LightsDeviceController : ILightsDeviceController
    {
        #region Constants and Fields

        /// <summary>
        /// Local logger. 
        /// </summary>
        protected static readonly ILog Log = LogManager.GetLogger(typeof(LightsDeviceController));

        /// <summary>
        /// The log4net instance to use for logging. 
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(typeof(LightsDeviceController));

        /// <summary>
        /// The USB control transfer type.
        /// </summary>
        private readonly UsbControlTransferType controlTransferType;

        /// <summary>
        /// Spin timeout to wait until device is connected and open.
        /// </summary>
        private readonly int notifyDeviceInsertedSpinWaitTimeout = 10000;

        /// <summary>
        /// The USB lights device's PID. 
        /// </summary>
        private readonly ushort productId;

        /// <summary>
        /// Synchronisation run lock. 
        /// </summary>
        private readonly object runLock = new object();

        /// <summary>
        /// The USB lights device's usage.
        /// </summary>
        private readonly ushort usage;

        /// <summary>
        /// The USB lights device's usage page.
        /// </summary>
        private readonly ushort usagePage;

        /// <summary>
        /// The USB lights device's VID. 
        /// </summary>
        private readonly ushort vendorId;

        /// <summary>
        /// The time (in ms) to wait between retries if the connection to
        /// the NotificationManager couldn't be made during registration. 
        /// </summary>
        private readonly int waitForDeviceRetryTimeout;

        /// <summary>
        /// The thread that waits for the lights device to be detected. 
        /// </summary>
        private Thread waitForDeviceThread;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LightsDeviceController"/> class.
        /// </summary>
        /// <param name="productId">The USB devices' product ID.</param>
        /// <param name="vendorId">The USB devices' vendor ID.</param>
        /// <param name="usage">The usage.</param>
        /// <param name="usagePage">The usage page.</param>
        /// <param name="waitForDeviceRetryPeriod">The wait for device retry period.</param>
        /// <param name="controlTransferType">Type of the control transfer (raw, feature, etc.).</param>
        public LightsDeviceController(ushort productId, ushort vendorId, ushort usage, ushort usagePage, int waitForDeviceRetryPeriod, UsbControlTransferType controlTransferType)
        {
            this.Running = false;
            this.productId = productId;
            this.vendorId = vendorId;
            this.usage = usage;
            this.usagePage = usagePage;
            this.Device = null;
            this.waitForDeviceRetryTimeout = waitForDeviceRetryPeriod;
            this.controlTransferType = controlTransferType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LightsDeviceController"/> class.
        /// </summary>
        /// <param name="productId">The product id.</param>
        /// <param name="vendorId">The vendor id.</param>
        /// <param name="usage">The usage.</param>
        /// <param name="usagePage">The usage page.</param>
        public LightsDeviceController(ushort productId, ushort vendorId, ushort usage, ushort usagePage)
                : this(productId, vendorId, usage, usagePage, Config.GetWaitForDeviceRetryPeriod(), Config.GetUsbControlTransferType())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LightsDeviceController"/> class.
        /// </summary>
        public LightsDeviceController()
                : this(Config.GetUsbProductId(), Config.GetUsbVendorId(), Config.GetUsbUsage(), Config.GetUsbUsagePage(), Config.GetWaitForDeviceRetryPeriod(), Config.GetUsbControlTransferType())
        {
        }

        #endregion

        #region Delegates

        /// <summary>
        /// A delegate used when a device was inserted. 
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public delegate void DeviceInsertedEventHandler(object sender, EventArgs e);

        /// <summary>
        /// A delegate used when a device was removed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        public delegate void DeviceRemovedEventHandler(object sender, EventArgs e);

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a device was inserted.
        /// </summary>
        public event DeviceInsertedEventHandler OnDeviceInserted;

        /// <summary>
        /// Occurs when a device was removed.
        /// </summary>
        public event DeviceRemovedEventHandler OnDeviceRemoved;

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
        public IHidDevice Device { get; set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="LightsDeviceController"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        public bool Running { get; private set; }

        #endregion

        #region Implemented Interfaces

        #region ILightsDeviceController

        /// <summary>
        /// Gets the length (number of bytes) the feature report must be.
        /// </summary>
        /// <returns>The length (number of bytes) the feature report must be; -1 indicates it is unavailable.</returns>
        public short GetFeatureReportByteLength()
        {
            if (this.Device != null && this.Device.Capabilities != null)
            {
                return this.Device.Capabilities.FeatureReportByteLength;
            }

            return -1;
        }

        /// <summary>
        /// Sends a command to the device.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>The status of sending the command.</returns>
        public LightsDeviceResult SendCommand(byte[] command)
        {
            if (this.Device == null || !this.Device.IsConnected)
            {
                return LightsDeviceResult.NotConnected;
            }

            if (!this.Device.IsOpen)
            {
                this.Device.OpenDevice();
            }

            if (!this.Device.IsOpen)
            {
                return LightsDeviceResult.NotOpen;
            }

            if (this.controlTransferType == UsbControlTransferType.Raw)
            {
                HidReport report = this.Device.CreateReport();
                report.Data = command;
                Log.Debug(string.Format("Sending command to USB device: {0}", BitConverter.ToString(report.Data)));
                bool result = this.Device.WriteReport(report);
                Log.Debug(string.Format("USB write result: {0}", result));

                if (!result)
                {
                    return LightsDeviceResult.NoResponse;
                }

                HidDeviceData data = this.Device.Read();
                Log.Debug(string.Format("USB device response: {0}", BitConverter.ToString(data.Data)));
                string usbResult = Encoding.ASCII.GetString(data.Data);
                return usbResult.Contains(UsbResponse.Ack) ? LightsDeviceResult.Ack : LightsDeviceResult.Nak;
            }

            if (this.controlTransferType == UsbControlTransferType.FeatureReport)
            {
                return this.Device.WriteFeatureData(command) ? LightsDeviceResult.Ack : LightsDeviceResult.Nak;
            }

            throw new UnsupportedUsbControlTransferTypeException(string.Format("Type {0} not supported", this.controlTransferType));
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            lock (this.runLock)
            {
                Log.Info("Starting USB lights device controller");
                if (this.Running)
                {
                    log.Error("Attempted to start an already running USB lights device controller");
                    return;
                }

                this.Running = true;
                this.waitForDeviceThread = new Thread(this.WaitForDevice);
                this.waitForDeviceThread.Start();
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            lock (this.runLock)
            {
                if (!this.Running)
                {
                    log.Error("Attempted to stop an already stopped USB lights device controller");
                    return;
                }

                Log.Info("Stopping USB lights device controller");
                this.Running = false;
                if (this.waitForDeviceThread != null)
                {
                    try
                    {
                        Log.Info("Stopping USB lights device controller");
                        this.waitForDeviceThread.Join(2 * this.waitForDeviceRetryTimeout);
                        this.waitForDeviceThread.Abort();
                    }
                    catch (Exception e)
                    {
                        log.Error(e);
                    }
                }

                log.Info("USB lights device controller stopped");
            }
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Notifies the subscriber that the device was inserted.
        /// </summary>
        private void NotifyDeviceInserted()
        {
            // Fix: [CIBL-1] System.OverflowException: Arithmetic operation resulted in an overflow
            // When we cannot read the feature report length, the device isn't yet ready for sending commands.
            while (this.GetFeatureReportByteLength() < 0)
            {
                Thread.SpinWait(this.notifyDeviceInsertedSpinWaitTimeout);
            }

            if (this.OnDeviceInserted != null)
            {
                this.OnDeviceInserted(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Notifies the subscriber that the device was removed.
        /// </summary>
        private void NotifyDeviceRemoved()
        {
            if (this.OnDeviceRemoved != null)
            {
                this.OnDeviceRemoved(null, EventArgs.Empty);
            }

            this.Stop();
            this.Start();
        }

        /// <summary>
        /// Waits for a lights device to be inserted or connected.
        /// </summary>
        private void WaitForDevice()
        {
            try
            {
                Log.Info(string.Format("Enumerating for USB device (vid_0x{0:x4}, pid_0x{1:x4}, 0x{2:x4}, 0x{3:x4})", this.vendorId, this.productId, this.usage, this.usagePage));
                this.Device = null;
                while (this.Device == null)
                {
                    if (!this.Running)
                    {
                        return;
                    }

                    // All four hex values must match
                    // Section Device Configuration Options: http://www.pjrc.com/teensy/rawhid.html
                    // NOTE: This line complicates testing, since the Enumerate method is static. We can't
                    // mock a static method, at least not with a significant amount of hacking. This is a
                    // small bit of code that we can't test, so we just live with it. 
                    foreach (HidDevice device in HidDevices.Enumerate(this.vendorId, this.productId))
                    {
                        // The HidLibrary /should/ supply the usage and usage page values as unsigned 16 bit integers
                        ushort deviceUsage = BitConverter.ToUInt16(BitConverter.GetBytes(device.Capabilities.Usage), 0);
                        ushort deviceUsagePage = BitConverter.ToUInt16(BitConverter.GetBytes(device.Capabilities.UsagePage), 0);
                        if (deviceUsage == this.usage && deviceUsagePage == this.usagePage)
                        {
                            this.Device = device;
                            Log.Info(string.Format("USB lights device detected: {0}", this.Device.DevicePath));
                            break;
                        }
                    }

                    Log.Debug("Sleeping USB device thread");

                    // Yeah, yeah, I know. Sleeping is bad (at least when programming threads). A more
                    // appropriate solution would be to use a timer or a monitor so that it can be interrupted. 
                    // Since we expect to poll relatively fast (and because I don't know how to detect that
                    // a device was added (not without knowing about it first and THEN monitoring it), I 
                    // decided the use of sleep is justified. Check the user cache if you think I can't work
                    // with timers... :-)
                    // TODO: Improve with monitor or timer
                    Thread.Sleep(this.waitForDeviceRetryTimeout);
                }

                Log.Info("Subscribing USB lights device events");
                this.Device.Inserted += this.NotifyDeviceInserted;
                this.Device.Removed += this.NotifyDeviceRemoved;
                this.Device.MonitorDeviceEvents = true;
            }
            catch (ThreadAbortException)
            {
                // Expected behaviour on shutdown
                Thread.ResetAbort();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        #endregion
    }
}