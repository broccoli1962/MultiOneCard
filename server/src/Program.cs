namespace OneTable.Gateway
{
    /// <summary>
    /// .NET 8 로컬 웹소켓 게이트웨이 진입점.
    /// 룸코드 입장과 PlayCard 중계만 하고 규칙은 판결하지 않는다.
    /// </summary>
    public static class Program
    {
        /// <summary>로컬 스켈레톤 listen URL. 클라는 ws://127.0.0.1:7777/ws 로 붙는다.</summary>
        public const string ListenUrl = "http://127.0.0.1:7777";

        /// <summary>
        /// 게이트웨이를 연다.
        /// </summary>
        /// <param name="args">프로세스 인자. 사용하지 않는다.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls(ListenUrl);

            var app = builder.Build();
            app.UseWebSockets();

            var gateway = new RoomGateway();
            app.Map("/ws", gateway.AcceptAsync);

            app.Run();
        }
    }
}
