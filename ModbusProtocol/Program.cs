using System;
using System.Net.Sockets;

namespace ModbusProtokol
{
    /// <summary>
    /// A simple Modbus over TCP client class for LabJack compatible devices.
    /// All Write methods use Modbus function 16, Preset Multiple Registers.
    /// All Read methods use Modbus function 3, Read Multiple Registers.
    ///
    /// Data Types = # Modbus Registers:
    ///    byte = 1/2 register
    ///    ushort = 1 register
    ///    uint = 2 registers
    ///    int = 2 registers
    ///    float = 2 registers
    /// </summary>
    class ModbusTCPClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private ushort _transactionID;
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch(); // remove

        /* Public methods */

        /// <summary>
        /// Constructor that connects to a Modbus device over TCP. Port defaults
        /// to 502.
        /// </summary>
        /// <param name="hostname">The host name of Modbus device.</param>
        public ModbusTCPClient(string hostname)
        {
            this._client = null;
            this._stream = null;
            this.Connect(hostname, 502);
        }

        /// <summary>
        /// Constructor that connects to a Modbus device over TCP.
        /// </summary>
        /// <param name="hostname">The host name of the Modbus device.</param>
        /// <param name="port">The port of the Modbus device.</param>
        public ModbusTCPClient(string hostname, int port)
        {
            this._client = null;
            this._stream = null;
            this.Connect(hostname, port);
        }

        /// <summary>
        /// Deconstructor. Closes the TCP connection.
        /// </summary>
        ~ModbusTCPClient()
        {
            this.Close();
        }

        /// <summary>
        /// Connect to a Modbus device over TCP. Port defaults to 502.
        /// </summary>
        /// <param name="hostname">The host name of the Modbus device.</param>
        public void Connect(string hostname)
        {
            this.Connect(hostname, 502);
        }

        /// <summary>
        /// Connect to a Modbus device over TCP.
        /// </summary>
        /// <param name="hostname">The host name of the Modbus device.</param>
        /// <param name="port">The port of the Modbus device.</param>
        public void Connect(string hostname, int port)
        {
            if (this._client != null)
                this.Close();

            this._client = new TcpClient(hostname, port);
            this._stream = _client.GetStream();
            this._transactionID = 0;
        }

        /// <summary>
        /// Close the TCP connection.
        /// </summary>
        public void Close()
        {
            if (this._stream != null)
                this._stream.Close();
            this._stream = null;

            if (this._client != null)
                this._client.Close();
            this._client = null;
        }

        /// <summary>
        /// Indicates whether there is an active TCP connection.
        /// </summary>
        /// <returns>True is there is a connection, false if there isn't.
        /// </returns>
        public bool IsConnected()
        {
            if (this._client != null)
                return this._client.Connected;
            return false;
        }

        /// <summary>
        /// Set the communication timeouts.
        /// </summary>
        /// <param name="sendTimeout">The send timeout, in milliseconds.</param>
        /// <param name="receiveTimeout">The receive timeout, in
        /// milliseconds.</param>
        public void SetTimeouts(int sendTimeout, int receiveTimeout)
        {
            this._client.ReceiveTimeout = receiveTimeout;
            this._client.SendTimeout = sendTimeout;
        }

        /// <summary>
        /// Write a byte array of data to the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The byte array of data to write.</param>
        public void Write(ushort address, byte[] data)
        {
            //Using Modbus function 16

            //Create Modbus Command
            if (data.Length > 254)
                throw new Exception("Too many bytes. The maximum is 254.");

            if (data.Length % 2 != 0)
                throw new Exception("The number of bytes needs to be a multiple of 2.");

            byte[] com = new byte[13 + data.Length];
            com[7] = 16;
            com[8] = (byte)(address >> 8);
            com[9] = (byte)(address & 0xFF);
            com[10] = 0;
            com[11] = (byte)(data.Length / 2);
            com[12] = (byte)(data.Length);
            Array.Copy(data, 0, com, 13, data.Length);
            this.SetHeader(com);
            this._stream.Write(com, 0, com.Length);

            byte[] res = new byte[12];
            int expectedSize = res.Length;
            int size = this._stream.Read(res, 0, res.Length);

            Array.Resize(ref res, size);
            this.ResponseErrorChecks(res, expectedSize, com);
        }

        /// <summary>
        /// Read a byte array of data from the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The read byte array of data. The array length is
        /// the amount of bytes to read.</param>
        public void Read(ushort address, byte[] data)
        {
            //Using Modbus function 3

            //Create Modbus Command
            if (data.Length > 254)
                throw new Exception("Too many bytes. The maximum is 254.");

            if (data.Length % 2 != 0)
                throw new Exception("The number of bytes needs to be a multiple of 2.");

            byte[] com = new byte[12];
            com[7] = 3;
            com[8] = (byte)(address >> 8);
            com[9] = (byte)(address & 0xFF);
            com[10] = 0;
            com[11] = (byte)(data.Length / 2);
            this.SetHeader(com);
            watch.Reset();
            watch.Start();
            this._stream.Write(com, 0, com.Length);

            byte[] res = new byte[9 + data.Length];
            int expectedSize = res.Length;
            int size = this._stream.Read(res, 0, res.Length);
            Array.Resize(ref res, size);
            this.ResponseErrorChecks(res, expectedSize, com);

            Array.Copy(res, 9, data, 0, data.Length);
        }

        /// <summary>
        /// Write an ushort array of data to the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The ushort array of data to write.</param>
        public void Write(ushort address, ushort[] data)
        {
            if (data.Length > 127)
                throw new Exception("Too many ushorts. The maximum is 127.");

            byte[] bytes = new byte[data.Length * 2];
            for (int i = 0; i < data.Length; i++)
            {
                BitConverter.GetBytes(data[i]).CopyTo(bytes, i * 2);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes, i * 2, 2);
            }
            this.Write(address, bytes);
        }

        /// <summary>
        /// Read an ushort array of data from the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The read ushort array of data. The array length
        /// is the amount of ushorts to read.</param>
        public void Read(ushort address, ushort[] data)
        {
            if (data.Length > 127)
                throw new Exception("Too many ushorts. The maximum is 127.");

            byte[] bytes = new byte[data.Length * 2];
            this.Read(address, bytes);
            for (int i = 0; i < data.Length; i++)
            {
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes, i * 2, 2);
                data[i] = BitConverter.ToUInt16(bytes, i * 2);
            }
        }

        /// <summary>
        /// Write an uint array of data to the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The uint array of data to write.</param>
        public void Write(ushort address, uint[] data)
        {
            if (data.Length > 63)
                throw new Exception("Too many uint. The maximum is 63.");

            byte[] bytes = new byte[data.Length * 4];
            for (int i = 0; i < data.Length; i++)
            {
                BitConverter.GetBytes(data[i]).CopyTo(bytes, i * 4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes, i * 4, 4);
            }
            this.Write(address, bytes);
        }

        /// <summary>
        /// Read an uint array of data from the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The read uint array of data. The array length is
        /// the amount of uints to read.</param>
        public void Read(ushort address, uint[] data)
        {
            if (data.Length > 63)
                throw new Exception("Too many uints. The maximum is 63.");

            byte[] bytes = new byte[data.Length * 4];
            this.Read(address, bytes);
            for (int i = 0; i < data.Length; i++)
            {
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes, i * 4, 4);
                data[i] = BitConverter.ToUInt32(bytes, i * 4);
            }
        }

        /// <summary>
        /// Write an int array of data to the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The int array of data to write.</param>
        public void Write(ushort address, int[] data)
        {
            if (data.Length > 63)
                throw new Exception("Too many ints. The maximum is 63.");

            byte[] bytes = new byte[data.Length * 4];
            for (int i = 0; i < data.Length; i++)
            {
                BitConverter.GetBytes(data[i]).CopyTo(bytes, i * 4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes, i * 4, 4);
            }
            this.Write(address, bytes);
        }

        /// <summary>
        /// Read an int array of data from the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The read int array of data. The array length is
        /// the amount of ints to read.</param>
        public void Read(ushort address, int[] data)
        {
            if (data.Length > 63)
                throw new Exception("Too many ints. The maximum is 63.");

            byte[] bytes = new byte[data.Length * 4];
            this.Read(address, bytes);
            for (int i = 0; i < data.Length; i++)
            {
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes, i * 4, 4);
                data[i] = BitConverter.ToInt32(bytes, i * 4);
            }
        }

        /// <summary>
        /// Write a float array of data to the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The float array of data to write.</param>
        public void Write(ushort address, float[] data)
        {
            if (data.Length > 63)
                throw new Exception("Too many floats. The maximum is 63.");

            byte[] bytes = new byte[data.Length * 4];
            for (int i = 0; i < data.Length; i++)
            {
                BitConverter.GetBytes(data[i]).CopyTo(bytes, i * 4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes, i * 4, 4);
            }
            this.Write(address, bytes);
        }

        /// <summary>
        /// Reads a float array of data from the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The read float array of data. The array length is
        /// the amount of floats to read.</param>
        public void Read(ushort address, float[] data)
        {
            if (data.Length > 63)
                throw new Exception("Too many floats. The maximum is 63.");

            byte[] bytes = new byte[data.Length * 4];
            this.Read(address, bytes);
            for (int i = 0; i < data.Length; i++)
            {
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes, i * 4, 4);
                data[i] = BitConverter.ToSingle(bytes, i * 4);
            }
        }

        /// <summary>
        /// Write a single ushort of data to the Modbus device.
        /// </summary>
        /// <param name="address">The register address.</param>
        /// <param name="data">The ushort data to write.</param>
        public void Write(ushort address, ushort data)
        {
            ushort[] dataArray = new ushort[1];
            dataArray[0] = data;
            Write(address, dataArray);
        }

        /// <summary>
        /// Read a single ushort of data from the Modbus device.
        /// </summary>
        /// <param name="address">The register address.</param>
        /// <param name="data">The read ushort data.</param>
        public void Read(ushort address, ref ushort data)
        {
            ushort[] dataArray = new ushort[1];
            Read(address, dataArray);
            data = dataArray[0];
        }

        /// <summary>
        /// Write a single uint of data to the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The uint data to write.</param>
        public void Write(ushort address, uint data)
        {
            uint[] dataArray = new uint[1];
            dataArray[0] = data;
            this.Write(address, dataArray);
        }

        /// <summary>
        /// Read a single uint of data from the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="value">The read uint data.</param>
        public void Read(ushort address, ref uint data)
        {
            uint[] dataArray = new uint[1];
            this.Read(address, dataArray);
            data = dataArray[0];
        }

        /// <summary>
        /// Write a single int of data to the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The int data to write.</param>
        public void Write(ushort address, int data)
        {
            int[] dataArray = new int[1];
            dataArray[0] = data;
            this.Write(address, dataArray);
        }

        /// <summary>
        /// Read a single int of data from the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The read int data.</param>
        public void Read(ushort address, ref int data)
        {
            int[] dataArray = new int[1];
            this.Read(address, dataArray);
            data = dataArray[0];
        }

        /// <summary>
        /// Write a single float of data to the Modbus device.
        /// 1 uint = 2 registers.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The float data to write.</param>
        public void Write(ushort address, float data)
        {
            float[] dataArray = new float[1];
            dataArray[0] = data;
            this.Write(address, dataArray);
        }

        /// <summary>
        /// Read a single float of data from the Modbus device.
        /// </summary>
        /// <param name="address">The starting register address.</param>
        /// <param name="data">The read float data.</param>
        public void Read(ushort address, ref float data)
        {
            float[] dataArray = new float[1];
            this.Read(address, dataArray);
            data = dataArray[0];
        }

        /* Private methods */

        /// <summary>
        /// Sets the MBAP header of the Modbus TCP command.
        /// </summary>
        /// <param name="command">The byte array for the Modbus TCP command.
        /// The Modbus request bytes 7+ need to be set beforehand. The MBAP
        /// header bytes 0 to 6 will be updated based on the request bytes.
        /// </param>
        private void SetHeader(byte[] command)
        {
            //Transaction ID
            ushort transID = this._transactionID;
            if (this._transactionID >= 65535)
            {
                //Rollover global transaction ID to 0.
                this._transactionID = 0;
            }
            else
            {
                //Increment global transaction ID.
                this._transactionID++;
            }
            command[0] = (byte)(transID >> 8);
            command[1] = (byte)(transID & 0xFF);

            //Protocol ID
            command[2] = 0;
            command[3] = 0;

            //Length
            ushort length = (ushort)(command.Length - 6);
            command[4] = (byte)(length >> 8);
            command[5] = (byte)(length & 0xFF);

            //Unit ID
            command[6] = 1;
        }

        /// <summary>
        /// Checks the Modbus response for errors.
        /// </summary>
        /// <param name="response">The Modbus response byte array.</param>
        /// <param name="expectedSize">The expected response byte array length.</param>
        /// <param name="command">The Modbus command byte array.</param>
        private void ResponseErrorChecks(byte[] response, int expectedLength, byte[] command)
        {
            if (response.Length < expectedLength)
            {
                if (response.Length < 9)
                {
                    throw new Exception("Invalid Modbus response.");
                }
                if ((response[7] & 0x80) > 0)
                {
                    //Bit 7 set, indicating Modbus error
                    throw new Exception("Modbus exception code " + response[8] +
                        ", " + GetExceptionCodeString(response[8]) + ".");
                }
                throw new Exception("Other Modbus response error.");
            }

            if (response[0] != command[0] || response[1] != command[1])
            {
                throw new Exception("Modbus transaction ID mismatch.");
            }
        }

        /// <summary>
        /// Get the Modbus exception name.
        /// </summary>
        /// <param name="code">The exception code.</param>
        /// <returns>The exception name.</returns>
        private string GetExceptionCodeString(uint code)
        {
            switch (code)
            {
                case 1:
                    return "Illegal Function";
                case 2:
                    return "Illegal Data Address";
                case 3:
                    return "Illegal Data Value";
                case 4:
                    return "Slave Device Failure";
                case 5:
                    return "Acknowledge";
                case 6:
                    return "Slave Device Busy";
                case 7:
                    return "Negative Acknowledge";
                case 8:
                    return "Memory Parity Error";
                case 10:
                    return "Gateway Path Unavailable";
                case 11:
                    return "Gateway Target Device Failed to Respond";
            }
            return "";
        }
    }
}
