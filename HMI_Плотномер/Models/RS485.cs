using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using EasyModbus;
using IDensity.AddClasses;
using IDensity.AddClasses.AdcBoardSettings;
using IDensity.AddClasses.AnalogInOut;
using IDensity.AddClasses.Standartisation;

namespace IDensity.Models
{
    /// <summary>
    /// Представляет набор свойств и методов, необходимых для связи с платой по RS485 (Modbus RTU)
    /// </summary>
    class RS485: PropertyChangedBase
    {


        #region Перечисления номеров holding регистров, представляющих команды
        enum Holds
        { 
            SwitchMeas = 1,// включение измерения
            SingleMeasDuration = 2,// длительность ед. измерения
            SingleMeasStart = 3, // старт ед. измерения
            SwitchHv = 4,   // включение hv
            SwitchPwrAm  = 7,    // стартовый номер регистра управления питанием аналоговых модулей
            TestValueDac = 12,  // тестовая величина для отправки на ЦАП
            SendTestValue = 13, // отправка тестового значения на ЦАП
            AdcSett = 45, // начальный номер регистров, отвечающих за настройки АЦП
            DacSett=51, // начальный номер регистров, отвечающих за настройки ЦАП
            UdpAddr = 109, // начальный номер регистров, отвечающих за IP адрес UDP приемника
            StartAdc = 118,// старт-стоп платы АЦП
            StartAdcData = 119// старт-стоп выдачи данных АЦП
        }
        #endregion        

        #region Свойства
        #region Адрес в сети Modbus
        byte _mbAddr = 1;
        public byte MbAddr
        {
            get => _mbAddr;
            set => Set(ref _mbAddr, value);
        }
        #endregion

        #region Название COM порта
        string _comName="COM1";
        public string ComName { get => _comName; set => Set(ref _comName, value); }
        #endregion        

        #region Баудрейт
        int _baudrate = 115200;
        public int BaudRate { get => _baudrate; set => Set(ref _baudrate, value); }
        #endregion        

        #region Проверка четности
        Parity _parity = Parity.None;
        public string Par { get => (_parity).ToString(); set => Set(ref _parity, GetParity(value)); }
        Parity GetParity(string name)
        {
            switch (name.ToLower())
            {
                case "even": return Parity.Even;
                case "odd": return Parity.Odd;
                default: return Parity.None;                    
            }
        }
        #endregion

        #endregion

        #region Поля
        #region Ссылка на модель
        MainModel model;
        #endregion
        ModbusClient client;
        #region Массив Holding регистров
        /// <summary>
        /// Массив Holding регистров
        /// </summary>
        ushort[] holdRegs = new ushort[200];
        #endregion
        #region Массив Reading регистров
        /// <summary>
        /// Массив Reading регистров
        /// </summary>
        ushort[] readRegs = new ushort[100];
        #endregion
        #endregion        

        #region Номер процесса для выбора
        public Parameter<int> SelectMeasNum = new Parameter<int>("SelectMeasNum", "Номер процесса для выбора", 0, MainModel.measProcessNum - 1, 26, "hold");
        #endregion        

        #region Очередь для хранения команд, которые надо выполнить
        Queue<Command> commands = new Queue<Command>();

        class Command
        {
            public Command(Action<int,int> act, int p1,int p2)
            {
                f = act;
                par1 = p1;
                par2 = p2;
            }
            public Action<int, int> f;
            public int par1;
            public int par2;
        }
        #endregion

        #region Метод GetData  -  основной метод
        public void GetData(MainModel model)
        {
            this.model = model;
            try
            {
                if ((client == null) || (!client.Connected)) PortInit();
                else
                {
                    // Прочитать данные Reading регистров
                    for (int i = 0; i < 5; i++)ReadInputRegs( i * 15, 15);
                    // Прочитать данные Holding регистров
                    for (int i = 0; i < 2; i++)ReadHoldRegs(i * 15, 15);
                    RecognizePacket();
                    ReadSettings();// Чтение настроек, если надо

                    while (commands.Count > 0)
                    {
                        var command = commands.Dequeue();
                        command.f.Invoke(command.par1, command.par2);
                                              
                    }
                    model.Connecting.Value = true;
                }
            }
            catch (Exception ex)
            {
                commands.Clear();
                client.Disconnect();
                model.Connecting.Value = false;
            }
        }
        #endregion

        #region Инициализаця порта
        void PortInit()
        {           
            client = new ModbusClient(ComName);
            client.ConnectionTimeout = 500;
            client.Baudrate = BaudRate;
            client.UnitIdentifier = MbAddr;
            client.Parity = System.IO.Ports.Parity.None;
            client.Connect();            
        }
        #endregion

        #region Чтение input регистров
        void ReadInputRegs(int offset, int size)
        {
            var arr = client.ReadInputRegisters(offset, size);
            Array.Copy(arr.Select(num => (ushort)num).ToArray(), 0, readRegs, offset, size);
            //Thread.Sleep(20);
        }
        #endregion

        #region Чтение холдинг регистров
        void ReadHoldRegs(int offset, int size)
        {
            var arr = client?.ReadHoldingRegisters(offset, size);
            Array.Copy(arr.Select(num => (ushort)num).ToArray(), 0, holdRegs, offset, size);
            //Thread.Sleep(20);
        }
        #endregion

        #region Записть в регистры
        void WriteRegs(int offset, int size)
        {
            client.WriteMultipleRegisters(offset, holdRegs.Skip(offset).Take(size).Select(u16 => (int)u16).ToArray());
            Thread.Sleep(20);
        } 
        #endregion

        #region Функция распознавания данных из  регистров
        void RecognizePacket()
        {
            GetRtc();
            GetHalfPeriodStandartisation();
            GetCurMeas();
            GetAmTelemetry();
            GetHVTelemetry();
            GetTempTelemetry();
            GetDeviceStatus();
        }
        #endregion

        #region Статусы устройств
        void GetDeviceStatus()
        {
            model.CommStates.Value = SelectRegs(model.CommStates.RegType)[model.CommStates.RegNum];
            foreach (var group in model.AnalogStateGroups) group.Value = SelectRegs(group.RegType)[group.RegNum];
            foreach (var group in model.AnalogStateGroups)group.Value = SelectRegs(group.RegType)[group.RegNum];           
            model.GetDeviceData();
        }
        #endregion

        #region Телемерия аналоговых модулей
        void GetAmTelemetry()
        {
            var startNumReg = 11;
            // Аналоговые выходы
            for (int i = 0; i < 2; i++)
            {
                model.AnalogGroups[i].AO.VoltageTest.Value = readRegs[startNumReg + i * 2];
                model.AnalogGroups[i].AO.AdcValue.Value = readRegs[startNumReg + 4 + i * 2];
                model.AnalogGroups[i].AO.VoltageDac.Value = readRegs[startNumReg + 8 + i * 2];
                model.AnalogGroups[i].AI.AdcValue.Value = readRegs[startNumReg + 5 + i * 2];                
            }
            model.ConsInputSettings.CurConsumtion.Value = GetFloatFromUshorts(readRegs, model.ConsInputSettings.CurConsumtion.RegNum);
        }
        #endregion

        #region Функция чтения времени-даты
        void GetRtc()
        {
            model.Rtc.Value = new DateTime(
                holdRegs[model.Rtc.RegNum]+2000,
                holdRegs[model.Rtc.RegNum+1],
                holdRegs[model.Rtc.RegNum+2],
                holdRegs[model.Rtc.RegNum+3],
                holdRegs[model.Rtc.RegNum+4],
                holdRegs[model.Rtc.RegNum+5]
                );
        }
        #endregion

        #region Получить данные текущих измерений
        void GetCurMeas()
        {
            model.CycleMeasStatus.Value = SelectRegs(model.CycleMeasStatus.RegType)[model.CycleMeasStatus.RegNum] == 0 ? false : true;
            if (model.CycleMeasStatus.Value)
            {                
                model.PhysValueCur.Value = GetFloatFromUshorts(SelectRegs(model.PhysValueCur.RegType), model.PhysValueCur.RegNum);
                model.PhysValueAvg.Value = GetFloatFromUshorts(SelectRegs(model.PhysValueAvg.RegType), model.PhysValueAvg.RegNum);
                model.ContetrationValueCur.Value = GetFloatFromUshorts(SelectRegs(model.ContetrationValueCur.RegType), model.ContetrationValueCur.RegNum);
                model.ContetrationValueAvg.Value = GetFloatFromUshorts(SelectRegs(model.ContetrationValueAvg.RegType), model.ContetrationValueAvg.RegNum);
                model.ConsMassCommon.Value = GetFloatFromUshorts(SelectRegs(model.ConsMassCommon.RegType), model.ConsMassCommon.RegNum);
                model.ConsMassSolid.Value = GetFloatFromUshorts(SelectRegs(model.ConsMassSolid.RegType), model.ConsMassSolid.RegNum);
            }
            for (int i = 0; i < 3; i++)
            {
                model.CountersCur[i].Value = GetFloatFromUshorts(SelectRegs(model.CountersCur[i].RegType), model.CountersCur[i].RegNum);

            }
        }
        
        ushort[] SelectRegs(string reg)
        {
            if (reg == "hold") return holdRegs;
            return readRegs;
        }
        #endregion

        #region Получить float из 2х регистров
        float GetFloatFromUshorts(ushort[] regs, int regNum)
        {
            ushort[] arr = new ushort[] { regs[regNum], regs[regNum + 1] };
            return BitConverter.ToSingle(arr.SelectMany(num => BitConverter.GetBytes(num)).ToArray(), 0);
        }
        #endregion

        #region Получить int32 из 2х регистров
        uint GetUInt32FromUshorts(ushort[] regs, int regNum)
        {
            try
            {
                ushort[] arr = new ushort[] { regs[regNum], regs[regNum + 1] };
                return BitConverter.ToUInt32(arr.SelectMany(num => BitConverter.GetBytes(num)).ToArray(), 0);
            }
            catch (Exception)
            {
                return 0;
            }
           
           
        }
        #endregion

        #region Получить 2 регистра из float
        void GetUshortsFromFloat(ushort[] destination, int destinationIndex, float source)
        {
            var bytes = BitConverter.GetBytes(source);
            for (int i = 0; i < 2; i++)
            {
                destination[i + destinationIndex] = BitConverter.ToUInt16(bytes, i * 2);
            }  
        }
        #endregion

        #region Получить 2 регистра из int32
        void GetUshortsFromUInt32(ushort[] destination, int destinationIndex, uint source)
        {
            var bytes = BitConverter.GetBytes(source);
            for (int i = 0; i < 2; i++)
            {
                destination[i + destinationIndex] = BitConverter.ToUInt16(bytes, i * 2);
            }
        }
        #endregion

        #region Получить данные телеметрии HV
        void GetHVTelemetry()
        {            
            model.TelemetryHV.VoltageCurIn.Value = ((float)SelectRegs(model.TelemetryHV.VoltageCurIn.RegType)[model.TelemetryHV.VoltageCurIn.RegNum]) / 1000;
            model.TelemetryHV.VoltageCurOut.Value = (ushort)(SelectRegs(model.TelemetryHV.VoltageCurOut.RegType)[model.TelemetryHV.VoltageCurOut.RegNum] / 20);
            model.TelemetryHV.HvOn.Value = model.TelemetryHV.VoltageCurOut.Value > 100;
        }
        #endregion

        #region Запрос стандартизаций, скорректированных по времени
        void GetHalfPeriodStandartisation()
        {
            for (int i = 0; i < 3; i++)
            {
                model.StandHalfPeriodValues[i].Value = GetFloatFromUshorts(readRegs, model.StandHalfPeriodValues[i].RegNum);
                model.StandHalfPeriodAges[i].Value = GetFloatFromUshorts(readRegs, model.StandHalfPeriodAges[i].RegNum);
            }
        }
        #endregion

        #region Запрос телеметрии от платы температуры
        void GetTempTelemetry()
        {
            model.TempTelemetry.TempExternal.Value = ((float)(SelectRegs(model.TempTelemetry.TempExternal.RegType)[model.TempTelemetry.TempExternal.RegNum] - 2730)) / 10;
            model.TempTelemetry.TempInternal.Value = ((float)(SelectRegs(model.TempTelemetry.TempInternal.RegType)[model.TempTelemetry.TempInternal.RegNum] - 2730)) / 10;
        }
        #endregion

        #region Чтение настроек
        void ReadSettings()
        {
            if (!model.SettingsReaded)
            {
                // Читаем данные Holding регистров
                for (int i = 0; i < 3; i++) ReadHoldRegs(30 + i * 15, 15);
                GetMeasProcessData();
                GetSerialSettings();
                GetHvTarget();
                GetAnalogOutSettings();
                GetAllStandSettings();
                GetAnalogInSettings();
                GetCounterSettingsAll();
                GetCalibrDataAll();
                GetUdpAddr();
                GetAdcBoardSettings();
                GetMeasUnitSettingsAll();
                GetConsInputSettings();
            }
        }
        #region Данные измерительных настроек
        void GetMeasProcessData()
        {
            model.SettingsReaded = false;
            // Читаем данные процессов
            for (int i = 0; i < MainModel.measProcessNum; i++)
            {
                client.WriteSingleRegister(SelectMeasNum.RegNum, i);
                for (int j = 0; j < 2; j++)
                {
                    ReadHoldRegs(j * 10 + MeasProcess.ModbRegNum, 10);
                }
                for (int j = 0; j < 3; j++)
                {
                    model.MeasProcesses[i].Ranges[j].CalibCurveNum.Value = holdRegs[MeasProcess.ModbRegNum + j * 3];
                    model.MeasProcesses[i].Ranges[j].StandNum.Value = holdRegs[MeasProcess.ModbRegNum + j * 3 + 1];
                    model.MeasProcesses[i].Ranges[j].CounterNum.Value = holdRegs[MeasProcess.ModbRegNum + j * 3 + 2];
                }
                model.MeasProcesses[i].BackStandNum.Value = holdRegs[MeasProcess.ModbRegNum + 9];
                model.MeasProcesses[i].MeasDuration.Value = holdRegs[MeasProcess.ModbRegNum + 10];
                model.MeasProcesses[i].MeasDeep.Value = holdRegs[MeasProcess.ModbRegNum + 11];
                model.MeasProcesses[i].HalfLife.Value = GetFloatFromUshorts(holdRegs, MeasProcess.ModbRegNum + 12);
                model.MeasProcesses[i].DensityLiquid.Value = GetFloatFromUshorts(holdRegs, MeasProcess.ModbRegNum + 14);
                model.MeasProcesses[i].DensitySolid.Value = GetFloatFromUshorts(holdRegs, MeasProcess.ModbRegNum + 16);
            }
            //Текущий номер измерений
            model.CurMeasProcessNum.Value = SelectRegs(model.CurMeasProcessNum.RegType)[model.CurMeasProcessNum.RegNum];
            if (model.CurMeasProcessNum.Value < MainModel.measProcessNum) model.CurMeasProcess = model.MeasProcesses[model.CurMeasProcessNum.Value];
            model.SettingsReaded = true;
        }
        #endregion

        #region Настройки порта
        void GetSerialSettings()
        {
            model.SettingsReaded = false;
            // Чтение баудрейта            
            model.PortBaudrate.Value = GetUInt32FromUshorts(holdRegs, model.PortBaudrate.RegNum);
            // Чтение режима работы           
            
            model.SettingsReaded = true;
        }
        #endregion        

        #region Уставка HV
        void GetHvTarget()
        {
            model.SettingsReaded = false;
            model.TelemetryHV.VoltageSV.Value = (ushort)(holdRegs[model.TelemetryHV.VoltageSV.RegNum] / 20);
            model.SettingsReaded = true;
        }
        #endregion

        #region Настройки аналоговых выходов
        void GetAnalogOutSettings()
        {
            model.SettingsReaded = false;
            for (int i = 0; i < 2; i++)
            {
                model.AnalogGroups[i].AO.Activity.Value = holdRegs[(int)Holds.DacSett + i];
                model.AnalogGroups[i].AO.DacType.Value = holdRegs[(int)Holds.DacSett + i + 2];
                model.AnalogGroups[i].AO.DacEiNdx.Value = holdRegs[(int)Holds.DacSett + i + 4];
                model.AnalogGroups[i].AO.DacVarNdx.Value = holdRegs[(int)Holds.DacSett + i + 6];
                model.AnalogGroups[i].AO.DacLowLimit.Value = GetFloatFromUshorts(holdRegs, (int)Holds.DacSett + i * 2 + 8);
                model.AnalogGroups[i].AO.DacHighLimit.Value = GetFloatFromUshorts(holdRegs, (int)Holds.DacSett + i * 2 + 12);
            }
            model.SettingsReaded = true;
        }
        #endregion

        #region Настройки аналоговых выходов
        void GetAnalogInSettings()
        {
            model.SettingsReaded = false;
            for (int i = 0; i < 2; i++)
            {
                model.AnalogGroups[i].AI.Activity.Value = holdRegs[(int)Holds.AdcSett + i];                
            }
            model.SettingsReaded = true;
        }
        #endregion

        #region Настройки стандартизации
        /// <summary>
        /// Получить данные всех наборов стандартизаций
        /// </summary>
        void GetAllStandSettings()
        {
            for (ushort i = 0; i < MainModel.CountStand; i++)
            {
                GetStandSetiings(i);
            }
        }
        void GetStandSetiings(ushort num)
        {
            model.SettingsReaded = false;
            client.WriteSingleRegister(StandData.NumSelection.RegNum, num);
            for (int i = 0; i < 2; i++)
            {
                ReadHoldRegs(model.StandSettings[0].Duration.RegNum + i * 12, 12);
            }
            model.StandSettings[num].Duration.Value = holdRegs[model.StandSettings[0].Duration.RegNum];
            model.StandSettings[num].Type.Value = holdRegs[model.StandSettings[0].Type.RegNum];
            model.StandSettings[num].Value.Value = GetFloatFromUshorts(holdRegs, model.StandSettings[0].Value.RegNum);
            for (int j = 0; j < 8; j++)
            {
                model.StandSettings[num].Results[j].Value = GetFloatFromUshorts(holdRegs, model.StandSettings[num].Results[j].RegNum + j * 2);
            }
            var year = holdRegs[model.StandSettings[0].Date.RegNum+2] + 2000;
            var month = (int)holdRegs[model.StandSettings[0].Date.RegNum + 1];
            month = month > 0 && month <= 12 ? month : 1;
            var day = (int)holdRegs[model.StandSettings[0].Date.RegNum];
            day = day > 0 && day <= 31 ? day : 1;
            model.StandSettings[num].Date.Value = new DateTime(year, month, day);
            model.SettingsReaded = true;
        }
        #endregion

        #region Настройки счетчиков
        void GetCounterSettingsAll()
        {
            for (ushort i = 0; i < MainModel.CountCounters; i++)
            {
                GetCounterSettings(i);
            }
        }
        void GetCounterSettings(ushort num)
        {
            model.SettingsReaded = false;
            client.WriteSingleRegister(model.CountDiapasones[0].Num.RegNum, num);
            ReadHoldRegs(model.CountDiapasones[0].Start.RegNum, 2);
            if (num < MainModel.CountCounters)
            {
                model.CountDiapasones[num].Num.Value = num;
                model.CountDiapasones[num].Start.Value = holdRegs[model.CountDiapasones[0].Start.RegNum];
                model.CountDiapasones[num].Finish.Value = holdRegs[model.CountDiapasones[0].Finish.RegNum];
            }
            model.SettingsReaded = true;
        }


        #endregion

        #region UDP адрес приемника
        void GetUdpAddr()
        {
            model.SettingsReaded = false;
            ReadHoldRegs((int)Holds.UdpAddr, 2);
            var ip = GetUInt32FromUshorts(holdRegs, (int)Holds.UdpAddr);
            string ipString = "";
            for (int i = 0; i < 4; i++)
            {
                ipString += ((byte)(ip >> (i * 8))).ToString()+".";
            }
            model.UdpAddrString = ipString.Remove(ipString.Length - 1, 1);
            model.SettingsReaded = true;
        }
        #endregion

        #region Настройки платы АЦП
        void GetAdcBoardSettings()
        {
            model.SettingsReaded = false;
            ReadHoldRegs(model.AdcBoardSettings.AdcMode.RegNum, 6);
            model.AdcBoardSettings.AdcMode.Value = holdRegs[model.AdcBoardSettings.AdcMode.RegNum];
            model.AdcBoardSettings.AdcProcMode.Value = holdRegs[model.AdcBoardSettings.AdcProcMode.RegNum];
            model.AdcBoardSettings.AdcSyncLevel.Value = holdRegs[model.AdcBoardSettings.AdcSyncLevel.RegNum];
            model.AdcBoardSettings.AdcSyncMode.Value = holdRegs[model.AdcBoardSettings.AdcSyncMode.RegNum];
            model.AdcBoardSettings.TimerMax.Value = holdRegs[model.AdcBoardSettings.TimerMax.RegNum];
            model.AdcBoardSettings.PreampGain.Value = holdRegs[model.AdcBoardSettings.PreampGain.RegNum];
            model.SettingsReaded = true;
        }
        #endregion

        #region Настройки калибровочных кривых
        /// <summary>
        /// Получить данные всех калибровочных кривых
        /// </summary>
        void GetCalibrDataAll()
        {
            for (ushort i = 0; i < MainModel.CalibCurveNum; i++)
            {
                GetCalibrData(i);
            }
        }
        /// <summary>
        /// Получить данные калибровочной кривой по индексу
        /// </summary>
        /// <param name="index"></param>
        void GetCalibrData(ushort index)
        {
            model.SettingsReaded = false;
            client.WriteSingleRegister(model.CalibrDatas[index].Num.RegNum, index);
            ReadHoldRegs(model.CalibrDatas[index].MeasUnitNum.RegNum, 14);
            model.CalibrDatas[index].Num.Value = index;
            model.CalibrDatas[index].MeasUnitNum.Value = holdRegs[model.CalibrDatas[index].MeasUnitNum.RegNum];
            for (int i = 0; i < 6; i++)
            {
                model.CalibrDatas[index].Coeffs[i].Value = GetFloatFromUshorts(holdRegs, model.CalibrDatas[index].Coeffs[i].RegNum);                    
            }
            model.SettingsReaded = true;
        }
        #endregion

        #region Настройки единиц измерения
        void GetMeasUnitSettingsAll()
        {
            for (ushort i = 0; i < 5; i++)
            {
                GetMeasUnitSettings(i);
            }
        }
        void GetMeasUnitSettings(ushort index)
        {
            model.SettingsReaded = false;
            client.WriteSingleRegister(model.MeasUnitSettings[0].Id.RegNum, index);
            ReadHoldRegs(model.MeasUnitSettings[0].A.RegNum, 4);
            model.MeasUnitSettings[index].Id.Value = index;
            model.MeasUnitSettings[index].A.Value = GetFloatFromUshorts(holdRegs, model.MeasUnitSettings[0].A.RegNum);
            model.MeasUnitSettings[index].B.Value = GetFloatFromUshorts(holdRegs, model.MeasUnitSettings[0].B.RegNum);
            model.SettingsReaded = true;
        }
        #endregion

        #region Данные настроек входа расходомера
        void GetConsInputSettings()
        {
            model.SettingsReaded = false;
            ReadHoldRegs(model.ConsInputSettings.InputNum.RegNum, 8);
            model.ConsInputSettings.InputNum.Value = holdRegs[model.ConsInputSettings.InputNum.RegNum];
            model.ConsInputSettings.Activity.Value = holdRegs[model.ConsInputSettings.Activity.RegNum]>0? true:false;
            model.ConsInputSettings.A.Value = GetFloatFromUshorts(holdRegs, model.ConsInputSettings.A.RegNum);
            model.ConsInputSettings.B.Value = GetFloatFromUshorts(holdRegs, model.ConsInputSettings.B.RegNum);
            model.SettingsReaded = true;
        }
        #endregion


        #endregion

        #region Команды
        #region Включить-выключить HV
        public void SwitchHv(int value)
        {
            commands.Enqueue(new Command((offset, value) => client.WriteSingleRegister(offset, value), (int)Holds.SwitchHv, value));
        }
        #endregion

        #region Включить-выключить измерения
        public void SwitchMeas(int value)
        {
            commands.Enqueue(new Command((offset, value) => client.WriteSingleRegister(offset, value), (int)Holds.SwitchMeas, value));
        }
        #endregion

        #region Записать данные измерительного процеса
        public void SetMeasProcessSettings(MeasProcess process, int index)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                model.SettingsReaded = false;
                //Записываем регистр для записи
                client.WriteSingleRegister(SelectMeasNum.RegNum, index);
                //Записываем данные процесса измерений
                for (int i = 0; i < 3; i++)// Данные диапазонов
                {
                    holdRegs[MeasProcess.ModbRegNum + i * 3] = process.Ranges[i].CalibCurveNum.Value;
                    holdRegs[MeasProcess.ModbRegNum + i * 3 + 1] = process.Ranges[i].StandNum.Value;
                    holdRegs[MeasProcess.ModbRegNum + i * 3 + 2] = process.Ranges[i].CounterNum.Value;
                }
                holdRegs[MeasProcess.ModbRegNum + 9] = process.BackStandNum.Value;
                holdRegs[MeasProcess.ModbRegNum + 10] = process.MeasDuration.Value;
                holdRegs[MeasProcess.ModbRegNum + 11] = process.MeasDeep.Value;
                GetUshortsFromFloat(holdRegs, MeasProcess.ModbRegNum + 12, process.HalfLife.Value);
                GetUshortsFromFloat(holdRegs, MeasProcess.ModbRegNum + 14, process.DensityLiquid.Value);
                GetUshortsFromFloat(holdRegs, MeasProcess.ModbRegNum + 16, process.DensitySolid.Value);
                for (int i = 0; i < 2; i++)
                {
                    WriteRegs(MeasProcess.ModbRegNum + i * 9, 9);
                }
            }, 0, 0));
            
        }
        #endregion

        #region Смена номера измерительного процесса
        public void ChangeMeasProcess(int index)
        {
            commands.Enqueue(new Command((p1, p2) => 
            {
                model.SettingsReaded = false;
                client?.WriteSingleRegister(model.CurMeasProcessNum.RegNum, index);
            }, 0, 0));            
        }
        #endregion

        #region Команды настроек последовательного порта
        #region Записать бадрейт
        public void ChangeBaudrate(uint value)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
              model.SettingsReaded = false;
                GetUshortsFromUInt32(holdRegs, model.PortBaudrate.RegNum, value);
                WriteRegs(model.PortBaudrate.RegNum, 2);
            }, 0, 0));
        }
        #endregion

        #region Изменить режим работы последовательного порта
        public void ChangeSerialSelect(ushort value)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                model.SettingsReaded = false;
                client?.WriteSingleRegister(model.PortSelectMode.RegNum, value);
            }, 0, 0));
        }
        #endregion
        #endregion

        #region Установка даты-времени
        public void SetRtc(DateTime dt)
        {            
            commands.Enqueue(new Command((p1, p2) =>
            {
                if (model.Rtc.RegNum >= holdRegs.Length - 6) return;
                int i = model.Rtc.RegNum;
                holdRegs[i] = (ushort)(dt.Year % 100);
                holdRegs[i + 1] = (ushort)(dt.Month);
                holdRegs[i + 2] = (ushort)(dt.Day);
                holdRegs[i + 3] = (ushort)(dt.Hour);
                holdRegs[i + 4] = (ushort)(dt.Minute);
                holdRegs[i + 5] = (ushort)(dt.Second);
                WriteRegs(i, 6); 
            }, 0, 0));
        }
        #endregion

        #region Уставка HV
        public void SetHv(ushort value)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                client?.WriteSingleRegister(model.TelemetryHV.VoltageSV.RegNum, value*20);
                model.SettingsReaded = false;
            }, 0, 0));
        }
        #endregion

        #region Управление питанием аналоговых модулей
        public void SwitchAm(int groupNum, int moduleNum, bool value)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                client?.WriteSingleRegister((int)Holds.SwitchPwrAm + groupNum * 2 + moduleNum, value ? 1 : 0);
            }, 0, 0));
        }
        #endregion

        #region Отправить значение тестовой величины AM
        public void SetTestValueAm(int groupNum, int moduleNum, ushort value)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                client?.WriteSingleRegister((int)Holds.TestValueDac, value);
                client?.WriteSingleRegister((int)Holds.SendTestValue, groupNum * 2 + moduleNum + 1);
            }, 0, 0));
        }
        #endregion

        #region Отправить настройки аналоговых выходов
        public void SendAnalogOutSwttings(int groupNum, int moduleNum, AnalogOutput value)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                holdRegs[(int)Holds.DacSett + groupNum] = value.Activity.Value;// Активнсть
                holdRegs[(int)Holds.DacSett + groupNum + 2] = value.DacType.Value;// Тип
                holdRegs[(int)Holds.DacSett + groupNum + 4] = value.DacEiNdx.Value;// Номер ЕИ
                holdRegs[(int)Holds.DacSett + groupNum + 6] = value.DacVarNdx.Value;// Номер переменной
                GetUshortsFromFloat(holdRegs, (int)Holds.DacSett + groupNum * 2 + 8, value.DacLowLimit.Value); // нижний предел
                GetUshortsFromFloat(holdRegs, (int)Holds.DacSett + groupNum * 2 + 12, value.DacHighLimit.Value); // верхний предел
                for (int i = 0; i < 2; i++) WriteRegs((int)Holds.DacSett + i * 8, 8);
                model.SettingsReaded = false;

            }, 0, 0));          
            
        }
        #endregion

        #region Отправить настройки аналоговых входов
        public void SendAnalogInSwttings(int groupNum, int moduleNum, AnalogInput value)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                holdRegs[(int)Holds.AdcSett + groupNum] = value.Activity.Value;// Активнсть
                WriteRegs((int)Holds.AdcSett + groupNum, 1);

            }, 0, 0));
        }
        #endregion

        #region Записать настройки набора стандартизации
        public void WriteStdSettings(ushort index, StandData stand)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                client.WriteSingleRegister(StandData.NumSelection.RegNum, index);
                holdRegs[model.StandSettings[0].Duration.RegNum] = stand.Duration.Value;
                holdRegs[model.StandSettings[0].Type.RegNum] = stand.Type.Value;
                GetUshortsFromFloat(holdRegs, model.StandSettings[0].Value.RegNum, stand.Value.Value);
                for (int i = 0; i < 8; i++)
                {
                    GetUshortsFromFloat(holdRegs, model.StandSettings[0].Results[i].RegNum+i*2, stand.Results[i].Value);
                }
                // Записываем регисры
                for (int i = 0; i < 2; i++)
                {
                    WriteRegs(model.StandSettings[0].Duration.RegNum + i * 12, 12);
                }
                GetStandSetiings(index);
            }, 0, 0));
        }
        #endregion

        #region Команда "Произвести стандартизацию"
        /// <summary>
        /// Произвести стандартизацию
        /// </summary>
        /// <param name="index">Номер набора стандартизации</param>
        public void MakeStand(int index)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                client.WriteSingleRegister(StandData.NumSelection.RegNum, index);
                client.WriteSingleRegister(StandData.StandCommandRegNum, index);
            }, 0, 0));
            
        }
        #endregion

        #region Команда принудиельного запроса набора стандартизации после стандартизации
        public void GetStdSelection(ushort index)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                GetStandSetiings(index);
            }, 0, 0));
        }
        #endregion

        #region Команда "Записать настройки счечиков"
        public void WriteCounterSettings(CountDiapasone diapasone)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                client.WriteSingleRegister(diapasone.Num.RegNum,diapasone.Num.Value);
                holdRegs[diapasone.Start.RegNum] = diapasone.Start.Value;
                holdRegs[diapasone.Finish.RegNum] = diapasone.Finish.Value;
                WriteRegs(diapasone.Start.RegNum, 2);
                GetCounterSettings(diapasone.Num.Value);
            }, 0, 0));
        }
        #endregion

        #region Команда "Записать данные калибровочных кривых"
        public void SetCalibrData(CalibrData calibrData)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                if (calibrData.Num.Value<MainModel.CalibCurveNum)
                {
                    client.WriteSingleRegister(calibrData.Num.RegNum, calibrData.Num.Value);
                    holdRegs[calibrData.MeasUnitNum.RegNum] = calibrData.MeasUnitNum.Value;
                    for (int i = 0; i < 6; i++)
                    {
                        GetUshortsFromFloat(holdRegs, calibrData.Coeffs[i].RegNum, calibrData.Coeffs[i].Value);
                    }
                    WriteRegs(calibrData.MeasUnitNum.RegNum, 14);
                    GetCalibrData(calibrData.Num.Value);
                }
                
            }, 0, 0));
        }
        #endregion

        #region Команда "Поменять UDP адрес источника"
        public void SetUdpAddr(byte[] addr)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                uint ip = 0;
                for (int i = 0; i < 4; i++)
                {
                    ip += (uint)(addr[i] << (i * 8));
                }
                GetUshortsFromUInt32(holdRegs, (int)Holds.UdpAddr,ip);
                WriteRegs((int)Holds.UdpAddr, 2);
                GetUdpAddr();

            }, 0, 0));
        }

        #endregion

        #region Команда "Произвести еденичное измеренине"
        public void MakeSingleMeasure(ushort time)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                client.WriteSingleRegister((int)Holds.SingleMeasDuration, time*10);
                client.WriteSingleRegister((int)Holds.SingleMeasStart, 1);
            }, 0, 0));
        }
        #endregion

        #region Команды изменения настроек платы АЦП
        public void SetAdcBoardSettings(AdcBoardSettings settings)
        {
            SwitchAdcBoard(0);
            commands.Enqueue(new Command((p1, p2) =>
            {
                Thread.Sleep(200);
                holdRegs[model.AdcBoardSettings.AdcMode.RegNum] = settings.AdcMode.Value;
                holdRegs[model.AdcBoardSettings.AdcProcMode.RegNum] = settings.AdcProcMode.Value;
                holdRegs[model.AdcBoardSettings.AdcSyncLevel.RegNum] = settings.AdcSyncLevel.Value;
                holdRegs[model.AdcBoardSettings.AdcSyncMode.RegNum] = settings.AdcSyncMode.Value;
                holdRegs[model.AdcBoardSettings.TimerMax.RegNum] = settings.TimerMax.Value;
                holdRegs[model.AdcBoardSettings.PreampGain.RegNum] = settings.PreampGain.Value;
                WriteRegs(model.AdcBoardSettings.AdcMode.RegNum, 6);
                GetAdcBoardSettings();
                Thread.Sleep(200);
                SwitchAdcBoard(1);

            }, 0, 0));
            
        }
        #endregion

        #region Команда "Запуск-останов платы АЦП"
        public void SwitchAdcBoard(ushort value)
        {            
            commands.Enqueue(new Command((p1, p2) =>
            {
                
                client.WriteSingleRegister((int)Holds.StartAdc, value);
            }, 0, 0));
            
        }
        #endregion

        #region Команда "Запуск/останов выдачи данных АЦП "
        public void StartStopAdcData(ushort value)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                client.WriteSingleRegister((int)Holds.StartAdcData, value);
            }, 0, 0));
        }
        #endregion

        #region Команда "Записать настройки едениц измерерия"
        public void SetMeasUnitsSettings(MeasUnitSettings settings)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                client.WriteSingleRegister(settings.Id.RegNum, settings.Id.Value);
                GetUshortsFromFloat(holdRegs, settings.A.RegNum, settings.A.Value);
                GetUshortsFromFloat(holdRegs, settings.B.RegNum, settings.B.Value);
                WriteRegs(settings.A.RegNum, 4);
                GetMeasUnitSettings((ushort)settings.Id.Value);
            }, 0, 0));
        }

        #endregion

        #region Команда "Записать настройки входа расходомера"
        public void SetConsInputSettings(ConsInputSettings settings)
        {
            commands.Enqueue(new Command((p1, p2) =>
            {
                holdRegs[model.ConsInputSettings.InputNum.RegNum] = (ushort)settings.InputNum.Value;
                holdRegs[model.ConsInputSettings.Activity.RegNum] = (ushort)(settings.Activity.Value?1:0);
                GetUshortsFromFloat(holdRegs, settings.A.RegNum, settings.A.Value);
                GetUshortsFromFloat(holdRegs, settings.B.RegNum, settings.B.Value);
                WriteRegs(model.ConsInputSettings.InputNum.RegNum, 8);
                GetConsInputSettings();
            }, 0, 0));
        }
        #endregion
        #endregion
    }
}
