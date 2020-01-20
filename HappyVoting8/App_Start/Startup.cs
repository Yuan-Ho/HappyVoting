using Microsoft.Owin;
using Owin;

namespace HappyVoting8
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
} 