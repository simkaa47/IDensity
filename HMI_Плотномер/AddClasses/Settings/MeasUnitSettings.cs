using IDensity.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace IDensity.AddClasses
{
    /// <summary>
    /// Класс настроек едениц измерения
    /// </summary>
    class MeasUnitSettings:ICloneable
    {
        #region Номер еденицы измерения
        public Parameter<ushort> Id { get; set; } = new Parameter<ushort>("MeasUnitSettNum", "Номер еденицы измерения", 0, 4, 120, "hold");
        #endregion
        #region Коэффициент А
        /// <summary>
        /// Коэффициент А
        /// </summary>
        public Parameter<float> A { get; set; } = new Parameter<float>("MeasUnitA", "Коэффициент A", float.NegativeInfinity, float.PositiveInfinity, 121, "hold");
        #endregion
        #region Коэффициент B
        /// <summary>
        /// Коэффициент B
        /// </summary>
        public Parameter<float> B { get; set; } = new Parameter<float>("MeasUnitB", "Коэффициент B", float.NegativeInfinity, float.PositiveInfinity, 123, "hold");
        #endregion
        public object Clone()
        {
            return new MeasUnitSettings
            {
                Id = this.Id.Clone() as Parameter<ushort>,
                A = this.A.Clone() as Parameter<float>,
                B = this.B.Clone() as Parameter<float>,
            };
        }
        #region Событие
        public event Action<MeasUnitSettings> Writing;
        #endregion

        #region Команды
        RelayCommand _setACommand;
        public RelayCommand SetACommand => _setACommand ?? (_setACommand = new RelayCommand(execPar => {
            var sett = this.Clone() as MeasUnitSettings;
            sett.A.Value = A.WriteValue;
            Writing?.Invoke(sett);
        
        }, canExecPar => true));

        RelayCommand _setBCommand;
        public RelayCommand SetBCommand => _setBCommand ?? (_setBCommand = new RelayCommand(execPar => {
            var sett = this.Clone() as MeasUnitSettings;
            sett.B.Value = B.WriteValue;
            Writing?.Invoke(sett);

        }, canExecPar => true));
        #endregion

    }
}
