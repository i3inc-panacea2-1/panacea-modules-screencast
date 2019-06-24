using Panacea.Core;
using Panacea.Modularity.ScreenCast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Modules.ScreenCast
{
    class ScreenCastPlugin : IScreenCastPlugin
    {
        IScreenCastPlayer ScreenCastPlayer;
        PanaceaServices _core;
        public ScreenCastPlugin(PanaceaServices core)
        {
            _core = core;
        }
        public IScreenCastPlayer GetScreenCastPlayer()
        {
            return ScreenCastPlayer;
        }

        public Task BeginInit()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            return;
        }

        public Task EndInit()
        {
            ScreenCastPlayer = new ScreenCastPlayer(_core);
            return Task.CompletedTask;
        }


        public Task Shutdown()
        {
            return Task.CompletedTask;
        }
    }
}
