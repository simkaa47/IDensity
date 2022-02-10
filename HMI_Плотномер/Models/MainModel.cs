using IDensity.AddClasses;
using IDensity.AddClasses.AdcBoardSettings;
using IDensity.AddClasses.AnalogInOut;
using IDensity.AddClasses.Standartisation;
using IDensity.Models.XML;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace IDensity.Models
{
    class MainModel : PropertyChangedBase
    {
        #region Количество наборов стандартизаций
        /// <summary>
        /// Количество стандартизаций
        /// </summary>
        public static readonly int CountStand = 12;
        #endregion

        #region Количество счетчиков
        /// <summary>
        /// Количество счетчиков
        /// </summary>
        public static readonly int CountCounters = 8;
        #endregion

        #region Количетво калибровочных кривых
        /// <summary>
        /// Количетво калибровочных кривых
        /// </summary>
        public static int CalibCurveNum = 6;
        #endregion
        public MainModel()
        {
            Init();// Инициализация параметров
            CalibrDataDescribe();
            AdcSettingsEventDescribe();
            MeasUnitSettingsDescribe();
            ConsInputSettings.SetConsInputEvent += SetConsInputSettings;
        }


        #region События
        #region Обновились данные
        public event Action UpdateDataEvent;
        #endregion
        #endregion

        #region Спсоб соединения с платой
        public CommMode CommMode { get; private set; }
        #endregion

        #region RTC контроллера платы
        public Parameter<DateTime> Rtc { get; } = new Parameter<DateTime>("Rtc", "Часы реального времени", DateTime.MinValue, DateTime.MaxValue, 19, "hold");
        #endregion

        #region Статус соединения с платой       
        /// <summary>
        /// Статус соединения с платой
        /// </summary>
        public Parameter<bool> Connecting { get; } = new Parameter<bool>("ConnectBoard", "Статус соединения с платой", false, true, 0, "");

        #endregion

        #region Данные измерения
        #region ФВ усредненное по диапазонам мгновенное
        public Parameter<float> PhysValueCur { get; } = new Parameter<float>("PhysValueCur", "ФВ усредненное по диапазонам мгновенное", 0, float.PositiveInfinity, 0, "read");

        #endregion

        #region ФВ усредненное по диапазонам усредненное
        public Parameter<float> PhysValueAvg { get; } = new Parameter<float>("PhysValueAvg", "ФВ усредненное по диапазонам усредненное", 0, float.PositiveInfinity, 2, "read");
        #endregion

        #region Значения стандартизаций, скорректированных по времени
        public Parameter<float>[] StandHalfPeriodValues { get; } = Enumerable.Range(0, 3).Select(i => new Parameter<float>("StandHalfPeriodValue" + i.ToString(), $"Значение стандартизации, скорректированное по времени  {i}", float.NegativeInfinity, float.PositiveInfinity, 51 + i * 2, "read")).ToArray();
        #endregion
        #region Возвраст
        public Parameter<float>[] StandHalfPeriodAges { get; } = Enumerable.Range(0, 3).Select(i => new Parameter<float>("StandHalfPeriodAge" + i.ToString(), $"Возраст стандартизации  {i}", float.NegativeInfinity, float.PositiveInfinity, 57 + i * 2, "read")).ToArray();
        #endregion

        #region Текущие значения счетчиков
        public Parameter<float>[] CountersCur = Enumerable.Range(0, 3).Select(i => new Parameter<float>("CounterCur" + i.ToString(), "Текущее значение счетчика " + i.ToString(), float.NegativeInfinity, float.PositiveInfinity, 31 + i * 2, "read")).ToArray();
        #endregion

        #region Концентрация мгновенная
        public Parameter<float> ContetrationValueCur { get; } = new Parameter<float>("ContetrationValueCur", "Мгновенное значение концентрации", 0, float.PositiveInfinity, 27, "read");
        #endregion

        #region Концентрация усредненная
        public Parameter<float> ContetrationValueAvg { get; } = new Parameter<float>("ContetrationValueAvg", "Усреднненное значение концентрации", 0, float.PositiveInfinity, 29, "read");
        #endregion

        #region Общий массовый расход
        public Parameter<float> ConsMassCommon { get; } = new Parameter<float>("ConsMassCommon", "Общий массовый расход", float.MinValue, float.MaxValue, 39, "read");
        #endregion
        #region Массовый расход твердого
        public Parameter<float> ConsMassSolid { get; } = new Parameter<float>("ConsMassSolid", "Массовый расход твердого", float.MinValue, float.MaxValue, 41, "read");
        #endregion

        #region Статус циклических измерений
        public Parameter<bool> CycleMeasStatus { get; } = new Parameter<bool>("CycleMeasStatus", "Статус циклических измерений", false, true, 1, "hold");
        #endregion

        #region Данные тлеметрии HV
        public HvTelemetry TelemetryHV { get; } = new HvTelemetry();
        #endregion        

        #region Данные телеметрии платы питания(темпратуры)
        public TempBoardTelemetry TempTelemetry { get; } = new TempBoardTelemetry();
        #endregion

        #region Номер текущкго измерительного процесса
        public Parameter<ushort> CurMeasProcessNum { get; } = new Parameter<ushort>("CurMeasProcessNum", "Номер текущего измерительного процесса", 0, (ushort)MainModel.measProcessNum, 25, "hold");
        #endregion

        #region Статус связи с платой АЦП
        public Parameter<bool> AdcBoardCommState { get; } = new Parameter<bool>("AdcBoardCommState", "Статус связи с платой АЦП", false, true, 0, "");
        #endregion

        #region Переменные связи
        public Parameter<ushort> CommStates { get; } = new Parameter<ushort>("CommStates", "Переменные связи", ushort.MinValue, ushort.MaxValue, 63, "read")
        {
            OnlyRead = true
        };
        #endregion

        #region Битовые переменные состояния аналоговых модулей
        public Parameter<ushort>[] AnalogStateGroups { get; } = new Parameter<ushort>[]
        {
            new Parameter<ushort>("AnalogStateGroup1", "Состояние аналоговых модулей группы 1", 0, ushort.MaxValue, 65, "read"),
            new Parameter<ushort>("AnalogStateGroup2", "Состояние аналоговых модулей группы 2", 0, ushort.MaxValue, 66, "read")
         };
        #endregion



        #endregion

        #region Настройки в плате
        #region Настройки платы АЦП
        public AdcParameters AdcBoard { get; } = new AdcParameters();
        #endregion

        #region Настройки аналоговых модулей
        AnalogGroup[] _analogGroups;
        public AnalogGroup[] AnalogGroups
        {
            get
            {
                if (_analogGroups == null)
                {
                    _analogGroups = Enumerable.Range(0, 2).
                        Select(z =>
                        {
                            var gr = new AnalogGroup(z);
                            gr.AI.SwitchPwrEvent += SwitchAm;
                            gr.AI.ChangeSettCallEvent += ChangeAdcAct;
                            gr.AO.SwitchPwrEvent += SwitchAm;
                            gr.AO.SetTestValueCallEvent += SetTestValueAm;
                            gr.AO.ChangeSettCallEvent += ChangeDacAct;
                            return gr;
                        }).ToArray();

                }
                return _analogGroups;
            }
        }
        #endregion       

        #region Настройки измерительных процессов
        #region Количество измерительных процессов
        /// <summary>
        /// Количество измерительных процессов
        /// </summary>
        public static int measProcessNum = 4;
        #endregion

        #region Текущий процесс
        MeasProcess _curMeasProcess = new MeasProcess();
        public MeasProcess CurMeasProcess { get => _curMeasProcess; set => Set(ref _curMeasProcess, value); }
        #endregion

        #region Настройки измерительных процессов
        public MeasProcess[] MeasProcesses { get; set; } = Enumerable.Range(0, measProcessNum).Select(z => new MeasProcess()).ToArray();
        #endregion

        #endregion

        #region Данные стандартизаций

        public StandData[] StandSettings { get; } = Enumerable.Range(0, 12).Select(i => new StandData()).ToArray();
        #endregion

        #region Данные счетчиков        
        public CountDiapasone[] CountDiapasones { get; } = Enumerable.Range(0, CountCounters).Select(i => new CountDiapasone()).ToArray();
        #endregion

        #region Данные калибровочных кривых        
        public CalibrData[] CalibrDatas { get; } = Enumerable.Range(0, CalibCurveNum).Select(i => new CalibrData()).ToArray();
        void CalibrDataDescribe()
        {
            foreach (var calibr in CalibrDatas)
            {
                calibr.SetSettingsCommandEvent += SetCalibrData;
            }
        }
        #endregion

        #region Параметры полседовательного порта платы
        #region Баудрейт
        public Parameter<uint> PortBaudrate { get; } = new Parameter<uint>("PortBaudrate", "Скорость передачи данных", 1200, 115200, 48, "hold");
        #endregion

        #region Режим работы последовательного порта
        public Parameter<ushort> PortSelectMode { get; } = new Parameter<ushort>("PortSelectMode", "Режим работы последовательного порта", 0, 1, 50, "hold");
        #endregion
        #endregion

        #region Настройки едениц измерений
        public MeasUnitSettings[] MeasUnitSettings { get; } = Enumerable.Range(0, 5).Select(i => new MeasUnitSettings()).ToArray();
        void MeasUnitSettingsDescribe()
        {
            foreach (var sett in MeasUnitSettings)
            {
                sett.Writing += SetMeasUnitsSettings;
            }
        }
        #endregion

        #region Настройки платы АЦП

        #region Адрес UDP приемника        
        private string _udpAddrString = "0.0.0.0";
        /// <summary>
        /// Адрес UDP приемника
        /// </summary>
        public string UdpAddrString
        {
            get { return _udpAddrString; }
            set
            {
                if (CheckIp(value))
                {
                    Set(ref _udpAddrString, value);
                }
            }
        }

        #region Метод проверки корректности ip
        /// <summary>
        /// Проверка корректности ip
        /// </summary>
        /// <param name="ip">Проверяемая строка</param>
        /// <returns>true, если ip корректно</returns>
        public static bool CheckIp(string ip)
        {
            var arrStr = ip.Split(".");
            int temp = 0;
            if (arrStr.Length != 4) return false;
            foreach (var item in arrStr)
            {
                if (!int.TryParse(item, out temp)) return false;
                if ((temp < 0) || (temp > 255)) return false;
            }
            return true;
        }
        #endregion
        #endregion

        #region Настройки платы АЦП
        public AdcBoardSettings AdcBoardSettings { get; } = new AdcBoardSettings();
        #region Подписка на события класса AdcBoardSettings
        /// <summary>
        /// Подписка на события класса AdcBoardSettings
        /// </summary>
        void AdcSettingsEventDescribe()
        {
            AdcBoardSettings.SettingsChangedEvent += SetAdcBoardSettings;            
        }
        #endregion
        #endregion


        #endregion

        #region Настройки входа расходомера
        public ConsInputSettings ConsInputSettings { get; } = new ConsInputSettings();
        
        #endregion


        #region Данные прочитаны
        public bool SettingsReaded { get; set; }
        #endregion 
        #endregion

        public RS485 rs { get; private set; }
        public TCP Tcp { get; private set; }

        public async void ModelProcess()
        {
            await Task.Run(() => MainProcess());
        }

        void MainProcess()
        {
            while (true)
            {
                if (CommMode.RsEnable) rs?.GetData(this);
                else if (CommMode.EthEnable) Tcp?.GetData(this);
                else Connecting.Value = false;
                if (CycleMeasStatus.Value && Connecting.Value)
                    UpdateDataEvent?.Invoke();                
                Thread.Sleep(100);
            }
        }

        #region Инициализация
        void Init()
        {
            rs = ClassInit<RS485>();
            Tcp = ClassInit<TCP>();
            CommMode = ClassInit<CommMode>();
            CycleMeasStatus.PropertyChanged += (obj, args) =>
            {
                if (args.PropertyName == "Value")
                {
                    PhysValueAvg.Value = 0;
                    ContetrationValueAvg.Value = 0;
                    ContetrationValueCur.Value = 0;
                    PhysValueCur.Value = 0;
                    UpdateDataEvent?.Invoke();                    
                }
            };
            Connecting.PropertyChanged += (obj, args) => SettingsReaded = false;
           
        }

        T ClassInit<T>() where T : PropertyChangedBase
        {
            T cell = XmlMethods.GetParam<T>().FirstOrDefault();
            if (cell == null)
            {
                cell = (T)Activator.CreateInstance(typeof(T));
                XmlMethods.AddToXml<T>(cell);
            }
            cell.PropertyChanged += (sender, e) => XmlMethods.EditParam<T>(cell, e.PropertyName);
            return cell;
        }
        #endregion

        #region Парсинг битовых значений устройств
        public void GetDeviceData()
        {
            AdcBoard.CommState.Value = (CommStates.Value & 1) == 0;
            TempTelemetry.TempBoardCommState.Value = (CommStates.Value & 2) == 0;
            TelemetryHV.HvCommState.Value = (CommStates.Value & 4) == 0;            
            for (int i = 0; i < 2; i++)
            {
                AnalogGroups[i].AO.PwrState.Value = (AnalogStateGroups[i].Value & 1) != 1;
                AnalogGroups[i].AI.PwrState.Value = (AnalogStateGroups[i].Value & 2) != 2;
                AnalogGroups[i].AO.CommState.Value = (AnalogStateGroups[i].Value & 4) != 4;
                AnalogGroups[i].AI.CommState.Value = (AnalogStateGroups[i].Value & 8) != 8;
                
            }            
        }
        #endregion

        #region Команды
        #region Старт-стоп циклических измерений        
        public async void SwitchMeas()
        {
            var value = CycleMeasStatus.Value ? 0 : 1;
            if (value > 0 && !TelemetryHV.HvOn.Value)
            {

                SwitchHv();
                await Task.Run(() =>
                {
                    while (Connecting.Value && !TelemetryHV.HvOn.Value)
                    {
                        Thread.Sleep(100);
                    }
                });
            }

            if (CommMode.EthEnable) Tcp.SwitchMeas(value);
            else if (CommMode.RsEnable) rs.SwitchMeas(value);
        }
        #endregion

        #region Включитть-выключить HV 
        public void SwitchHv()
        {
            var value = TelemetryHV.HvOn.Value ? 0 : 1;
            if (CommMode.EthEnable) Tcp.SwitchHv(value);
            else if (CommMode.RsEnable) rs.SwitchHv(value);
        }
        #endregion

        #region Настройки измерительных процессов

        #region Записать данные измерительных процессов
        public void SetMeasProcessSettings(MeasProcess process, int index)
        {
            if (CommMode.EthEnable) Tcp.SetMeasProcessSettings(process,index);
            else if((CommMode.RsEnable))rs.SetMeasProcessSettings(process, index);
        }

        #endregion

        #region Сменить номер измерительного процесса
        public void ChangeMeasProcess(int index)
        {
            if (CommMode.EthEnable) Tcp.ChangeMeasProcess(index);
            else if (CommMode.RsEnable) rs.ChangeMeasProcess(index);
        }
        #endregion
        #endregion

        #region Команды настроек последовательного порта
        #region Записать бадрейт
        public void ChangeBaudrate(uint value)
        {
            if (CommMode.EthEnable) Tcp.ChangeBaudrate(value);
            else if ((CommMode.RsEnable)) rs.ChangeBaudrate(value);
        }
        #endregion       

        #region Изменить режим работы последовательного порта
        public void ChangeSerialSelect(int value)
        {
            ushort temp = (ushort)(value > 0 ? 1 : 0);
            if (CommMode.EthEnable) Tcp.ChangeSerialSelect(temp);
            else if ((CommMode.RsEnable)) rs.ChangeSerialSelect(temp);
            
        }
        #endregion
        #endregion

        #region Установить дату-время
        public void SetRtc(DateTime dt)
        {
            if (CommMode.EthEnable) Tcp.SetRtc(dt);
            else if (CommMode.RsEnable) rs.SetRtc(dt);
        }

        #endregion

        #region Установить напряжение HV
        public void SetHv(ushort value)
        {
            if (CommMode.EthEnable) Tcp.SetHv(value);
            else if (CommMode.RsEnable) rs.SetHv(value);
        }
        #endregion

        #region Управление питанием аналоговых модулей
        void SwitchAm(int groupNum, int moduleNum, bool value)
        {
            if (CommMode.EthEnable) Tcp.SwitchAm(groupNum, moduleNum, value);
            else if (CommMode.RsEnable) rs.SwitchAm(groupNum, moduleNum, value);
        }
        #endregion

        #region Отправить значение тестовой величины
        void SetTestValueAm(int groupNum, int moduleNum, ushort value)
        {
            if (CommMode.EthEnable) Tcp.SetTestValueAm(groupNum, moduleNum, value);
            else if (CommMode.RsEnable) rs.SetTestValueAm(groupNum, moduleNum, value);
        }
        #endregion

        #region Команда "Изменить активность аналогового выхода"
        void ChangeDacAct(int groupNum, int moduleNum, AnalogOutput value)
        {
            if (CommMode.EthEnable) Tcp.SendAnalogOutSwttings(groupNum, moduleNum, value);
            else if (CommMode.RsEnable) rs.SendAnalogOutSwttings(groupNum, moduleNum, value);
        }
        #endregion

        #region Команда "Изменить активность аналогового входа"
        void ChangeAdcAct(int groupNum, int moduleNum, AnalogInput value)
        {
            if (CommMode.EthEnable) Tcp.SendAnalogInSwttings(groupNum, moduleNum, value);
            else if (CommMode.RsEnable) rs.SendAnalogInSwttings(groupNum, moduleNum, value);
        }
        #endregion

        #region Команда "Записать настройки стандартизации"
        public void WriteStdSettings(ushort index, StandData stand)
        {
            if (CommMode.EthEnable) Tcp.WriteStdSettings(index, stand);
            else if (CommMode.RsEnable) rs.WriteStdSettings(index, stand);
        }
        #endregion

        #region Команда "Произвести стандартизацию"
        /// <summary>
        /// Произвести стандартизацию
        /// </summary>
        /// <param name="index">Номер набора стандартизации</param>
        public void MakeStand(int index)
        {
            if (CommMode.EthEnable) Tcp.MakeStand(index);
            else if (CommMode.RsEnable) rs.MakeStand(index);
        }
        #endregion

        #region Команда принудиельного запроса набора стандартизации после стандартизации
        public void GetStdSelection(ushort index)
        {
            if (CommMode.EthEnable) Tcp.GetStdSel(index);
            else if (CommMode.RsEnable) rs.GetStdSelection(index);
        }
        #endregion

        #region Команда "Записать настройки счечиков"
        public void WriteCounterSettings(CountDiapasone diapasone)
        {
            if (diapasone.Num.Value < MainModel.CountStand)
            {
                if (CommMode.EthEnable) Tcp.WriteCounterSettings(diapasone);
                else if (CommMode.RsEnable) rs.WriteCounterSettings(diapasone);
            }
        }
        #endregion

        #region Команда "Записать данные калибровочных кривых"
        public void SetCalibrData(CalibrData calibrData)
        {
            if (CommMode.EthEnable) Tcp.SetCalibrData(calibrData);
            else if (CommMode.RsEnable) rs.SetCalibrData(calibrData);
        }
        #endregion

        #region Команда "Поменять UDP адрес источника"
        public void SetUdpAddr(byte[] addr)
        {
           if (CommMode.EthEnable) Tcp.SetUdpAddr(addr);
           else if (CommMode.RsEnable) rs.SetUdpAddr(addr);
        }

        #endregion

        #region Команды изменения настроек платы АЦП
        public void SetAdcBoardSettings(AdcBoardSettings settings)
        {
            if (CommMode.EthEnable) Tcp.SetAdcBoardSettings(settings);
            else if (CommMode.RsEnable) rs.SetAdcBoardSettings(settings);
        }
        #endregion

        #region Команда "Запуск-останов платы АЦП"
        public void SwitchAdcBoard(ushort value)
        {
            if (CommMode.EthEnable) Tcp.SwitchAdcBoard(value);
            else if (CommMode.RsEnable) rs.SwitchAdcBoard(value);
        }
        #endregion

        #region Команда "Запуск/останов выдачи данных АЦП "
        public void StartStopAdcData(ushort value)
        {
            if (CommMode.EthEnable) Tcp.StartStopAdcData(value);
            else if (CommMode.RsEnable) rs.StartStopAdcData(value);
        }
        #endregion

        #region Команда "Произвести еденичное измеренине"
        public void MakeSingleMeasure(ushort time)
        {
            if (CommMode.EthEnable) Tcp.MakeSingleMeasure(time);
            else if (CommMode.RsEnable) rs.MakeSingleMeasure(time);
        }
        #endregion

        #region Команда "Записать настройки едениц измерерия"
        public void SetMeasUnitsSettings(MeasUnitSettings settings)
        {
            if (CommMode.EthEnable) Tcp.SetMeasUnitsSettings(settings);
            else if (CommMode.RsEnable) rs.SetMeasUnitsSettings(settings);
        }

        #endregion

        #region Команда "Переключить реле"
        public void SwitchRelay(ushort value)
        {
            if (CommMode.EthEnable) Tcp.SwitchRelay(value);
            //else if (CommMode.RsEnable) rs.SetMeasUnitsSettings(settings);
        }
        #endregion

        #region Команда "Записать настройки входа расходомера"
        public void SetConsInputSettings(ConsInputSettings settings)
        {
            if (CommMode.EthEnable) Tcp.SetConsInputSettings(settings);
           else if (CommMode.RsEnable) rs.SetConsInputSettings(settings);
        }
        #endregion
        #endregion

    }
}
