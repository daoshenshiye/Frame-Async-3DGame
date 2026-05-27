using System.Net;
using System.Net.Sockets;
using System.IO;

class TestClient
{
    static int playerId = -1;
    static long udpSeq = 0;
    static byte[] recvBuf = new byte[8192];
    static IPEndPoint srvUdp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 29010);
    static StreamWriter log;
    static int frameCount = 0;

    static void L(string s)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] {s}";
        Console.WriteLine(line);
        log?.WriteLine(line);
        log?.Flush();
    }

    static void Main()
    {
        log = new StreamWriter("sync_test.log", false);
        L("=== FRAME SYNC TEST ===");

        // ====== TCP Handshake ======
        L("TCP connect 127.0.0.1:36252");
        var tcp = new TcpClient();
        tcp.Connect("127.0.0.1", 36252);
        tcp.ReceiveTimeout = 5000;
        var stream = tcp.GetStream();
        L("TCP connected");

        // Wait for TCP data 
        byte[] data = new byte[1024];
        int totalRead = 0;
        for (int i = 0; i < 15; i++)
        {
            if (stream.DataAvailable)
            {
                totalRead = stream.Read(data, 0, data.Length);
                L($"TCP recv {totalRead} bytes");
                break;
            }
            Thread.Sleep(500);
        }

        if (totalRead == 0)
        {
            L("ERROR: No TCP data received! Trying poll...");
            var sock = tcp.Client;
            if (sock.Poll(3000000, SelectMode.SelectRead))
            {
                totalRead = stream.Read(data, 0, data.Length);
                L($"Poll: TCP recv {totalRead} bytes");
            }
        }

        // Parse TCP: ID(4) + bodyLen(4) + [body]
        int idx = 0;
        while (idx + 8 <= totalRead)
        {
            int id = BitConverter.ToInt32(data, idx); idx += 4;
            int bodyLen = BitConverter.ToInt32(data, idx); idx += 4;
            if (id == 450)
            {
                playerId = BitConverter.ToInt32(data, idx);
                L($"PlayerAccessInfo(ID=450): playerId={playerId}");
            }
            else if (id == 205)
            {
                L($"TCPConnectionBuild(ID=205): handshake complete");
            }
            else
            {
                L($"TCP msg: ID={id} len={bodyLen}");
            }
            idx += bodyLen;
        }
        L($"TCP handshake done, playerId={playerId}");
        tcp.Close();

        // ====== UDP connection ======
        var udp = new UdpClient(18501);
        L("Send UdpPlayerAddMsg(ID=466)");

        // Build UdpPlayerAddMsg: type(short)=0, msgId(4), bodyLen(4), playerId(4), hs(4), px(4), py(4), pz(4)
        byte[] addPkt = new byte[2 + 4 + 4 + 4 + 4 + 4 + 4 + 4];
        int pos = 0;
        BitConverter.GetBytes((short)0).CopyTo(addPkt, pos); pos += 2;
        BitConverter.GetBytes(466).CopyTo(addPkt, pos); pos += 4;
        BitConverter.GetBytes(4 + 4 + 4 + 4 + 4).CopyTo(addPkt, pos); pos += 4; // bodyLen
        BitConverter.GetBytes(playerId).CopyTo(addPkt, pos); pos += 4;
        BitConverter.GetBytes(100).CopyTo(addPkt, pos); pos += 4;
        BitConverter.GetBytes(0f).CopyTo(addPkt, pos); pos += 4;
        BitConverter.GetBytes(0f).CopyTo(addPkt, pos); pos += 4;
        BitConverter.GetBytes(0f).CopyTo(addPkt, pos); pos += 4;
        udp.Send(addPkt, addPkt.Length, srvUdp);
        L($"Sent UDP add msg, waiting for response...");
        Thread.Sleep(2000);

        // Check for UDP response (UDPConnectionBuildMsg ID=453)
        for (int i = 0; i < 6; i++)
        {
            if (udp.Available > 0)
            {
                IPEndPoint ep = null;
                byte[] r = udp.Receive(ref ep);
                L($"UDP recv {r.Length}B, type={r[0]}, seq={BitConverter.ToInt64(r, 2)}");
                // Parse: type(2) + seq(8) + msgId(4) + bodyLen(4) + [body]
                // type=0 for SIMPLE (no seq)
                int ti = 0;
                short t = BitConverter.ToInt16(r, ti); ti += 2;
                long seq = -1;
                if (t == 1) { seq = BitConverter.ToInt64(r, ti); ti += 8; }
                int mid = BitConverter.ToInt32(r, ti); ti += 4;
                int bl = BitConverter.ToInt32(r, ti); ti += 4;
                if (mid == 453)
                {
                    long serverFrame = BitConverter.ToInt64(r, ti); ti += 8;
                    int delayBuf = BitConverter.ToInt32(r, ti);
                    L($"UDPConnectionBuild(ID=453): serverFrame={serverFrame}, delayBuf={delayBuf}");
                }
                else
                {
                    L($"  Unexpected: ID={mid}");
                }
                break;
            }
            Thread.Sleep(500);
        }

        // ====== Send inputs ======
        L("===== Phase 1: Pure Horizontal (H=1.0, V=0) =====");
        for (long pf = 2; pf <= 15; pf++)
        {
            SendInput(udp, pf, 1.0f, 0f, false);
            Thread.Sleep(30);
        }

        Thread.Sleep(2000);
        L("===== Phase 2: Pure Vertical (H=0, V=0.8) =====");
        for (long pf = 16; pf <= 30; pf++)
        {
            SendInput(udp, pf, 0f, 0.8f, false);
            Thread.Sleep(30);
        }

        // ====== Receive & parse server frames ======
        Thread.Sleep(3000);
        L("===== Receiving ServerFrames =====");
        udp.Client.ReceiveTimeout = 1000;
        for (int i = 0; i < 80; i++)
        {
            try
            {
                if (udp.Available > 0)
                {
                    IPEndPoint ep = null;
                    byte[] r = udp.Receive(ref ep);
                    ParseServerFrame(r);
                }
            }
            catch { }
            Thread.Sleep(80);
        }

        L("===== DONE =====");
        log?.Close();
        udp.Close();
    }

    static void SendInput(UdpClient udp, long predictFrame, float h, float v, bool jump)
    {
        // InputMessage ID=140
        int bodyLen = 4 + 8 + 4 + 4 + 4 + 1 + 4 + 4 + 4;
        long seq = udpSeq++;
        byte[] pkt = new byte[2 + 8 + 4 + 4 + bodyLen];
        int pos = 0;
        BitConverter.GetBytes((short)1).CopyTo(pkt, pos); pos += 2;
        BitConverter.GetBytes(seq).CopyTo(pkt, pos); pos += 8;
        BitConverter.GetBytes(140).CopyTo(pkt, pos); pos += 4;
        BitConverter.GetBytes(bodyLen).CopyTo(pkt, pos); pos += 4;
        BitConverter.GetBytes(playerId).CopyTo(pkt, pos); pos += 4;
        BitConverter.GetBytes(predictFrame).CopyTo(pkt, pos); pos += 8;
        BitConverter.GetBytes(playerId).CopyTo(pkt, pos); pos += 4;
        BitConverter.GetBytes(v).CopyTo(pkt, pos); pos += 4;
        BitConverter.GetBytes(h).CopyTo(pkt, pos); pos += 4;
        BitConverter.GetBytes(jump).CopyTo(pkt, pos); pos += 1;
        BitConverter.GetBytes(1f).CopyTo(pkt, pos); pos += 4;
        BitConverter.GetBytes(1f).CopyTo(pkt, pos); pos += 4;
        BitConverter.GetBytes(1f).CopyTo(pkt, pos); pos += 4;
        udp.Send(pkt, pkt.Length, srvUdp);
    }

    static void ParseServerFrame(byte[] r)
    {
        if (r.Length < 10) return;
        int idx = 0;
        short type = BitConverter.ToInt16(r, idx); idx += 2;
        long seq = -1;
        if (type == 1 && idx + 8 <= r.Length) { seq = BitConverter.ToInt64(r, idx); idx += 8; }

        int id = BitConverter.ToInt32(r, idx); idx += 4;
        int bodyLen = BitConverter.ToInt32(r, idx); idx += 4;

        if (id == 101) // ServerFrameAuthenMsg
        {
            if (idx + 12 > r.Length) return;
            long frame = BitConverter.ToInt64(r, idx); idx += 8;
            int count = BitConverter.ToInt32(r, idx); idx += 4;
            frameCount++;
            string info = $"Frame#{frame} seq={seq} count={count} | ";
            for (int i = 0; i < count; i++)
            {
                if (idx + 4 + 4 + 4 + 4 + 1 + 4 + 4 + 4 + 4 + 4 + 4 + 4 > r.Length) break;
                int pid = BitConverter.ToInt32(r, idx); idx += 4;
                float inv = BitConverter.ToSingle(r, idx); idx += 4;
                float inh = BitConverter.ToSingle(r, idx); idx += 4;
                bool inj = BitConverter.ToBoolean(r, idx); idx += 1;
                idx += 12; // colliderBoxSize xyz
                int hp = BitConverter.ToInt32(r, idx); idx += 4;
                float px = BitConverter.ToSingle(r, idx); idx += 4;
                float py = BitConverter.ToSingle(r, idx); idx += 4;
                float pz = BitConverter.ToSingle(r, idx); idx += 4;
                info += $"P{pid}(in={inh},{inv}) pos=({px:F4},{py:F4},{pz:F4}) ";
            }
            L($"[FRAME] {info}");
        }
        else if (id == 120) { /* HeartMsg - skip */ }
        else if (id == 453) { /* UDPConnectionBuild - skip */ }
        else if (id == 205) { /* TCPConnectionBuild - skip */ }
        else { L($"  UDP rcv: ID={id} len={bodyLen}"); }
    }
}
