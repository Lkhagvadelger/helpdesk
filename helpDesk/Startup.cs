using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(helpDesk.Startup))]
namespace helpDesk
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
