using System;
using System.IO;
using System.Text;

namespace Server
{
	public class ModbusTCP
	{
		#region Fields

		private byte[] _transactionID = new byte[2];
		private byte[] _protocolID = new byte[2];
		private byte[] _length = new byte[2];
		private byte[] _uintID = new byte[1];
		private byte[] _functionCode = new byte[1];
		private byte[] _data;

		#endregion Fields

		#region Properties

		public byte[] TransactionID
		{
			set { _transactionID = value; }
			get { return _transactionID; }			
		}

		public byte[] ProtocolID
		{
			set { _protocolID = value; }
			get { return _protocolID; }
		}

		public byte[] Length
		{
			set { _length = value; }
			get { return _length; }
		}

		public byte[] UnitID
		{
			set { _uintID = value; }
			get { return _uintID; }
		}

		public byte[] FunctionCode
		{
			set { _functionCode = value; }
			get { return _functionCode; }
		}

		public byte[] Data
		{
			set { _data = value; }
			get { return _data; }
		}

		#endregion Properties

		//public ModbusTCP(ushort transactionID, ushort protocolID, ushort lenght, byte uintID, byte functionCode, byte[] data)
		//{
		//	TransactionID = BitConverter.GetBytes(transactionID);
		//	ProtocolID = BitConverter.GetBytes(protocolID);
		//	Length = BitConverter.GetBytes(lenght);
		//	UintID = BitConverter.GetBytes(uintID);
		//	FunctionCode = BitConverter.GetBytes(functionCode);
		//	Data = data;
		//}

		public void ResponseToClient(byte functionCode, byte[] data)
		{
			bool flag = false;
			this.FunctionCode = BitConverter.GetBytes(functionCode);

			switch (this.FunctionCode[0])
			{
				case 1:
					Console.WriteLine("(0x01) | Чтение DO | Read Coil Status | Дискретное");
					flag = true;

					break;

				case 2:
					Console.WriteLine("(0x02) | Чтение DI | Read Input Status | Дискретное");
					flag = true;

					break;

				case 3:
					Console.WriteLine("(0x03) | Чтение AO | Read Holding Registers | 16 битное");
					flag = true;

					break;

				case 4:
					Console.WriteLine("(0x04) | Чтение AI | Read Input Registers | 16 битное");
					flag = true;

					break;

				case 5:
					Console.WriteLine("(0x05) | Запись одного DO | Force Single Coil | Дискретное");
					flag = true;

					break;

				case 6:
					Console.WriteLine("(0x06) | Запись одного AO | Preset Single Register | 16 битное");
					flag = true;

					break;

				case 15:
					Console.WriteLine("(0x0F) | Запись нескольких DO | Force Multiple Coils | Дискретное");
					flag = true;

					break;

				case 16:
					Console.WriteLine("(0x10) | Запись нескольких AO | Preset Multiple Registers | 16 битное");
					flag = true;

					break;

				default:
					break;
			}

            if (flag)
            {
				this.GetByteArray(data);

				Console.WriteLine(this.ToString());
			}
		}

		public void RequestFromClient(byte functionCode, byte[] buffer)
		{
			bool flag = false;
			this.FunctionCode = BitConverter.GetBytes(functionCode);

			switch (this.FunctionCode[0])
			{
				case 1:
					Console.WriteLine("(0x01) | Чтение DO | Read Coil Status | Дискретное");
					flag = true;

					break;

				case 2:
					Console.WriteLine("(0x02) | Чтение DI | Read Input Status | Дискретное");
					flag = true;

					break;

				case 3:
					Console.WriteLine("(0x03) | Чтение AO | Read Holding Registers | 16 битное");
					flag = true;

					break;

				case 4:
					Console.WriteLine("(0x04) | Чтение AI | Read Input Registers | 16 битное");
					flag = true;

					break;

				case 5:
					Console.WriteLine("(0x05) | Запись одного DO | Force Single Coil | Дискретное");
					flag = true;

					break;

				case 6:
					Console.WriteLine("(0x06) | Запись одного AO | Preset Single Register | 16 битное");
					flag = true;

					break;

				case 15:
					Console.WriteLine("(0x0F) | Запись нескольких DO | Force Multiple Coils | Дискретное");
					flag = true;

					break;

				case 16:
					Console.WriteLine("(0x10) | Запись нескольких AO | Preset Multiple Registers | 16 битное");
					flag = true;

					break;

				default:
					break;
			}

            if (flag)
            {
				this.Data = buffer;
				this.Length = BitConverter.GetBytes(this.GetLenght());

				Console.WriteLine(this.ToString());
			}
		}

		public ushort GetLenght()
		{
			return (ushort)(/*UintID.Length + FunctionCode.Length + */Data.Length);
		}

		public void GetFieldsData(byte[] buffer)
		{
			MemoryStream memoryStream = new MemoryStream(buffer);

			memoryStream.Read(this.TransactionID, 0, 2);
			memoryStream.Read(this.ProtocolID, 0, 2);
			memoryStream.Read(this.Length, 0, 2);
			memoryStream.Read(this.UnitID, 0, 1);			
			memoryStream.Read(this.FunctionCode, 0, 1);
			this.Data = new byte[BitConverter.ToInt16(this.Length, 0)];
			memoryStream.Read(this.Data, 0, BitConverter.ToInt16(this.Length, 0));
			//Console.WriteLine(this.ToString());
			memoryStream.Close();
		}

		public byte[] GetByteArray(byte[] buffer)
		{
			MemoryStream memoryStream = new MemoryStream(buffer);

			memoryStream.Write(this.TransactionID, 0, 2);
			memoryStream.Write(this.ProtocolID, 0, 2);
			memoryStream.Write(this.Length, 0, 2);
			memoryStream.Write(this.UnitID, 0, 1);
			memoryStream.Write(this.FunctionCode, 0, 1);
			memoryStream.Write(this.Data, 0, this.GetLenght());
			memoryStream.Close();

			return buffer;
		}

		public override string ToString()
		{
			return $"{BitConverter.ToString(TransactionID)}-{BitConverter.ToString(ProtocolID)}-{BitConverter.ToString(Length)}-{BitConverter.ToString(UnitID)}-{BitConverter.ToString(FunctionCode)}-{Encoding.UTF8.GetString(Data)}";
		}
	}
}
