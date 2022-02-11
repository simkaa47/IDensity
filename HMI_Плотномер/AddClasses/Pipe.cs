using System;
using System.Collections.Generic;
using System.Text;

namespace IDensity.AddClasses
{
    /// <summary>
    /// Параметры трубы
    /// </summary>
    class Pipe :PropertyChangedBase
    {
        #region Диаметр трубы
        private float _diameter;
        /// <summary>
        /// Диаметр трубы
        /// </summary>
        public float Diameter
        {
            get { return _diameter; }
            set { Set(ref _diameter, value); }
        } 
        #endregion

    }
}
