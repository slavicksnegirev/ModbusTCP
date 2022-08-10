using Gtk;
using System;
using QRCoder;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using Server;

/*==================== SERVER ====================*/

public partial class MainWindow : Gtk.Window
{
    int flag = -1;
    bool isInitialized = false;

    string text = @"";

    const int port = 8080;
    const int qr_code_size = 400;
    const string ip = "127.0.0.1";

    ModbusTCP modbusTCP = new ModbusTCP();
    QRCodeGenerator qr_code = new QRCodeGenerator();
    IPEndPoint tcpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
    System.Net.Sockets.Socket tcpSocket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();

        label3.Text = "IP: " + ip;
        label4.Text = "Port: " + Convert.ToString(port);

        GLib.Timeout.Add(100, new GLib.TimeoutHandler(OnTimer));
    }

    protected bool OnTimer()
    {
        InitServer();
        flag++;

        return true;
    }

    protected void InitServer()
    {
        if (!isInitialized)
        {            
            tcpSocket.Bind(tcpEndPoint);
            tcpSocket.Listen(5);

            isInitialized = true;
        }

        while (flag % 3 == 0)
        {
            var listener = tcpSocket.Accept();           
            var buffer = new byte[1024 * 1024];


            listener.Receive(buffer);
            modbusTCP.GetFieldsData(buffer);

            text = Encoding.UTF8.GetString(modbusTCP.Data);
            label2.Text = "Transaction ID: " + BitConverter.ToInt16(modbusTCP.TransactionID, 0);
            label5.Text = "Protocol ID: " + BitConverter.ToInt16(modbusTCP.ProtocolID, 0);
            label7.Text = "Length: " + BitConverter.ToInt16(modbusTCP.Length, 0);
            label9.Text = "Unit ID: " + BitConverter.ToString(modbusTCP.UnitID, 0);
            label11.Text = "Function code: " + BitConverter.ToString(modbusTCP.FunctionCode, 0);
            label15.Text = "Data: " + text;
            var myData = qr_code.CreateQrCode(text, QRCodeGenerator.ECCLevel.H);
            var code = new QRCode(myData);

            var bitmap = new Bitmap(code.GetGraphic(50));
            bitmap = new Bitmap(bitmap, new System.Drawing.Size(qr_code_size, qr_code_size));
            bitmap.Save("QR_code_(server).png");

            MemoryStream memoryStream = new MemoryStream();

            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            byte[] bitmapBytes = memoryStream.ToArray();
            bitmap.Dispose();
            memoryStream.Close();

            modbusTCP.RequestFromClient(modbusTCP.FunctionCode[0], bitmapBytes);
            modbusTCP.ResponseToClient(modbusTCP.FunctionCode[0], buffer);

            listener.Send(buffer);
            listener.Shutdown(SocketShutdown.Both);
            listener.Close();

            //Console.WriteLine(data);
            flag++;
        }       
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }
}
