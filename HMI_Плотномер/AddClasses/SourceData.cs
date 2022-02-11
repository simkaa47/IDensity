using IDensity.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace IDensity.AddClasses
{
    class SourceData :PropertyChangedBase
    {
        #region Тип радионуклида
        private int _sourceType;

        public int SourceType
        {
            get { return _sourceType; }
            set { Set(ref _sourceType, value); }
        }
        #endregion

        #region Дата изготовления
        private DateTime _creationTime;

        public DateTime CreationTime
        {
            get { return _creationTime; }
            set { Set(ref _creationTime, value); DateProlong = value; }
        }
        #endregion

        #region Активность источника
        private int _activity;

        public int Activity
        {
            get { return _activity; }
            set { Set(ref _activity, value); }
        }

        #endregion

        #region Единица измерения активности источника
        private int _actMeasNum;

        public int ActMeasNum
        {
            get { return _actMeasNum; }
            set { Set(ref _actMeasNum, value); }
        }

        #endregion

        #region Дата продления службы
        private DateTime _dateProlong;

        public DateTime DateProlong
        {
            get { return _dateProlong; }
            set { Set(ref _dateProlong, value); }
        }

        #endregion

        #region Срок эксплуатации прибора
        private int _sourcePeriod;

        public int SourcePeriod
        {
            get { return _sourcePeriod; }
            set { Set(ref _sourcePeriod, value); }
        }

        #endregion

        #region Дата окончания источника
        private DateTime _finishTime;

        public DateTime FinishTime
        {
            get { return _finishTime; }
            set { Set(ref _finishTime, value); }
        }
        #endregion

        public SourceData()
        {
            this.PropertyChanged += (s, e) => 
            {
                FinishTime = DateProlong.AddYears(3);
            };
        }

    }
}
