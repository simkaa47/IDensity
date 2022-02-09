using System;
using System.Collections.Generic;
using System.Text;

namespace IDensity.AddClasses.Settings
{
    class MeasProcSettings:PropertyChangedBase
    {
        
        #region Номер текущего измерительного процесса
        private ushort _num;
        /// <summary>
        /// Номер текущего измерительного процесса
        /// </summary>
        public ushort Num
        {
            get { return _num; }
            set { Set(ref _num, value); }
        }
        #endregion
        #region Номер счетчика
        /// <summary>
        /// Номер счетчика
        /// </summary>
        public Parameter<ushort> MeasProcCounterNum { get; } = new Parameter<ushort>("MeasProcCounterNum", "Номер счетчика", 0, Counters.CountersNum, 0, "hold"); 
        #endregion

    }
}
