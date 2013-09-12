// <copyright file="LightsDeviceControllerUnitTests.cs" company="What's That Light?">
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
namespace WhatsThatLight.Ci.Tools.BuildLights.Client.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;

    using HidLibrary;

    using WhatsThatLight.Ci.Tools.BuildLights.Client.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Client.Exceptions;
    using WhatsThatLight.Ci.Tools.BuildLights.Common;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Enums;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Interfaces;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Protocol;
    using WhatsThatLight.Ci.Tools.BuildLights.Common.Requests;

    using NMock;

    using NUnit.Framework;

    /// <summary>
    /// Test the client manager.
    /// </summary>
    public sealed class LightsDeviceControllerUnitTests : IDisposable
    {
        #region Constants and Fields

        /// <summary>
        /// Mock factory. 
        /// </summary>
        private MockFactory mockFactory;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets up the tests.
        /// </summary>
        [TestFixtureSetUp]
        public void SetUp()
        {
            this.mockFactory = new MockFactory();
        }

        /// <summary>
        /// Tests the get feature report byte length when there is no device.
        /// </summary>
        [Test]
        public void TestGetFeatureReportByteLengthForNoDevice()
        {
            LightsDeviceController lightsDeviceController = new LightsDeviceController();
            Assert.That(lightsDeviceController.GetFeatureReportByteLength(), NUnit.Framework.Is.EqualTo(-1));
        }

        /// <summary>
        /// Tests the blinking sequence.
        /// </summary>
        [Test]
        [Explicit("Test result needs to be verified by a human, as it tests the actual switching of LEDs with the lights device connected.")]
        public void TestManualBlinkingSequence()
        {
            LightsDeviceController controller = new LightsDeviceController(Config.GetUsbProductId(), Config.GetUsbVendorId(), Config.GetUsbUsage(), Config.GetUsbUsagePage(), Config.GetWaitForDeviceRetryPeriod(), Config.GetUsbControlTransferType());
            controller.Start();
            int sleepTime = 150;
            int iterations = 10;
            Thread.Sleep(sleepTime);
            Console.WriteLine("Start");
            string command = "red=on\ngreen=on\nyellow=on\n";
            Console.WriteLine(string.Format("Send: {0}", command.Replace('\n', '|')));
            Console.WriteLine(string.Format("Recv: {0}", controller.SendCommand(Encoding.ASCII.GetBytes(command))));
            Thread.Sleep(sleepTime);
            for (int i = 0; i < iterations; i++)
            {
                Console.WriteLine(string.Format("Iteration {0}", i));
                command = "red=on\ngreen=off\nyellow=off\n";
                Console.WriteLine(string.Format("\tSend: {0}", command.Replace('\n', '|')));
                Console.WriteLine(string.Format("\tRecv: {0}", controller.SendCommand(Encoding.ASCII.GetBytes(command))));
                Thread.Sleep(sleepTime);
                command = "red=off\ngreen=on\nyellow=off\n";
                Console.WriteLine(string.Format("\tSend: {0}", command.Replace('\n', '|')));
                Console.WriteLine(string.Format("\tRecv: {0}", controller.SendCommand(Encoding.ASCII.GetBytes(command))));
                Thread.Sleep(sleepTime);
                command = "red=off\ngreen=off\nyellow=on\n";
                Console.WriteLine(string.Format("\tSend: {0}", command.Replace('\n', '|')));
                Console.WriteLine(string.Format("\tRecv: {0}", controller.SendCommand(Encoding.ASCII.GetBytes(command))));
                Thread.Sleep(sleepTime);
            }

            Console.WriteLine("Stop");
            command = "red=on\ngreen=on\nyellow=on\n";
            Console.WriteLine(string.Format("\tSend: {0}", command.Replace('\n', '|')));
            Console.WriteLine(string.Format("\tRecv: {0}", controller.SendCommand(Encoding.ASCII.GetBytes(command))));
            Thread.Sleep(sleepTime);
        }

        /// <summary>
        /// Test result needs to be verified by a human, as it tests the insertion and removal of a blink(1) device. Start with it inserted.
        /// </summary>
        [Test]
        [Explicit("Test result needs to be verified by a human, as it tests the insertion and removal of a blink(1) device. Start with it inserted.")]
        public void TestManualInsertRemoveBlink1()
        {
            object insertedLock = new object();
            object removedLock = new object();

            LightsDeviceController controller = new LightsDeviceController(0x01ED, 0x27B8, 0x0001, 0xFF00, Config.GetWaitForDeviceRetryPeriod(), UsbControlTransferType.FeatureReport);

            controller.OnDeviceInserted += (sender, args) =>
            {
                lock (insertedLock)
                {
                    Monitor.Pulse(insertedLock);
                }
            };

            controller.OnDeviceRemoved += (sender, args) =>
            {
                lock (removedLock)
                {
                    Monitor.Pulse(removedLock);
                }
            };

            controller.Start();
            Assert.IsTrue(controller.Running);

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("Please insert the device");
                lock (insertedLock)
                {
                    Monitor.Wait(insertedLock);
                }

                Console.WriteLine("Device inserted");
                controller.SendCommand(Parser.TranslateForBlink1(new AttentionRequest(false), controller.GetFeatureReportByteLength()));
                Console.WriteLine("Please remove the device");
                lock (removedLock)
                {
                    Monitor.Wait(removedLock);
                }

                Console.WriteLine("Device removed");
            }

            controller.Stop();
        }

        /// <summary>
        /// Manually run the sequence of lights device tests.
        /// </summary>
        [Test]
        [Explicit("Test result needs to be verified by a human, as it tests the actual switching of LEDs with the lights device connected.")]
        public void TestManualSequenceLightsDevice()
        {
            LightsDeviceController controller = new LightsDeviceController(Config.GetUsbProductId(), Config.GetUsbVendorId(), Config.GetUsbUsage(), Config.GetUsbUsagePage(), Config.GetWaitForDeviceRetryPeriod(), Config.GetUsbControlTransferType());
            controller.Start();
            int sleepTimeout = 3000;
            int sosSleepTimeout = 8000;
            List<IRequest> requests = new List<IRequest>();

            // RGY-RGX-RGY-RXY-XGY-RXY-SXY-XGY-RGY
            requests.Add(new StatusRequest(false));
            requests.Add(new BuildActiveRequest(false));
            requests.Add(new BuildActiveRequest(true));
            requests.Add(new AttentionRequest(true));
            requests.Add(new AttentionRequest(false));
            requests.Add(new AttentionRequest(true));
            requests.Add(new AttentionRequest(true, true));
            requests.Add(new AttentionRequest(false));
            requests.Add(new StatusRequest(false));
            Thread.Sleep(sleepTimeout);
            foreach (IRequest request in requests)
            {
                controller.SendCommand(Parser.TranslateForDasBlinkenlichten(request));
                Thread.Sleep((request.GetType() == typeof(AttentionRequest)) && ((AttentionRequest)request).IsPriority ? sosSleepTimeout : sleepTimeout);
            }

            controller.Stop();
        }

        /// <summary>
        /// Manually run the sequence of lights device tests for a blink(1) device.
        /// </summary>
        [Test]
        [Explicit("Test result needs to be verified by a human, as it tests the actual switching of LEDs with the lights device connected.")]
        public void TestManualSequenceLightsDeviceBlink1Device()
        {
            LightsDeviceController controller = new LightsDeviceController(0x01ED, 0x27B8, 0x0001, 0xFF00, Config.GetWaitForDeviceRetryPeriod(), UsbControlTransferType.FeatureReport);
            controller.Start();
            Assert.IsTrue(controller.Running);
            int sleepTimeout = 1000;
            List<IRequest> requests = new List<IRequest>();

            // B-Y-R-Y-R-Y-G-B
            requests.Add(new StatusRequest(false));
            requests.Add(new BuildActiveRequest(true));
            requests.Add(new AttentionRequest(true));
            requests.Add(new BuildActiveRequest(true));
            requests.Add(new AttentionRequest(true, true));
            requests.Add(new BuildActiveRequest(true));
            requests.Add(new AttentionRequest(false));
            requests.Add(new StatusRequest(false));
            Thread.Sleep(sleepTimeout);
            foreach (IRequest request in requests)
            {
                if (request.GetType() == typeof(StatusRequest))
                {
                    controller.SendCommand(Parser.TranslateForBlink1((StatusRequest)request, controller.GetFeatureReportByteLength()));
                }
                else if (request.GetType() == typeof(AttentionRequest))
                {
                    controller.SendCommand(Parser.TranslateForBlink1((AttentionRequest)request, controller.GetFeatureReportByteLength()));
                }
                else if (request.GetType() == typeof(BuildActiveRequest))
                {
                    controller.SendCommand(Parser.TranslateForBlink1((BuildActiveRequest)request, controller.GetFeatureReportByteLength()));
                }

                Thread.Sleep(sleepTimeout);
            }

            controller.Stop();
        }

        /// <summary>
        /// Tests that the controller starts after been stopped.
        /// </summary>
        [Test]
        public void TestStartAfterBeenStopped()
        {
            // Setup
            LightsDeviceController lightsDeviceController = new LightsDeviceController();
            Assert.That(lightsDeviceController.Running, NUnit.Framework.Is.False);
            lightsDeviceController.Start();
            Assert.That(lightsDeviceController.Running, NUnit.Framework.Is.True);
            lightsDeviceController.Stop();
            Assert.That(lightsDeviceController.Running, NUnit.Framework.Is.False);

            // Test
            lightsDeviceController.Start();
            Assert.That(lightsDeviceController.Running, NUnit.Framework.Is.True);

            // Clean-up
            lightsDeviceController.Stop();
            Assert.That(lightsDeviceController.Running, NUnit.Framework.Is.False);
        }

        /// <summary>
        /// Tests that the controller restarts after already been started.
        /// </summary>
        [Test]
        public void TestStartAlreadyStarted()
        {
            // Setup
            LightsDeviceController lightsDeviceController = new LightsDeviceController();
            Assert.That(lightsDeviceController.Running, NUnit.Framework.Is.False);
            lightsDeviceController.Start();
            Assert.That(lightsDeviceController.Running, NUnit.Framework.Is.True);

            // Test
            lightsDeviceController.Start();
            Assert.That(lightsDeviceController.Running, NUnit.Framework.Is.True);

            // Clean-up
            lightsDeviceController.Stop();
            Assert.That(lightsDeviceController.Running, NUnit.Framework.Is.False);
        }

        /// <summary>
        /// Tests the unsupported USB control transfer type exception gets thrown.
        /// </summary>
        [Test]
        [ExpectedException(typeof(UnsupportedUsbControlTransferTypeException))]
        public void TestUnsupportedUsbControlTransferTypeGetsThrown()
        {
            // Setup
            LightsDeviceController lightsDeviceController = new LightsDeviceController(0, 0, 0, 0, 1000, UsbControlTransferType.None);
            Mock<IHidDevice> mockDevice = this.mockFactory.CreateMock<IHidDevice>();
            mockDevice.Expects.AtLeastOne.GetProperty(p => p.IsConnected).WillReturn(true);
            mockDevice.Expects.AtLeastOne.GetProperty(p => p.IsOpen).WillReturn(true);
            lightsDeviceController.Device = mockDevice.MockObject;

            // Test
            lightsDeviceController.SendCommand(new byte[0]);
            Assert.Fail();
        }

        /// <summary>
        /// Tests the USB control transfer type of feature report can be sent via the lights device controller.
        /// </summary>
        [Test]
        public void TestUsbControlTransferTypeFeatureReport()
        {
            // Setup
            LightsDeviceController lightsDeviceController = new LightsDeviceController(0, 0, 0, 0, 1000, UsbControlTransferType.FeatureReport);
            Mock<IHidDevice> mockDevice = this.mockFactory.CreateMock<IHidDevice>();
            mockDevice.Expects.AtLeastOne.GetProperty(p => p.IsConnected).WillReturn(true);
            mockDevice.Expects.AtLeastOne.GetProperty(p => p.IsOpen).WillReturn(true);
            mockDevice.Expects.One.Method(m => m.WriteFeatureData(new byte[0])).WithAnyArguments().WillReturn(true);
            mockDevice.Expects.One.Method(m => m.WriteFeatureData(new byte[0])).WithAnyArguments().WillReturn(false);
            lightsDeviceController.Device = mockDevice.MockObject;

            // Test
            Assert.That(lightsDeviceController.SendCommand(new byte[0]), NUnit.Framework.Is.EqualTo(LightsDeviceResult.Ack));
            Assert.That(lightsDeviceController.SendCommand(new byte[0]), NUnit.Framework.Is.EqualTo(LightsDeviceResult.Nak));
        }

        #endregion

        #region Implemented Interfaces

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.mockFactory != null)
            {
                this.mockFactory.Dispose();
            }
        }

        #endregion

        #endregion
    }
}