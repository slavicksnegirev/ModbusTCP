using Gtk;
using Gdk;
using Cairo;
using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using Client;

/*==================== CLIENT ====================*/

public partial class MainWindow : Gtk.Window
{
    string text;
    const int port = 8080;
    const int qr_code_size = 400;
    const string ip = "127.0.0.1";

    ModbusTCP modbusTCP = new ModbusTCP(0, 0, 1);

    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();
        
        drawingArea.AddEvents((int)EventMask.ButtonPressMask);
        GLib.Timeout.Add(100, new GLib.TimeoutHandler(OnTimer));
    }

    protected bool OnTimer()
    {
        drawingArea.QueueDraw();

        return true;
    }

    protected void QR_code(Context cc)
    {
        if (text != null)
        {
            var surface = new ImageSurface("QR_code_(client).png");
            cc.SetSourceSurface(surface, 0, 0);
            cc.Paint();
            surface.Dispose();
        }
    }

    protected void OnClicked(object sender, EventArgs e)
    {
        IPEndPoint tcpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        System.Net.Sockets.Socket tcpSocket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        var message = text = entry.Text;
        entry.DeleteText(0, text.Length);

        var buffer = new byte[1024 * 1024];
        

        modbusTCP.RequestToServer(6, Encoding.UTF8.GetBytes(message));
        var data = modbusTCP.GetByteArray(buffer);//Encoding.UTF8.GetBytes(message);

        tcpSocket.Connect(tcpEndPoint);
        try
        {
            tcpSocket.Send(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        try
        {
            tcpSocket.Receive(buffer);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        modbusTCP.ResponseFromServer(modbusTCP.FunctionCode[0], buffer);

        MemoryStream memoryStream = new MemoryStream(modbusTCP.Data);
        System.Drawing.Image bitmap = System.Drawing.Image.FromStream(memoryStream);
        bitmap = new Bitmap(bitmap, new System.Drawing.Size(qr_code_size, qr_code_size));
        bitmap.Save("QR_code_(client).png");
        memoryStream.Close();

        tcpSocket.Shutdown(SocketShutdown.Both);
        tcpSocket.Close();
        
    }

    protected void OnDrawingExposeEvent(object o, ExposeEventArgs args)
    {
        Context cc = CairoHelper.Create(drawingArea.GdkWindow);

        QR_code(cc);

        ((IDisposable)cc.GetTarget()).Dispose();
        ((IDisposable)cc).Dispose();
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }
}
