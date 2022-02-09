using System;
using System.Collections.Generic;
using System.Text;

namespace IDensity.AddClasses.Settings
{
    class StandSettings
    {
        public Parameter<ushort> StandMeasUnitNum { get; } = new Parameter<ushort>("StandMeasUnitNum", "Номер единицы измерения", 0, 5, 0, "hold");
    }
}
