using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PPBA_Alpaca.Alpaca;

namespace PPBA_Alpaca.Alpaca
{
    internal class SwitchDevice
    {
        private readonly AlpacaController _controller;
        private readonly AppConfig.Settings _config;

        public SwitchDevice(AlpacaController controller, AppConfig.Settings config)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // You can now use _controller and _config in your methods
    }
}


