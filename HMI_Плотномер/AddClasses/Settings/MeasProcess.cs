using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace IDensity.AddClasses
{
    class MeasProcess : ICloneable
    {  
        #region Количество диапазонов в процессе
        /// <summary>
        /// Количество диапазонов в процессе
        /// </summary>
        public static int rangeNum = 3;
        #endregion

        #region Сведения о диапазонах
        public Diapasone[] Ranges { get; set; } = Enumerable.Range(0, rangeNum).Select(z => new Diapasone()).ToArray();
        #endregion

        #region Номер фоновой стандартизации
        public Parameter<ushort> BackStandNum { get; set; } = new Parameter<ushort>("BackStandNum", "Номер фоновой стандартизации", 0, 3, 0, "");
        #endregion

        #region Длительность измерения одной точки
        public Parameter<ushort> MeasDuration { get; set; } = new Parameter<ushort>("MeasDuration", "Длительность измерения одной точки, *0.1 c.", 0, ushort.MaxValue, 0, "");

        #endregion

        #region Глубина усреднения        
        public Parameter<ushort> MeasDeep { get; set; } = new Parameter<ushort>("MeasDeep", "Глубина усреднения, точек", 1, ushort.MaxValue, 0, "");        
        #endregion

        #region Период полупаспада
        public Parameter<float> HalfLife { get; set; } = new Parameter<float>("HalfLife", "Период полураспада", 0, float.PositiveInfinity, 0, "");

        #endregion

        #region Плотность жидкости        
        public Parameter<float> DensityLiquid { get; set; } = new Parameter<float>("DensityLiquid", "Плотность жидкости", 0, float.PositiveInfinity, 0, "");
        #endregion

        #region Плотность твердости        
        public Parameter<float> DensitySolid { get; set; } = new Parameter<float>("DensitySolid", "Плотность твердости", 0, float.PositiveInfinity, 0, "");        
        #endregion

        #region Номер регистра Modbus, где находится структура данных измерений
        /// <summary>
        /// Номер регистра Modbus, где находится структура данных измерений
        /// </summary>
        public const int ModbRegNum = 27; 
        #endregion

        public object Clone()
        {
            return new MeasProcess
            {
                Ranges = this.Ranges.Select(r => (Diapasone)r.Clone()).ToArray(),
                BackStandNum = this.BackStandNum.Clone() as Parameter<ushort>,
                MeasDuration = this.MeasDuration.Clone() as Parameter<ushort>,
                MeasDeep = this.MeasDeep.Clone() as Parameter<ushort>,
                HalfLife = this.HalfLife.Clone() as Parameter<float>,
                DensityLiquid = this.DensityLiquid.Clone() as Parameter<float>,
                DensitySolid = this.DensitySolid.Clone() as Parameter<float>
            };
        }
    }
}
