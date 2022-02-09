using IDensity.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace IDensity.AddClasses.AnalogInOut
{
    /// <summary>
    /// Настройки входа расходомера
    /// </summary>
    class ConsInputSettings:PropertyChangedBase, ICloneable
    {
        #region Событие
        /// <summary>
        /// Необходимо записать настройки входа расходомера
        /// </summary>
        public event Action<ConsInputSettings> SetConsInputEvent;
        #endregion
        #region Номер аналогового входа
        /// <summary>
        /// Номер аналогового входа
        /// </summary>
        public Parameter<int> InputNum { get; set; } = new Parameter<int>("ConsInputNum", "Номер аналогового входа", 0, 1, 125, "hold");
        #endregion
        #region Коэффициент a
        /// <summary>
        /// Коэффициент a
        /// </summary>
        public Parameter<float> A { get; set; } = new Parameter<float>("ConsA", "Коэффициент a", float.MinValue, float.MaxValue, 127, "hold");
        #endregion
        #region Коэффициент b
        /// <summary>
        /// Коэффициент b
        /// </summary>
        public Parameter<float> B { get; set; } = new Parameter<float>("ConsB", "Коэффициент b", float.MinValue, float.MaxValue, 129, "hold");
        #endregion
        #region Команды
        #region Записать номер входа
        RelayCommand _setAiNumCommand;
        public RelayCommand SetAiNumCommand => _setAiNumCommand ?? (_setAiNumCommand = new RelayCommand(par =>
        {
            var sett = this.Clone() as ConsInputSettings;
            sett.InputNum.Value = InputNum.WriteValue;
            SetConsInputEvent?.Invoke(sett);
        }, par => true));
        #endregion
        #region Записать к-т A
        RelayCommand _setACommand;
        public RelayCommand SetACommand => _setACommand ?? (_setACommand = new RelayCommand(par =>
        {
            var sett = this.Clone() as ConsInputSettings;
            sett.A.Value = A.WriteValue;
            SetConsInputEvent?.Invoke(sett);
        }, par => true));
        #endregion
        #region Записать к-т B
        RelayCommand _setBCommand;
        public RelayCommand SetBCommand => _setBCommand ?? (_setBCommand = new RelayCommand(par =>
        {
            var sett = this.Clone() as ConsInputSettings;
            sett.B.Value = B.WriteValue;
            SetConsInputEvent?.Invoke(sett);
        }, par => true));
        #endregion
        #endregion

        public object Clone()
        {
            return new ConsInputSettings()
            {
                InputNum = this.InputNum.Clone() as Parameter<int>,
                A = this.A.Clone() as Parameter<float>,
                B = this.B.Clone() as Parameter<float>
            };
        }
    }
}
