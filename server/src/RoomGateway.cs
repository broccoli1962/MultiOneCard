using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace OneTable.Gateway
{
    /// <summary>
    /// 6자리 룸코드 입장과 PlayCard JSON 중계. RuleEngine 은 호출하지 않는다.
    /// </summary>
    public sealed class RoomGateway
    {
        public const int MaxSeats = 6;
        public const int ProtocolMajor = 1;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly ConcurrentDictionary<string, Room> _rooms = new ConcurrentDictionary<string, Room>();

        /// <summary>웹소켓 업그레이드 후 룸에 넣고 메시지를 중계한다.</summary>
        public async Task AcceptAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var roomCode = context.Request.Query["room"].ToString();
            if (!TryNormalizeRoomCode(roomCode, out var normalized))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (TryReadInt(context.Request.Query["major"], out var major) && major != ProtocolMajor)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var requestedSeat = TryReadInt(context.Request.Query["seat"], out var seat) ? seat : -1;
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            var room = _rooms.GetOrAdd(normalized, code => new Room(code));
            var session = room.TryJoin(socket, requestedSeat);
            if (session == null)
            {
                await SendAsync(socket, Reject(-1, requestedSeat, requestedSeat < 0 ? "RoomFull" : "SeatTaken"));
                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "join rejected", CancellationToken.None);
                return;
            }

            try
            {
                await room.BroadcastAsync(RoomUpdated(room));
                await ReceiveLoopAsync(room, session);
            }
            finally
            {
                room.Leave(session);
                if (room.OccupiedCount == 0)
                {
                    _rooms.TryRemove(room.Code, out _);
                }
                else
                {
                    await room.BroadcastAsync(PlayerDisconnected(session.Seat));
                }
            }
        }

        private async Task ReceiveLoopAsync(Room room, SeatSession session)
        {
            var buffer = new byte[4096];
            var acc = new MemoryStream();
            while (session.Socket.State == WebSocketState.Open)
            {
                var result = await session.Socket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                acc.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                {
                    continue;
                }

                var json = Encoding.UTF8.GetString(acc.ToArray());
                acc.SetLength(0);
                await HandleCommandAsync(room, session, json);
            }
        }

        private static async Task HandleCommandAsync(Room room, SeatSession session, string json)
        {
            WireCommand? command;
            try
            {
                command = JsonSerializer.Deserialize<WireCommand>(json, JsonOptions);
            }
            catch (JsonException)
            {
                return;
            }

            if (command == null || string.IsNullOrEmpty(command.op))
            {
                return;
            }

            if (command.protocolMajor != 0 && command.protocolMajor != ProtocolMajor)
            {
                await SendAsync(session.Socket, Reject(command.seq, session.Seat, "VersionMismatch"));
                return;
            }

            if (command.op == "PlayCard")
            {
                await room.BroadcastAsync(new WireEvent
                {
                    ev = "CardPlayed",
                    seat = session.Seat,
                    instanceId = command.instanceId,
                    ackSeq = command.seq,
                });
            }
        }

        private static WireEvent RoomUpdated(Room room)
        {
            return new WireEvent
            {
                ev = "RoomUpdated",
                room = room.ToView(),
            };
        }

        private static WireEvent PlayerDisconnected(int seat)
        {
            return new WireEvent
            {
                ev = "PlayerDisconnected",
                seat = seat,
            };
        }

        private static WireEvent Reject(int ackSeq, int seat, string reject)
        {
            return new WireEvent
            {
                ev = "Reject",
                ackSeq = ackSeq,
                seat = seat,
                reject = reject,
            };
        }

        internal static async Task SendAsync(WebSocket socket, WireEvent ev)
        {
            if (socket.State != WebSocketState.Open)
            {
                return;
            }

            var json = JsonSerializer.Serialize(ev, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        internal static bool TryNormalizeRoomCode(string roomCode, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 6)
            {
                return false;
            }

            for (var i = 0; i < 6; i++)
            {
                if (roomCode[i] < '0' || roomCode[i] > '9')
                {
                    return false;
                }
            }

            normalized = roomCode;
            return true;
        }

        private static bool TryReadInt(string? value, out int parsed)
        {
            return int.TryParse(value, out parsed);
        }
    }

    internal sealed class Room
    {
        private readonly object _gate = new object();
        private readonly SeatSession?[] _seats = new SeatSession[RoomGateway.MaxSeats];

        public Room(string code)
        {
            Code = code;
        }

        public string Code { get; }

        public int OccupiedCount
        {
            get
            {
                lock (_gate)
                {
                    var count = 0;
                    for (var i = 0; i < _seats.Length; i++)
                    {
                        if (_seats[i] != null)
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }
        }

        public SeatSession? TryJoin(WebSocket socket, int requestedSeat)
        {
            lock (_gate)
            {
                var seat = requestedSeat;
                if (seat < 0 || seat >= _seats.Length)
                {
                    seat = -1;
                    for (var i = 0; i < _seats.Length; i++)
                    {
                        if (_seats[i] == null)
                        {
                            seat = i;
                            break;
                        }
                    }
                }

                if (seat < 0 || seat >= _seats.Length || _seats[seat] != null)
                {
                    return null;
                }

                var session = new SeatSession(seat, socket);
                _seats[seat] = session;
                return session;
            }
        }

        public void Leave(SeatSession session)
        {
            lock (_gate)
            {
                if (session.Seat >= 0 && session.Seat < _seats.Length && ReferenceEquals(_seats[session.Seat], session))
                {
                    _seats[session.Seat] = null;
                }
            }
        }

        public async Task BroadcastAsync(WireEvent ev)
        {
            SeatSession[] snapshot;
            lock (_gate)
            {
                var live = new List<SeatSession>();
                for (var i = 0; i < _seats.Length; i++)
                {
                    if (_seats[i] != null)
                    {
                        live.Add(_seats[i]!);
                    }
                }

                snapshot = live.ToArray();
            }

            for (var i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    await RoomGateway.SendAsync(snapshot[i].Socket, ev);
                }
                catch (WebSocketException)
                {
                }
            }
        }

        public WireRoom ToView()
        {
            lock (_gate)
            {
                var nicks = new string[MaxOccupiedOrTwo()];
                var ready = new bool[nicks.Length];
                var hostSeat = 0;
                var foundHost = false;
                for (var i = 0; i < _seats.Length && i < nicks.Length; i++)
                {
                    nicks[i] = _seats[i] != null ? "P" + i : string.Empty;
                    if (!foundHost && _seats[i] != null)
                    {
                        hostSeat = i;
                        foundHost = true;
                    }
                }

                return new WireRoom
                {
                    roomCode = Code,
                    phase = "Waiting",
                    nicks = nicks,
                    ready = ready,
                    hostSeat = hostSeat,
                    seatCount = OccupiedUnlocked(),
                };
            }
        }

        private int OccupiedUnlocked()
        {
            var count = 0;
            for (var i = 0; i < _seats.Length; i++)
            {
                if (_seats[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private int MaxOccupiedOrTwo()
        {
            var last = 1;
            for (var i = 0; i < _seats.Length; i++)
            {
                if (_seats[i] != null)
                {
                    last = i;
                }
            }

            return Math.Max(2, last + 1);
        }
    }

    internal sealed class SeatSession
    {
        public SeatSession(int seat, WebSocket socket)
        {
            Seat = seat;
            Socket = socket;
        }

        public int Seat { get; }

        public WebSocket Socket { get; }
    }

    internal sealed class WireCommand
    {
        public string? op { get; set; }
        public int seq { get; set; }
        public int seat { get; set; }
        public int protocolMajor { get; set; }
        public int protocolMinor { get; set; }
        public int instanceId { get; set; }
        public string? roomCode { get; set; }
    }

    internal sealed class WireEvent
    {
        public string ev { get; set; } = string.Empty;
        public int seq { get; set; }
        public int ackSeq { get; set; }
        public int seat { get; set; }
        public int instanceId { get; set; }
        public string? reject { get; set; }
        public WireRoom? room { get; set; }
    }

    internal sealed class WireRoom
    {
        public string roomCode { get; set; } = string.Empty;
        public string phase { get; set; } = "Waiting";
        public string[] nicks { get; set; } = Array.Empty<string>();
        public bool[] ready { get; set; } = Array.Empty<bool>();
        public int hostSeat { get; set; }
        public int seatCount { get; set; }
    }
}
