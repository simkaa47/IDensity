using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using IDensity.AddClasses;
using IDensity.Models;
using IDensity.Models.SQL;
using IDensity.Models.XML;
using IDensity.ViewModels.Commands;
using System.Timers;
using IDensity.AddClasses.EventHistory;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Controls;
using IDensity.AddClasses.Standartisation;

namespace IDensity.ViewModels
{
    /// <summary>
    /// Общая ViewModel для окна
    /// </summary>
    class VM : PropertyChangedBase
    {
        #region Пользователи

        #region Текущий пользователь
        User _curUser;
        public User CurUser { get => _curUser; set => Set(ref _curUser, value); }
        #endregion


        #region Коллекция пользователей
        public DataBaseCollection<User> Users { get; } = new DataBaseCollection<User>("Users", new User { Login = "admin", Password = "0000" });
        #endregion
        #endregion

        #region Команды

        #region Обновить список доступных Com портов
        RelayCommand _updateComPortListCommand;
        public RelayCommand UpdateComPortListCommand => _updateComPortListCommand ?? (_updateComPortListCommand = new RelayCommand(o => ComPorts = SerialPort.GetPortNames(), o => true));
        #endregion

        #region Команда "Закрыть приложение"
        RelayCommand _closeAppCommand;
        public RelayCommand CloseAppCommand => _closeAppCommand ?? (_closeAppCommand = new RelayCommand(o =>
        {
            Udp?.Stop();
            Application.Current.Shutdown();
        }, o => true));
        #endregion

        #region Команда вкл-выкл напряжение
        RelayCommand _switchHvCommand;
        public RelayCommand SwitchHvCommand { get => _switchHvCommand ?? (_switchHvCommand = new RelayCommand(o => mainModel.SwitchHv(), o => true)); }
        #endregion

        #region Команда вкл-выкл измерения
        RelayCommand _switchMeasCommand;
        public RelayCommand SwitchMeasCommand { get => _switchMeasCommand ?? (_switchMeasCommand = new RelayCommand(o => mainModel.SwitchMeas(), o => true)); }
        #endregion

        #region Команда плказать архивный тренд
        RelayCommand _showArchivalTrendCommand;
        public RelayCommand ShowArchivalTrendCommand { get => _showArchivalTrendCommand ?? (_showArchivalTrendCommand = new RelayCommand(o => ShowArchivalTrend(), o => true)); }
        #endregion

        #region Команда "Логаут"
        RelayCommand _logoutCommand;
        public RelayCommand LogoutCommand { get => _logoutCommand ?? (_logoutCommand = new RelayCommand(o => CurUser = null, o => true)); }
        #endregion

        #region Команда "Записать архивный тренд в файл"
        RelayCommand _writeLogCommand;
        public RelayCommand WriteLogCommand { get => _writeLogCommand ?? (_writeLogCommand = new RelayCommand(o => WriteArchivalTrendToText(), o => true)); }
        #endregion

        #region Команды настроек измерительных процессов
        #region Команда "Записать номер калибровочной кривой в диапазоне"
        RelayCommand _setCalibCurveDiapCommand;
        public RelayCommand SetCalibCurveDiapCommand
        {
            get => _setCalibCurveDiapCommand ?? (_setCalibCurveDiapCommand = new RelayCommand(execPar =>
            {
                MeasProcess process = SelectedMeasProcess.Clone() as MeasProcess;
                process.Ranges[SelectedDiapNum].CalibCurveNum.Value = process.Ranges[SelectedDiapNum].CalibCurveNum.WriteValue;
                mainModel.SetMeasProcessSettings(process, SelectedMeasProcessNum);
            },
                canExecPar => true));

        }
        #endregion
        #region Команда "Записать номер стандартизации в диапазоне"
        RelayCommand _setStandNumDiapCommand;
        public RelayCommand SetStandNumDiapCommand
        {
            get => _setStandNumDiapCommand ?? (_setStandNumDiapCommand = new RelayCommand(execPar =>
            {
                MeasProcess process = SelectedMeasProcess.Clone() as MeasProcess;
                process.Ranges[SelectedDiapNum].StandNum.Value = process.Ranges[SelectedDiapNum].StandNum.WriteValue;
                mainModel.SetMeasProcessSettings(process, SelectedMeasProcessNum);
            },
                canExecPar => true));

        }
        #endregion
        #region Команда "Записать счетчик в диапазоне"
        RelayCommand _setCounterDiapCommand;
        public RelayCommand SetCounterDiapCommand
        {
            get => _setCounterDiapCommand ?? (_setCounterDiapCommand = new RelayCommand(execPar =>
            {
                MeasProcess process = SelectedMeasProcess.Clone() as MeasProcess;
                process.Ranges[SelectedDiapNum].CounterNum.Value = process.Ranges[SelectedDiapNum].CounterNum.WriteValue;
                mainModel.SetMeasProcessSettings(process, SelectedMeasProcessNum);
            },
                canExecPar => true));

        }
        #endregion
        #region Команда "Записать номер стандартизации"
        RelayCommand _setStandNumCommand;
        public RelayCommand SetStandNumCommand
        {
            get => _setStandNumCommand ?? (_setStandNumCommand = new RelayCommand(execPar =>
            {
                MeasProcess process = SelectedMeasProcess.Clone() as MeasProcess;
                process.BackStandNum.Value = process.BackStandNum.WriteValue;
                mainModel.SetMeasProcessSettings(process, SelectedMeasProcessNum);
            },
                canExecPar => true));
        }
        #endregion
        #region Команда "Записать время измерения одной точки"
        RelayCommand _setMeasDurationCommand;
        public RelayCommand SetMeasDurationCommand
        {
            get => _setMeasDurationCommand ?? (_setMeasDurationCommand = new RelayCommand(execPar =>
            {
                MeasProcess process = SelectedMeasProcess.Clone() as MeasProcess;
                process.MeasDuration.Value = process.MeasDuration.WriteValue;
                mainModel.SetMeasProcessSettings(process, SelectedMeasProcessNum);
            },
                canExecPar => true));
        }
        #endregion
        #region Команда "Записать глубину усреднения"
        RelayCommand _setMeasDeepCommand;
        public RelayCommand SetMeasDeepCommand
        {
            get => _setMeasDeepCommand ?? (_setMeasDeepCommand = new RelayCommand(execPar =>
            {
                MeasProcess process = SelectedMeasProcess.Clone() as MeasProcess;
                process.MeasDeep.Value = process.MeasDeep.WriteValue;
                mainModel.SetMeasProcessSettings(process, SelectedMeasProcessNum);
            },
                canExecPar => true));
        }
        #endregion
        #region Команда "Отправить данные периода-полураспада"
        RelayCommand _setHalfLifeCommand;
        public RelayCommand SetHalfLifeCommand {
            get => _setHalfLifeCommand ?? (_setHalfLifeCommand = new RelayCommand(execPar =>
            {
                MeasProcess process = SelectedMeasProcess.Clone() as MeasProcess;
                process.HalfLife.Value = process.HalfLife.WriteValue;
                mainModel.SetMeasProcessSettings(process, SelectedMeasProcessNum);
            },
                canExecPar => true)); }
        #endregion
        #region Команда "Отправить данные плотности жидкости"
        RelayCommand _setDensityLiqCommand;
        public RelayCommand SetDensityLiqCommand
        {
            get => _setDensityLiqCommand ?? (_setDensityLiqCommand = new RelayCommand(execPar =>
            {
                MeasProcess process = SelectedMeasProcess.Clone() as MeasProcess;
                process.DensityLiquid.Value = process.DensityLiquid.WriteValue;
                mainModel.SetMeasProcessSettings(process, SelectedMeasProcessNum);
            },
                canExecPar => true));
        }
        #endregion
        #region Команда "Отправить данные плотности твердости"
        RelayCommand _setDensitySolidCommand;
        public RelayCommand SetDensitySolidCommand
        {
            get => _setDensitySolidCommand ?? (_setDensitySolidCommand = new RelayCommand(execPar =>
            {
                MeasProcess process = SelectedMeasProcess.Clone() as MeasProcess;
                process.DensitySolid.Value = process.DensitySolid.WriteValue;
                mainModel.SetMeasProcessSettings(process, SelectedMeasProcessNum);
            },
                canExecPar => true));
        }
        #endregion
        #region Команда "Сменить номер измерительного процесса"
        RelayCommand _changeMeasProcessCommand;
        public RelayCommand ChangeMeasProcessCommand => _changeMeasProcessCommand ?? (_changeMeasProcessCommand = new RelayCommand(execPar => mainModel.ChangeMeasProcess((int)execPar), canExecPar => canExecPar != null));
        #endregion

        #endregion

        #region Команды стандартизации

        #region Команды записи  настроек стандартизации
        #region Команда "Записать длительность стандартизации"
        RelayCommand _setStdDurationCommand;
        /// <summary>
        /// Команда "Записать длительность стандартизации"
        /// </summary>
        public RelayCommand SetStdDurationCommand => _setStdDurationCommand ?? (_setStdDurationCommand = new RelayCommand(execPar =>
        {
            StandData stand = SelectedStandData.Clone() as StandData;
            stand.Duration.Value = SelectedStandData.Duration.WriteValue;
            mainModel.WriteStdSettings((ushort)StandSelNum, stand);
        }, canExecPar => true));
        #endregion
        #region Команда "Записать тип стандартизации"
        RelayCommand _setStdTypeCommand;
        /// <summary>
        /// Команда "Записать тип стандартизации"
        /// </summary>
        public RelayCommand SetStdTypeCommand => _setStdTypeCommand ?? (_setStdTypeCommand = new RelayCommand(execPar =>
        {
            StandData stand = SelectedStandData.Clone() as StandData;
            stand.Type.Value = SelectedStandData.Type.WriteValue;
            mainModel.WriteStdSettings((ushort)StandSelNum, stand);
        }, canExecPar => true));
        #endregion
        #region Команда "Записать физ. величину стандартизации"
        RelayCommand _setStdValueCommand;
        /// <summary>
        /// Команда "Записать физ. величину стандартизации"
        /// </summary>
        public RelayCommand SetStdValueCommand => _setStdValueCommand ?? (_setStdValueCommand = new RelayCommand(execPar =>
        {
            StandData stand = SelectedStandData.Clone() as StandData;
            stand.Value.Value = SelectedStandData.Value.WriteValue;
            mainModel.WriteStdSettings((ushort)StandSelNum, stand);
        }, canExecPar => true));
        #endregion
        #region Команда "Записать результат стандартизации"
        RelayCommand _setStdResultCommand;
        /// <summary>
        /// Команда "Записать результат стандартизации"
        /// </summary>
        public RelayCommand SetStdResultCommand => _setStdResultCommand ?? (_setStdResultCommand = new RelayCommand(execPar =>
        {
            int index = 0;
            if (execPar != null) int.TryParse(execPar.ToString(), out index);
            StandData stand = SelectedStandData.Clone() as StandData;
            stand.Results[index].Value = SelectedStandData.Results[index].WriteValue;
            mainModel.WriteStdSettings((ushort)StandSelNum, stand);
        }, canExecPar => true));
        #endregion
        #endregion

        #region Произвести стандартизацию
        RelayCommand _makeStandCommand;
        public RelayCommand MakeStandCommand => _makeStandCommand ?? (_makeStandCommand = new RelayCommand(execPar => MakeStand(), canExecPar => mainModel.Connecting.Value));
        void MakeStand()
        {
            if (!IsStandartisation)
            {
                ushort index = (ushort)StandSelNum;
                mainModel.MakeStand(index);
                standTimer = new System.Timers.Timer(mainModel.StandSettings[index].Duration.Value * 100 + 1000);
                standTimer.Start();
                IsStandartisation = true;
                standTimer.Elapsed += (s, e) =>
                {
                    mainModel.GetStdSelection(index);
                    IsStandartisation = false;
                    standTimer.Stop();
                    standTimer?.Dispose();
                };
            }
        }
        #endregion
        #endregion

        #region Команды настроек счетчиков

        #region Записать стартовую точку
        RelayCommand _changeCounterDiapStartCommand;
        public RelayCommand ChangeCounterDiapStartCommand
        {
            get
            {
                if (_changeCounterDiapStartCommand == null)
                {
                    _changeCounterDiapStartCommand = new RelayCommand(execPar =>
                    {
                        if (execPar == null) return;
                        ushort index = 0;
                        if (ushort.TryParse(execPar.ToString(), out index) && index < MainModel.CountCounters)
                        {
                            var diap = mainModel.CountDiapasones[index].Clone() as CountDiapasone;
                            diap.Start.Value = mainModel.CountDiapasones[index].Start.WriteValue;
                            mainModel.WriteCounterSettings(diap);
                        }
                    },
                    canExec => mainModel.Connecting.Value);
                }
                return _changeCounterDiapStartCommand;
            }
        }
        #endregion

        #region Записать конечную точку
        RelayCommand _changeCounterDiapFinishCommand;
        public RelayCommand ChangeCounterDiapFinishCommand
        {
            get
            {
                if (_changeCounterDiapFinishCommand == null)
                {
                    _changeCounterDiapFinishCommand = new RelayCommand(execPar =>
                    {
                        if (execPar == null) return;
                        ushort index = 0;
                        if (ushort.TryParse(execPar.ToString(), out index) && index < MainModel.CountCounters)
                        {
                            var diap = mainModel.CountDiapasones[index].Clone() as CountDiapasone;
                            diap.Finish.Value = mainModel.CountDiapasones[index].Finish.WriteValue;
                            mainModel.WriteCounterSettings(diap);
                        }
                    },
                    canExec => mainModel.Connecting.Value);
                }
                return _changeCounterDiapFinishCommand;
            }
        }
        #endregion
        #endregion 

        #region Команды измерения даты-времени
        #region установить RTC пользователя
        RelayCommand _setRtcCommand;
        public RelayCommand SetRtcCommand => _setRtcCommand ?? (_setRtcCommand = new RelayCommand(execPar =>
        {
            mainModel.SetRtc(mainModel.Rtc.WriteValue);
        }, canExecPar => true));
        #endregion
        #region Синхронизировать c временем ПК
        RelayCommand _syncRtcCommand;
        public RelayCommand SyncRtcCommand => _syncRtcCommand ?? (_syncRtcCommand = new RelayCommand(execPar =>
        {
            mainModel.SetRtc(DateTime.Now);
        }, canExecPar => true));
        #endregion
        #endregion

        #region Команды настроек измерительного порта
        #region Сменить режим порта 
        RelayCommand _changeSerialSelectCommand;
        public RelayCommand ChangeSerialSelectCommand => _changeSerialSelectCommand ?? (_changeSerialSelectCommand = new RelayCommand(o => mainModel.ChangeSerialSelect((int)o), o => o != null));
        #endregion

        #region Сменить баудрейт 
        RelayCommand _changeBaudrateCommand;
        public RelayCommand ChangeBaudrateCommand => _changeBaudrateCommand ?? (_changeBaudrateCommand = new RelayCommand(o => mainModel.ChangeBaudrate((uint)(o)), o => o != null));
        #endregion
        #endregion

        #region Команда изменения времени усреднения для пользователя
        RelayCommand _changeUserAvgTime;
        public RelayCommand ChangeUserAvgTime => _changeUserAvgTime ?? (_changeUserAvgTime = new RelayCommand(o =>
        {
            if (mainModel.CurMeasProcess.MeasDuration.Value > 0)
            {
                MeasProcess process = mainModel.CurMeasProcess.Clone() as MeasProcess;
                process.MeasDeep.Value = (ushort)(((uint)o) * 10 / mainModel.CurMeasProcess.MeasDuration.Value);
                mainModel.SetMeasProcessSettings(process, SelectedMeasProcessNum);
            }

        }, o => true));

        #endregion

        #region Команда "Установить напряжение HV"
        RelayCommand _setHvCommand;
        public RelayCommand SetHvCommand => _setHvCommand ?? (_setHvCommand = new RelayCommand(obj => mainModel.SetHv(mainModel.TelemetryHV.VoltageSV.WriteValue), obj => true));
        #endregion

        #region Команды вывода и фильтрации событий
        private RelayCommand _showEventsCommand;

        public RelayCommand ShowEventsCommand => _showEventsCommand ?? (_showEventsCommand = new RelayCommand(exec => AddHistoryItemsFromDb(), canExex => true));

        #endregion

        #region Команда "Установить настроку IP приемника UDP даных
        RelayCommand _setUpsAddrCommand;
        public RelayCommand SetUpsAddrCommand => _setUpsAddrCommand ?? (_setUpsAddrCommand = new RelayCommand(execPar =>
        {
            byte num = 0;
            var nums = (mainModel.UdpAddrString.Split(".", StringSplitOptions.RemoveEmptyEntries)).Where(s => byte.TryParse(s, out num)).Select(s => num).ToArray();
            if (nums.Length == 4) mainModel.SetUdpAddr(nums);
        },
            canExecPar => mainModel.Connecting.Value));

        #endregion        

        #region Запуск-останов платы АЦП
        RelayCommand _startAdcCommand;
        public RelayCommand StartAdcCommand => _startAdcCommand ?? (_startAdcCommand = new RelayCommand(execPar =>
        {
            ushort num = 0;
            if (execPar != null && ushort.TryParse(execPar.ToString(), out num))
            {
                mainModel.SwitchAdcBoard(num);
                if (num != 0 && Udp == null) {
                    Udp = new UDP();
                    Udp.UpdateOscillEvent += UpdateAdcTrend;
                    Udp.UpdateAmplitudesEvent += UpdateMaxAmpsData;
                }


            }
        }, canEcecPar => true));
        #endregion

        #region Запуск/останов выдачи данных АЦП 
        RelayCommand _startAdcDataCommand;
        public RelayCommand StartAdcDataCommand => _startAdcDataCommand ?? (_startAdcDataCommand = new RelayCommand(execPar =>
        {
            ushort num = 0;
            if (execPar != null && ushort.TryParse(execPar.ToString(), out num)) mainModel.StartStopAdcData(num);
        }, canEcecPar => true));
        #endregion

        #region Произвести единичное измерение"
        private RelayCommand _makeSingleMeasCommand;

        public RelayCommand MakeSingleMeasCommand => _makeSingleMeasCommand ?? (_makeSingleMeasCommand = new RelayCommand(execObj => {
            mainModel.MakeSingleMeasure(SingleMeasTime);
            SingleMeasCurTime = SingleMeasTime;
            SingelMeasFlag = true;
        }, canExecObj => true));

        #endregion

        #region Переключить реле
        RelayCommand _switchRelayCommand;
        public RelayCommand SwitchRelayCommand => _switchRelayCommand ?? (_switchRelayCommand = new RelayCommand(par => {
            ushort temp = 0;
            if (par != null && ushort.TryParse(par.ToString(), out temp))
            {
                mainModel.SwitchRelay(temp);
            }
        }, canExec => true));
        #endregion

        #region Команда расчета к-тов калибровки
        private RelayCommand _getCalibrCoeffsCommand;

        public RelayCommand GetCalibrCoeffsCommand => _getCalibrCoeffsCommand ?? (_getCalibrCoeffsCommand = new RelayCommand(execObj =>
        {
            CalibrationClass.GetCoeffs();
            App.Current.Dispatcher.Invoke(() =>
            {
                CalculatedCoeffs.Clear();
                int deg = 0;
                foreach (var num in CalibrationClass.CalcCoeefs)
                {
                    CalculatedCoeffs.Add(new CalcCalibrationResult(deg, num));
                    deg++;
                }
            });


        }, canExec => true));


        #endregion

        #region Команда "Загрузить коэффициенты калибровики"
        RelayCommand _setCalibrCoeffsCommand;
        public RelayCommand SetCalibrCoeffsCommand => _setCalibrCoeffsCommand ?? (_setCalibrCoeffsCommand = new RelayCommand(execPar =>
        {
            if (CalculatedCoeffs.Count >= 2)
            {
                var calibData = new CalibrData();
                calibData.Num.Value = mainModel.CurMeasProcess.Ranges[0].CalibCurveNum.Value;
                calibData.MeasUnitNum.Value = (ushort)CurMeasNum;
                for (int i = 0; i < CalculatedCoeffs.Count; i++)
                {
                    calibData.Coeffs[i].Value = (float)CalculatedCoeffs[i].Coeff;
                }
                mainModel.SetCalibrData(calibData);
            };
        }, canExecPar => true));
        #endregion

        #region Команда посчитать график для проверки полинома
        RelayCommand _showPolinomTrend;
        public RelayCommand ShowPolinomTrendCommand => _showPolinomTrend ?? (_showPolinomTrend = new RelayCommand(par =>
        {
            if (CalibrationClass.SingleMeasCells.Data.Count >= 2 && CalculatedCoeffs.Count != 0)
            {
                var measList = CalibrationClass.SingleMeasCells.Data.OrderBy(sm => sm.Weak).Select(sm => new Point(sm.Weak, sm.PhysVal)).ToList();
                var startWeak = measList[0].X;
                var finishWeak = measList[measList.Count - 1].X;
                if (startWeak != finishWeak)
                {
                    int cnt = 50;
                    double diff = (finishWeak - startWeak) / cnt;
                    var calcList = Enumerable.Range(0, cnt).
                    Select(i => new Point(startWeak + i * diff, GetPhysvalueByWeak(startWeak + i * diff))).ToList();
                    MeasuredPointsCollection = measList;
                    CalculatedMeasCollection = calcList;
                }

            }
        }, canExecPar => true));
        #endregion

        #region Команда пролонгировать источник
        RelayCommand _prolongCommand;
        public RelayCommand ProlongCommand => _prolongCommand ?? (_prolongCommand = new RelayCommand(par => SourceData.DateProlong = DateTime.Now, o => true));
        #endregion
        double GetPhysvalueByWeak(double weak)
        {
            double result = 0;
            for (int i = 0; i < CalculatedCoeffs.Count; i++)
            {
                result += (Math.Pow(weak, i) * CalculatedCoeffs[i].Coeff);
            }
            return result;
        }

        #endregion
        public MainModel mainModel { get; } = new MainModel();

        #region Конструктор
        public VM()
        {
            mainModel.ModelProcess();
            mainModel.UpdateDataEvent += AddDataToCollection;
            timer.Elapsed += (s, e) => CurPcDateTime.Value = DateTime.Now;
            timer.Elapsed += (s, e) => UpdateSingleMeasTime();
            timer.Start();
            Events = new Events(mainModel);
            GetEventHistory();
            Events.EventExecute += AddHistoryItem;
            _selectedEventItems.Filter += OnEventsFiltered;
            _selectedEventItems.SortDescriptions.Add(new SortDescription("EventTime", ListSortDirection.Descending));
            GetMeasDates();
            MeasUnitSDescribe();
            CurMeasNumDescribe();

        }


        #endregion

        UDP Udp { get; set; }

        #region Расчет коэффициентов калибровки
        #region Класс, включающий в себя данные ед. измерений, методы расчета к-тов
        public Calibration CalibrationClass { get; } = new Calibration();
        #endregion


        #endregion

        #region Единичное измерение
        #region Время еденичного измерения
        private ushort _singleMeasTime = 30;

        public ushort SingleMeasTime
        {
            get { return _singleMeasTime; }
            set { Set(ref _singleMeasTime, value); }
        }

        #endregion

        #region Флаг еденичного измерения
        private bool _singelMeasFlag;

        public bool SingelMeasFlag
        {
            get { return _singelMeasFlag; ; }
            set { Set(ref _singelMeasFlag, value); }
        }

        #endregion

        #region Физическая величина образца
        private double _singleMeasPhysValue;

        public double SingleMeasPhysValue
        {
            get { return _singleMeasPhysValue; }
            set { Set(ref _singleMeasPhysValue, value); }
        }

        #endregion

        #region Текущее время еденичного измерения
        private int _singleMeasCurTime;

        public int SingleMeasCurTime
        {
            get { return _singleMeasCurTime; }
            set { Set(ref _singleMeasCurTime, value); }
        }

        #endregion

        #region Результат еденичного измерения (счетчик)
        private double _singleMeasCounterResult;

        public double SingleMeasCounterResult
        {
            get { return _singleMeasCounterResult; }
            set { Set(ref _singleMeasCounterResult, value); }
        }

        #endregion

        #region Результат еденичного измерения (ослабление)
        private double _singleMeasWeakResult;

        public double SingleMeasWeakResult
        {
            get { return _singleMeasWeakResult; }
            set { Set(ref _singleMeasWeakResult, value); }
        }


        #endregion

        #region Таймер еденичного измерения
        void UpdateSingleMeasTime()
        {
            if (SingelMeasFlag)
            {
                // если прошло 2 секунды и не в режиме измерения, то снимаем флаг измерения
                if (SingleMeasTime - SingleMeasCurTime > 2 && !mainModel.CycleMeasStatus.Value)
                {
                    SingelMeasFlag = false;
                }
                SingleMeasCurTime--;
                if (SingleMeasCurTime < 0)
                {
                    SingelMeasFlag = false;
                    singleMeasTimer = new System.Timers.Timer(2000);
                    singleMeasTimer.Elapsed += (s, e) =>
                    {
                        singleMeasTimer?.Stop();
                        singleMeasTimer?.Dispose();
                        SingleMeasCounterResult = mainModel.CountersCur[0].Value;
                        int bcStanNum = mainModel.CurMeasProcess.BackStandNum.Value;
                        int physStandNum = mainModel.CurMeasProcess.Ranges[0].StandNum.Value;
                        int countNum = mainModel.CurMeasProcess.Ranges[0].CounterNum.Value;
                        bcStanNum = bcStanNum < 4 && bcStanNum >= 0 ? bcStanNum : 0;
                        physStandNum = physStandNum < 12 && physStandNum >= 4 ? physStandNum : 4;
                        countNum = countNum < 7 && countNum >= 0 ? countNum : 0;
                        double bcStandValue = mainModel.StandSettings[bcStanNum].Results[countNum].Value;
                        double physStandValue = mainModel.StandHalfPeriodValues[0].Value;
                        SingleMeasWeakResult = Math.Log(Math.Abs((physStandValue - bcStandValue) / (SingleMeasCounterResult - bcStandValue)));
                        var singleMeasCell = new SingleMeasCell() { Weak = SingleMeasWeakResult, PhysVal = SingleMeasPhysValue };
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            CalibrationClass.SingleMeasCells.Data.Add(singleMeasCell);
                        });
                        mainModel.SwitchMeas();

                    };
                    singleMeasTimer.Start();
                }
            }
        }
        #endregion

        #region Таймер запроса результата измерения
        System.Timers.Timer singleMeasTimer;
        #endregion

        #region Отображение графика из измеренных точек
        private IEnumerable<Point> _measuredPointsCollection;
        /// <summary>
        /// Данные для графика "Измеренное ослабление - плотность"
        /// </summary>
        public IEnumerable<Point> MeasuredPointsCollection
        {
            get { return _measuredPointsCollection; }
            set { Set(ref _measuredPointsCollection, value); }
        }
        #endregion

        #region Отображение графика из посчитанного полинома
        private IEnumerable<Point> _calculatedMeasCollection;

        public IEnumerable<Point> CalculatedMeasCollection
        {
            get { return _calculatedMeasCollection; }
            set { Set(ref _calculatedMeasCollection, value); }
        }

        #endregion

        #region Коллекция к-тов
        public ObservableCollection<CalcCalibrationResult> CalculatedCoeffs { get; } = new ObservableCollection<CalcCalibrationResult>();
        #endregion
        #endregion

        #region Текущая дата-время компьютера
        public Parameter<DateTime> CurPcDateTime { get; private set; } = new Parameter<DateTime>("CurPcDateTime", "Текущие время и дата компьютера", DateTime.MinValue, DateTime.MaxValue, 0, "");

        #endregion

        #region таймер
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        #endregion

        #region Cписок доступных Com портов 
        string[] _comPorts;
        public string[] ComPorts { get => _comPorts ?? (_comPorts = SerialPort.GetPortNames()); set => Set(ref _comPorts, value); }
        #endregion

        #region Аналоговые входы
        public List<AnalogInput> AnalogInputs => mainModel.AnalogGroups.Select(g => g.AI).ToList();
        #endregion

        #region Аналоговые выходы
        public List<AnalogOutput> AnalogOutputs => mainModel.AnalogGroups.Select(g => g.AO).ToList();
        #endregion

        #region Данные для текущего тренда
        #region Алгоритм интерполяции
        int _interpolIndex = 3;
        public int InterpolIndex { get => _interpolIndex; set { Set(ref _interpolIndex, value); } }
        #endregion

        #region Настройки тренда
        GraphSettings _trendSettings;
        public GraphSettings TrendSettings { get => _trendSettings ?? (_trendSettings = ClassInit<GraphSettings>()); }
        #endregion

        ObservableCollection<TimePoint> _plotCollection;
        public ObservableCollection<TimePoint> PlotCollection
        {
            get => _plotCollection ?? (_plotCollection = new ObservableCollection<TimePoint>());
        }
        #endregion

        #region Данные для архивного тренда
        #region Коллекция
        IEnumerable<TimePoint> _archivalDataPotnts;
        public IEnumerable<TimePoint> ArchivalDataPotnts { get => _archivalDataPotnts; private set { Set(ref _archivalDataPotnts, value); } }
        #endregion

        #region Настройки
        #region Стартовая точка отображаемого тренда
        DateTime _displayDateStart = DateTime.Today.AddDays(-1);
        public DateTime DisplayDateStart
        {
            get => _displayDateStart;
            set
            {
                Set(ref _displayDateStart, value);
                if (value >= DisplayDateEnd) DisplayDateEnd = value.AddMinutes(1);
                if (value.AddDays(2) < DisplayDateEnd) DisplayDateEnd = value.AddDays(2);
            }
        }
        #endregion
        #region Конечная точка отображаемого тренда
        DateTime _displayDateEnd = DateTime.Today;
        public DateTime DisplayDateEnd
        {
            get => _displayDateEnd;
            set
            {
                Set(ref _displayDateEnd, value);
                if (value <= DisplayDateStart) DisplayDateStart = value.AddMinutes(-1);
                if (value.AddDays(-2) > DisplayDateStart) DisplayDateStart = value.AddDays(-2);
            }
        }
        #endregion
        #region Путь к логируемому файлу
        string _logPath;
        public string LogPath { get => _logPath; set => Set(ref _logPath, value); }
        #endregion

        #region Список дат, когда происходили измерения
        List<BlackOutData> MeasDates { get; set; } = new List<BlackOutData>();
        void GetMeasDates()
        {
            MeasDates = SqlMethods.ReadFromSql<BlackOutData>($"SELECT DISTINCT date(time) FROM TimePoints;");
        }
        #endregion

        #endregion

        #region Состояние загрузки из ДБ
        bool _archivalTrendDownloading;
        public bool ArchivalTrendDownloading { get => _archivalTrendDownloading; private set { Set(ref _archivalTrendDownloading, value); } }
        #endregion

        #region Состояние загрузки в текстовый файл
        bool _archivalTrendUploading;
        public bool ArchivalTrendUploading { get => _archivalTrendUploading; private set { Set(ref _archivalTrendUploading, value); } }
        #endregion

        #endregion        

        #region Данные перечислений
        #region Названия измерительных процессов
        public DataBaseCollection<EnumCustom> MeasProcessNames { get; } = new DataBaseCollection<EnumCustom>("MeasProcessNames", new EnumCustom());
        #endregion;        

        #region Названия единиц измерения
        public DataBaseCollection<EnumCustom> UnitNames { get; } = new DataBaseCollection<EnumCustom>("UnitNames", new EnumCustom());
        string[] _measUnits;
        public string[] MeasUnits
        {
            get => _measUnits;            
            set => Set(ref _measUnits, value);
        }
        void MeasUnitSDescribe()
        {
            _measUnits = UnitNames.Data.Select(enumCustom => enumCustom.Name).ToArray();
            UnitNames.CollectionChangedEvent+=()=> MeasUnits = UnitNames.Data.Select(enumCustom => enumCustom.Name).ToArray();
        }

       
        #endregion

        #region Названия переменных для аналогового выхода
        public DataBaseCollection<EnumCustom> TypeAnalogOutVars { get; } = new DataBaseCollection<EnumCustom>("TypeAnalogOutVars", new EnumCustom());
        #endregion

        #region Названия стандартизаций
        public DataBaseCollection<EnumCustom> StandNames { get; } = new DataBaseCollection<EnumCustom>("StandNames", new EnumCustom());
        #endregion
        #endregion

        #region Стандартизация
        #region Выбранный диапазон
        private StandData _selectedStandData;

        public StandData SelectedStandData
        {
            get { return _selectedStandData ?? (_selectedStandData = mainModel.StandSettings[0]); }
            set { Set(ref _selectedStandData, value); }
        }
        #endregion

        #region Номер стандартизации
        private int _standSelNum;
        public int StandSelNum
        {
            get { return _standSelNum; }
            set 
            {
                if (Set(ref _standSelNum, value) && value < MainModel.CountStand)
                {
                    SelectedStandData = mainModel.StandSettings[value];
                }
            }
        }

        #endregion

        #region Флаг стандартизации
        private bool _isStandartisation;
        /// <summary>
        /// Производится стандартизация
        /// </summary>
        public bool IsStandartisation
        {
            get { return _isStandartisation; }
            set { Set(ref _isStandartisation, value); }
        }

        #endregion

        #region Таймер стандартизации
        /// <summary>
        /// Таймер стандартизации
        /// </summary>
        System.Timers.Timer standTimer;
        #endregion

        #endregion

        #region Выбранный номер измерительного процесса (не текущий!)
        int _selectedMeasProcessNum;
        public int SelectedMeasProcessNum
        {
            get => _selectedMeasProcessNum;
            set 
            {
                if (value < mainModel.MeasProcesses.Length)
                {
                    Set(ref _selectedMeasProcessNum, value);
                    SelectedMeasProcess = mainModel.MeasProcesses[value];
                    SelectedDiap = SelectedMeasProcess.Ranges[0];
                }
                    

            } 
        }
        #endregion

        #region Выбранный измерительный процесс
        MeasProcess _selectedMeasProcess;
        public MeasProcess SelectedMeasProcess
        {
            get => _selectedMeasProcess ?? (_selectedMeasProcess = mainModel.MeasProcesses[0]);
            set => Set(ref _selectedMeasProcess, value);            
        }
        #endregion

        #region Выбранный номер диапазона
        int _selectedDiapNum;
        public int SelectedDiapNum
        {
            get => _selectedDiapNum;
            set
            {
                if (value < SelectedMeasProcess.Ranges.Length)
                {
                    Set(ref _selectedDiapNum, value);
                    SelectedDiap = SelectedMeasProcess.Ranges[value];
                }
            }
        }
        #endregion

        #region Текущая еденица измерений
        int _curMeasNum;
        public int CurMeasNum
        {
            get => _curMeasNum;
            set => Set(ref _curMeasNum, value);
        }
        /// <summary>
        /// Отслеживание изменений номера ед. изм
        /// </summary>
        void CurMeasNumDescribe()
        {
            mainModel.CurMeasProcessNum.PropertyChanged += OnPropChanged;
            foreach (var process in mainModel.MeasProcesses)
            {
                process.Ranges[0].CalibCurveNum.PropertyChanged += OnPropChanged;
            }
            foreach (var calibrData in mainModel.CalibrDatas)
            {
                calibrData.MeasUnitNum.PropertyChanged+= OnPropChanged;
            }
            //локальная ф-я
            void OnPropChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "Value")
                {
                    CurMeasNum = mainModel.CalibrDatas[mainModel.CurMeasProcess.Ranges[0].CalibCurveNum.Value].MeasUnitNum.Value;
                }
            }
        }



        #endregion

        #region Выбранный диапазон
        public Diapasone _selectedDiap;
        public Diapasone SelectedDiap
        {
            get => _selectedDiap ?? (_selectedDiap = SelectedMeasProcess.Ranges[0]);
            set => Set(ref _selectedDiap, value);
        }
        #endregion

        #region Настройки источника
        SourceData _sourceData;
        public SourceData SourceData => _sourceData ?? (_sourceData = ClassInit<SourceData>());
        #endregion

        #region Настройки трубы
        private Pipe _pipe;
        public Pipe Pipe => _pipe ?? (_pipe = ClassInit<Pipe>());
        #endregion

        #region Вывести данные из БД
        async void ShowArchivalTrend()
        {
            try
            {
                ArchivalTrendDownloading = true;
                await Task.Run(() =>
                {
                    var list = SqlMethods.ReadFromSql<TimePoint>($"SELECT * FROM TimePoints WHERE time >= datetime('{DisplayDateStart.ToString("u")}') AND time <= datetime('{DisplayDateEnd.ToString("u")}');");
                    ArchivalDataPotnts = list;
                    ArchivalTrendDownloading = false;
                });
                
            }
            catch (Exception)
            {

            }
        }
        #endregion

        #region Добавление данных в график
        void AddDataToCollection()
        {
            App.Current?.Dispatcher?.Invoke(
                () =>
                {
                    var tp = new TimePoint { time = DateTime.Now, y1 = mainModel.PhysValueAvg.Value, y2 = mainModel.PhysValueCur.Value };
                    PlotCollection.Add(tp);
                    while (PlotCollection.Count>0 && PlotCollection[0].time < DateTime.Now.AddMinutes(TrendSettings.PlotTime * (-1)))
                    {
                        PlotCollection.RemoveAt(0);
                    }
                    SqlMethods.WritetoDb<TimePoint>(tp);
                }); ;

        }
        #endregion

        #region Инициализация унивесального параметра
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

        #region Время усреднения для пользователя(запись)
        uint _avgTimeWrite = 1;
        public uint AvgTimeWrite
        {
            get => _avgTimeWrite;
            set 
            {
                if (value > 0) Set(ref _avgTimeWrite, value);
            }
        }
        #endregion

        #region Данные UDP
        DateTime lastUpdateTime = DateTime.Now;
        #region Данные тренда
        IEnumerable<Point> _adcDataTrend;
        public IEnumerable<Point> AdcDataTrend
        {
            get => _adcDataTrend;
            set
            {
                Set(ref _adcDataTrend, value);

            }
        }
        void UpdateAdcTrend(List<Point> list)
        {
            if (lastUpdateTime.AddMilliseconds(500) < DateTime.Now)
            {
                AdcDataTrend = list;
                lastUpdateTime = DateTime.Now;
            }

        }
        #endregion

        #region Данные гистограммы в режиме максимальных амплитуд
        IEnumerable<Point> _maxAmplitudesData;
        /// <summary>
        /// Данные гистограммы в режиме максимальных амплитуд
        /// </summary>
        public IEnumerable<Point> MaxAmplitudesData
        {
            get => _maxAmplitudesData;
            set => Set(ref _maxAmplitudesData, value);
        }
        void UpdateMaxAmpsData(List<Point> list)
        {
            if (lastUpdateTime.AddMilliseconds(500) < DateTime.Now)
            {
                MaxAmplitudesData = list;
                lastUpdateTime = DateTime.Now;
            }
        }
        #endregion
        #endregion

        #region Запись в файл
        async void WriteArchivalTrendToText()
        {
            if (ArchivalTrendUploading) return;
            try
            {
                ArchivalTrendUploading = true;
                await Task.Run(() =>
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (var item in ArchivalDataPotnts)
                    {
                        builder.Append(item.time.ToString("dd/MM/yyyy HH:mm:ss:f") + "\t" + item.y1.ToString("0.000") + "\t" + item.y2.ToString("0.000" + "\n"));
                    }
                    using (StreamWriter sw = new StreamWriter(LogPath, false, System.Text.Encoding.Default))
                    {
                        sw.WriteLine(builder.ToString());
                    }
                });
                ArchivalTrendUploading = false;
            }
            catch (Exception ex)
            {
                
            }
        }
        #endregion

        #region События
        #region Фильтрация событий
        #region Начальная точка
        private DateTime _startPointHistoryEvent = DateTime.Today;

        public DateTime StartPointHistoryEvent
        {
            get { return _startPointHistoryEvent; }
            set { Set(ref _startPointHistoryEvent, value); }
        }
        #endregion

        #region Конечная точка
        private DateTime _endPointHistoryEvent = DateTime.Now;

        public DateTime EndPointHistoryEvent
        {
            get { return _endPointHistoryEvent; }
            set { Set(ref _endPointHistoryEvent, value); }
        }
        #endregion

        private readonly CollectionViewSource _selectedEventItems = new CollectionViewSource();
        public ICollectionView SelectedEventItems => _selectedEventItems.View;

        private string _eventFilterText;
        /// <summary>
        /// Текст фильтра событий
        /// </summary>
        public string EventFilterText
        {
            get { return _eventFilterText; }
            set 
            {
                if (!Set(ref _eventFilterText, value)) return;
                _selectedEventItems.View.Refresh();
            }
        }

        private void OnEventsFiltered(object sender, FilterEventArgs e)
        {
            if (!(e.Item is HistoryEventItem eventItem))
            {
                e.Accepted = false;
                return;
            }
            var text = EventFilterText;
            if (string.IsNullOrWhiteSpace(text)) return;
            if (eventItem.Event.Id.Contains(text)) return;
            if (eventItem.Event.Num.ToString().Contains(text)) return;
            if (eventItem.Event.Description.Contains(text)) return;
            if (eventItem.Event.Title.Contains(text)) return;
            if (eventItem.UserName.Contains(text)) return;
            if (eventItem.EventTime.ToString().Contains(text)) return;
            e.Accepted = false;
        }
        #endregion

        #region Класс событий
        public Events Events { get; }
        #endregion

        #region История событий
        /// <summary>
        /// Коллекция истории событий
        /// </summary>
        private ObservableCollection<HistoryEventItem> _historyEventItems;
        public ObservableCollection<HistoryEventItem> HistoryEventItems
        {
            get => _historyEventItems;
            private set
            {
                if (!Set(ref _historyEventItems, value)) return;
                _selectedEventItems.Source = value; 
                OnPropertyChanged(nameof(SelectedEventItems));
            }
        } 
        #endregion
        #region Флаг загрузки данных событий из БД
        private bool _historyItemDownloading;
        /// <summary>
        /// Флаг загрузки данных событий из БД
        /// </summary>
        public bool HistoryItemDownloading
        {
            get { return _historyItemDownloading; }
            set { Set(ref _historyItemDownloading, value); }
        }

        #endregion
        #region Метод загрузки событий из базы данных
        async void AddHistoryItemsFromDb()
        {
            HistoryItemDownloading = true;
            await Task.Run(() => GetEventHistory());
            HistoryItemDownloading = false;
        }
        void GetEventHistory()
        {
            try
            {
                var list = SqlMethods.ReadFromSql<HistoryEventItemDb>($"SELECT * FROM HistoryEventItemDbs WHERE EventTime >= datetime('{StartPointHistoryEvent.ToString("u")}') AND EventTime <= datetime('{EndPointHistoryEvent.ToString("u")}');");
                App.Current.Dispatcher.Invoke(() =>
                {
                    HistoryEventItems = new ObservableCollection<HistoryEventItem>();
                    for (int i = 0; i < list.Count; i++)
                        HistoryEventItems.Add(new HistoryEventItem()
                        {
                            UserName = list[i].UserName,
                            EventTime = list[i].EventTime,
                            Activity = list[i].Activity,
                            Event = Events.EventDevices.Where(dev => dev.Id == list[i].Id).FirstOrDefault()
                        });
                });                        
            }
            catch (Exception)
            {

            }
        }
        #endregion

        

        #region Метод загрузки события в базу данных и коллекцию
        void AddHistoryItem(EventDevice device)
        {            
            App.Current.Dispatcher.Invoke(()=> 
            {
                var time = DateTime.Now;
                var name = CurUser == null ? "Пользователь не авторизован" : $"{CurUser.Somename} {CurUser.Name}";
                HistoryEventItems.Add(new HistoryEventItem()
                {
                    Event = device,
                    EventTime = time,
                    UserName = name,
                    Activity = device.IsActive
                }); ;
                try
                {
                    SqlMethods.WritetoDb<HistoryEventItemDb>(new HistoryEventItemDb
                    {
                        EventTime = time,
                        Id = device.Id,
                        Activity = device.IsActive,
                        UserName = name
                    });
                }
                catch (Exception)
                {
                    
                }
            });
        }
        #endregion


        void UpadateDates(object obj)
        {
            if (!(obj is Calendar calendar)) return;
            DateTime startDate = new DateTime(calendar.DisplayDate.Year,calendar.DisplayDate.Month,1);
            var enabledDates = MeasDates.Where(ms => ms.Time >= startDate && ms.Time <= startDate.AddMonths(1)).Select(ms=>ms.Time);            
            calendar.DisplayDateEnd = startDate.AddMonths(1);            
            var tempDate = startDate;
            calendar.BlackoutDates.Clear();
            while (tempDate<= calendar.DisplayDateEnd)
            {
                if (tempDate != calendar.SelectedDate && !enabledDates.Any(dt=>dt==tempDate))
                    calendar.BlackoutDates.Add(new CalendarDateRange(tempDate));
                tempDate = tempDate.AddDays(1);
            }  
        }
        private RelayCommand _selectDatesCommand;

        public RelayCommand SelectDatesCommand
        {
            get { return _selectDatesCommand ?? (_selectDatesCommand = new RelayCommand(obj => UpadateDates(obj), obj => true)); }
            
        }



        #endregion

    }
}
