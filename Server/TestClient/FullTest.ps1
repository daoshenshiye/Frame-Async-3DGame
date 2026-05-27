param(
    [string]$ServerPath = "D:\unity-Git\Frame Async 3DGame\Frame Async 3DGame\Server\ClientSocket\ClientSocket\bin\Debug\net10.0\ClientSocket.exe"
)

$logFile = "D:\unity-Git\Frame Async 3DGame\Frame Async 3DGame\Server\TestClient\full_test.log"
function Log($msg) {
    $line = "[$(Get-Date -Format 'HH:mm:ss.fff')] $msg"
    Write-Output $line
    Add-Content -Path $logFile -Value $line
}

# Clean up
Get-Process ClientSocket -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 1
Remove-Item $logFile -ErrorAction SilentlyContinue

Log "=== Starting Server ==="
$serverPsi = New-Object System.Diagnostics.ProcessStartInfo
$serverPsi.FileName = $ServerPath
$serverPsi.UseShellExecute = $false
$serverPsi.CreateNoWindow = $true
$server = [System.Diagnostics.Process]::Start($serverPsi)
Start-Sleep 3

# ===== TCP =====
Log "=== TCP Handshake ==="
$tcp = New-Object System.Net.Sockets.TcpClient
$tcp.Connect("127.0.0.1", 36252)
$stream = $tcp.GetStream()
Start-Sleep 3

if (-not $stream.DataAvailable) {
    Log "ERROR: No TCP data after 3s"
    $tcp.Close()
    $server.Kill()
    exit 1
}

$buf = New-Object byte[] 1024
$read = $stream.Read($buf, 0, 1024)
Log "TCP recv $read bytes"

$playerId = -1
$idx = 0
while ($idx + 8 -le $read) {
    $msgId = [System.BitConverter]::ToInt32($buf, $idx); $idx += 4
    $bodyLen = [System.BitConverter]::ToInt32($buf, $idx); $idx += 4
    if ($msgId -eq 450) {
        $playerId = [System.BitConverter]::ToInt32($buf, $idx)
        Log "PlayerAccessInfoMsg(ID=450): playerId=$playerId"
    } elseif ($msgId -eq 205) {
        Log "TCPConnectionBuildMsg(ID=205): handshake OK"
    } else {
        Log "Unknown TCP msg: ID=$msgId"
    }
    $idx += $bodyLen
}
$tcp.Close()
Log "playerId=$playerId"

# ===== UDP =====
Log "=== UDP Registration ==="
$udp = New-Object System.Net.Sockets.UdpClient(18502)
$srvUdpEp = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Parse("127.0.0.1"), 29010)

# UdpPlayerAddMsg (ID=466)
$pkt = New-Object byte[] (2+4+4+4+4+4+4+4)
$pos = 0
[System.BitConverter]::GetBytes([short]0).CopyTo($pkt, $pos); $pos += 2
[System.BitConverter]::GetBytes(466).CopyTo($pkt, $pos); $pos += 4
[System.BitConverter]::GetBytes(24).CopyTo($pkt, $pos); $pos += 4  # bodyLen
[System.BitConverter]::GetBytes($playerId).CopyTo($pkt, $pos); $pos += 4
[System.BitConverter]::GetBytes(100).CopyTo($pkt, $pos); $pos += 4
[System.BitConverter]::GetBytes(0.0).CopyTo($pkt, $pos); $pos += 4
[System.BitConverter]::GetBytes(0.0).CopyTo($pkt, $pos); $pos += 4
[System.BitConverter]::GetBytes(0.0).CopyTo($pkt, $pos); $pos += 4
$udp.Send($pkt, $pkt.Length, $srvUdpEp) | Out-Null
Log "Sent UdpPlayerAddMsg(playerId=$playerId)"

Start-Sleep 2
$udp.Client.ReceiveTimeout = 2000
try {
    $remoteEp = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)
    $resp = $udp.Receive([ref]$remoteEp)
    Log "UDP response: $($resp.Length) bytes from $remoteEp"
    $p = 0
    $t = [System.BitConverter]::ToInt16($resp, $p); $p += 2
    if ($t -eq 0) {
        $mid = [System.BitConverter]::ToInt32($resp, $p); $p += 4
        $bl = [System.BitConverter]::ToInt32($resp, $p); $p += 4
        Log "  type=SIMPLE ID=$mid bodyLen=$bl"
        if ($mid -eq 453) {
            $sf = [System.BitConverter]::ToInt64($resp, $p); $p += 8
            $db = [System.BitConverter]::ToInt32($resp, $p)
            Log "  UDPConnectionBuild: serverFrame=$sf delayBuf=$db"
        }
    }
} catch { Log "No UDP response received" }

# ===== Send Inputs =====
Log "=== Sending Inputs (Phase 1: H=1.5 for 10 frames) ==="
$seq = 0
$expectedHits = @()
for ($pf = 2; $pf -le 12; $pf++) {
    $bodyLen = 4 + 8 + 4 + 4 + 4 + 1 + 4 + 4 + 4
    $pkt = New-Object byte[] (2+8+4+4+$bodyLen)
    $pos = 0
    [System.BitConverter]::GetBytes([short]1).CopyTo($pkt, $pos); $pos += 2
    [System.BitConverter]::GetBytes($seq).CopyTo($pkt, $pos); $pos += 8; $seq++
    [System.BitConverter]::GetBytes(140).CopyTo($pkt, $pos); $pos += 4  # InputMessage
    [System.BitConverter]::GetBytes($bodyLen).CopyTo($pkt, $pos); $pos += 4
    [System.BitConverter]::GetBytes($playerId).CopyTo($pkt, $pos); $pos += 4
    [System.BitConverter]::GetBytes($pf).CopyTo($pkt, $pos); $pos += 8  # predictFrame
    [System.BitConverter]::GetBytes($playerId).CopyTo($pkt, $pos); $pos += 4
    [System.BitConverter]::GetBytes(0.0).CopyTo($pkt, $pos); $pos += 4  # V=0
    [System.BitConverter]::GetBytes(1.5).CopyTo($pkt, $pos); $pos += 4  # H=1.5
    [System.BitConverter]::GetBytes([byte]0).CopyTo($pkt, $pos); $pos += 1  # jump=false
    [System.BitConverter]::GetBytes(1.0).CopyTo($pkt, $pos); $pos += 4
    [System.BitConverter]::GetBytes(1.0).CopyTo($pkt, $pos); $pos += 4
    [System.BitConverter]::GetBytes(1.0).CopyTo($pkt, $pos); $pos += 4
    $udp.Send($pkt, $pkt.Length, $srvUdpEp) | Out-Null
    Start-Sleep -Milliseconds 50
}
Log "Sent $($seq) inputs (frames 2-12)"

# ===== Receive Server Frames =====
Start-Sleep 4
Log "=== Receiving ServerFrames ==="
$frameCount = 0
$prevPos = @{}
for ($i = 0; $i -lt 60; $i++) {
    if ($udp.Available -gt 0) {
        try {
            $remoteEp = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)
            $r = $udp.Receive([ref]$remoteEp)
            $pos = 0
            $type = [System.BitConverter]::ToInt16($r, $pos); $pos += 2
            $nowSeq = -1
            if ($type -eq 1) { $nowSeq = [System.BitConverter]::ToInt64($r, $pos); $pos += 8 }
            $msgId = [System.BitConverter]::ToInt32($r, $pos); $pos += 4
            $bodyLen = [System.BitConverter]::ToInt32($r, $pos); $pos += 4
            
            if ($msgId -eq 101) {
                $frame = [System.BitConverter]::ToInt64($r, $pos); $pos += 8
                $count = [System.BitConverter]::ToInt32($r, $pos); $pos += 4
                $frameCount++
                $msg = "FRAME#$frame players=$count | "
                for ($j = 0; $j -lt $count; $j++) {
                    $pid = [System.BitConverter]::ToInt32($r, $pos); $pos += 4
                    $inv = [System.BitConverter]::ToSingle($r, $pos); $pos += 4
                    $inh = [System.BitConverter]::ToSingle($r, $pos); $pos += 4
                    [System.BitConverter]::ToBoolean($r, $pos); $pos += 1  # jump
                    $pos += 12  # colliderBoxSize
                    $hp = [System.BitConverter]::ToInt32($r, $pos); $pos += 4
                    $px = [System.BitConverter]::ToSingle($r, $pos); $pos += 4
                    $py = [System.BitConverter]::ToSingle($r, $pos); $pos += 4
                    $pz = [System.BitConverter]::ToSingle($r, $pos); $pos += 4
                    
                    # Calculate delta
                    $key = "$pid"
                    $delta = ""
                    if ($prevPos.ContainsKey($key)) {
                        $dx = $px - $prevPos[$key].x
                        $dz = $pz - $prevPos[$key].z
                        $delta = " d=($('{0:F4}' -f $dx),$('{0:F4}' -f $dz))"
                    }
                    $prevPos[$key] = @{x=$px; y=$py; z=$pz}
                    
                    $msg += "P{0}(in={1:F1},{2:F1}) pos=({3:F4},{4:F4},{5:F4})$delta | " -f $pid,$inh,$inh,$px,$py,$pz
                }
                Log $msg
            } elseif ($msgId -ne 120 -and $msgId -ne 453) {
                Log "  Other UDP: ID=$msgId type=$type"
            }
        } catch { }
    }
    Start-Sleep -Milliseconds 100
}
Log "=== Received $frameCount frames ==="

# ===== Phase 2 =====
Log "=== Sending Inputs (Phase 2: V=0.8 for 15 frames) ==="
for ($pf = 14; $pf -le 28; $pf++) {
    $bodyLen = 4 + 8 + 4 + 4 + 4 + 1 + 4 + 4 + 4
    $pkt = New-Object byte[] (2+8+4+4+$bodyLen)
    $pos = 0
    [System.BitConverter]::GetBytes([short]1).CopyTo($pkt, $pos); $pos += 2
    [System.BitConverter]::GetBytes($seq).CopyTo($pkt, $pos); $pos += 8; $seq++
    [System.BitConverter]::GetBytes(140).CopyTo($pkt, $pos); $pos += 4
    [System.BitConverter]::GetBytes($bodyLen).CopyTo($pkt, $pos); $pos += 4
    [System.BitConverter]::GetBytes($playerId).CopyTo($pkt, $pos); $pos += 4
    [System.BitConverter]::GetBytes($pf).CopyTo($pkt, $pos); $pos += 8
    [System.BitConverter]::GetBytes($playerId).CopyTo($pkt, $pos); $pos += 4
    [System.BitConverter]::GetBytes(0.8).CopyTo($pkt, $pos); $pos += 4  # V=0.8
    [System.BitConverter]::GetBytes(0.0).CopyTo($pkt, $pos); $pos += 4  # H=0
    [System.BitConverter]::GetBytes([byte]0).CopyTo($pkt, $pos); $pos += 1
    [System.BitConverter]::GetBytes(1.0).CopyTo($pkt, $pos); $pos += 4
    [System.BitConverter]::GetBytes(1.0).CopyTo($pkt, $pos); $pos += 4
    [System.BitConverter]::GetBytes(1.0).CopyTo($pkt, $pos); $pos += 4
    $udp.Send($pkt, $pkt.Length, $srvUdpEp) | Out-Null
    Start-Sleep -Milliseconds 50
}
Log "Sent $seq total inputs"

Start-Sleep 5
Log "=== Final Frame Receive ==="
for ($i = 0; $i -lt 100; $i++) {
    if ($udp.Available -gt 0) {
        try {
            $remoteEp = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)
            $r = $udp.Receive([ref]$remoteEp)
            $pos = 0
            $type = [System.BitConverter]::ToInt16($r, $pos); $pos += 2
            $nowSeq = -1
            if ($type -eq 1) { $nowSeq = [System.BitConverter]::ToInt64($r, $pos); $pos += 8 }
            $msgId = [System.BitConverter]::ToInt32($r, $pos); $pos += 4
            $bodyLen = [System.BitConverter]::ToInt32($r, $pos); $pos += 4
            
            if ($msgId -eq 101) {
                $frame = [System.BitConverter]::ToInt64($r, $pos); $pos += 8
                $count = [System.BitConverter]::ToInt32($r, $pos); $pos += 4
                $frameCount++
                $msg = "FRAME#$frame count=$count | "
                for ($j = 0; $j -lt $count; $j++) {
                    $pid = [System.BitConverter]::ToInt32($r, $pos); $pos += 4
                    $inv = [System.BitConverter]::ToSingle($r, $pos); $pos += 4
                    $inh = [System.BitConverter]::ToSingle($r, $pos); $pos += 4
                    [System.BitConverter]::ToBoolean($r, $pos); $pos += 1
                    $pos += 12
                    $hp = [System.BitConverter]::ToInt32($r, $pos); $pos += 4
                    $px = [System.BitConverter]::ToSingle($r, $pos); $pos += 4
                    $py = [System.BitConverter]::ToSingle($r, $pos); $pos += 4
                    $pz = [System.BitConverter]::ToSingle($r, $pos); $pos += 4
                    $msg += "P{0}(in={1:F1},{2:F1})@({3:F4},{4:F4},{5:F4}) " -f $pid,$inh,$inh,$px,$py,$pz
                }
                Log $msg
            }
        } catch { }
    }
    Start-Sleep -Milliseconds 80
}
Log "=== TOTAL: $frameCount frames received ==="

$udp.Close()
$server.Kill()
Log "=== TEST COMPLETE ==="
