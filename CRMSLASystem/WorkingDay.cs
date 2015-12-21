using System;

namespace CRMSLASystem
{
    // CRM Working Day
    public class WorkingDay
    {
        private int _businessStartHour;
        private int _businessStartMinutes;
        private int _businessEndHour;
        private int _businessEndMinutes;

        private DayOfWeek _dayOfWeek;
        public DayOfWeek dayOfWeek
        {
            get
            {
                return _dayOfWeek;
            }
        }

        private bool _isWorking;

        public DateTime GetStartDate(DateTime dateTime)
        {
            return dateTime.Date.AddHours(_businessStartHour).AddMinutes(_businessStartMinutes);
        }

        public DateTime GetEndDate(DateTime dateTime)
        {
            return dateTime.Date.AddHours(_businessEndHour).AddMinutes(_businessEndMinutes);
        }

        public WorkingDay(DayOfWeek dayOfWeek, double businessStartHour, double businessEndHour, bool isWorking)
        {
            _businessStartHour = int.Parse(businessStartHour.ToString().Split('.')[0]);
            _businessStartMinutes = businessStartHour.ToString().Split('.').Length > 1 ? (int.Parse(businessStartHour.ToString().Split('.')[1]) * 60) / 100 : 0;

            _businessEndHour = int.Parse(businessEndHour.ToString().Split('.')[0]);
            _businessEndMinutes = businessEndHour.ToString().Split('.').Length > 1 ? (int.Parse(businessEndHour.ToString().Split('.')[1]) * 60) / 100 : 0;

            _dayOfWeek = dayOfWeek;

            _isWorking = isWorking;
        }

        public int RemainingMinutesInHour(DateTime dateTime)
        {
            DateTime startDate = dateTime.AddHours(-1);

            int endHour, endMinute;

            endHour = dateTime.Hour;
            endMinute = dateTime.Minute;

            if (dateTime > dateTime.Date.AddHours(_businessEndHour).AddMinutes(_businessEndMinutes))
            {
                return (int)dateTime.Subtract(
                    dateTime.Date.AddHours(_businessEndHour).AddMinutes(_businessEndMinutes)
                ).TotalMinutes;
            }

            return 0;
        }

        public DateTime? GetValidStartDate(DateTime dateTime)
        {
            if (!_isWorking)
                return null;

            DateTime businessStart = dateTime.Date.AddHours(_businessStartHour).AddMinutes(_businessStartMinutes);
            DateTime businessEnd = dateTime.Date.AddHours(_businessEndHour).AddMinutes(_businessEndMinutes);

            if (dateTime >= businessEnd)
                return null;

            if (dateTime <= businessStart)
                return businessStart;

            return dateTime;
        }

        public int[] GetWorkingTime()
        {
            int[] array = new int[2];

            if (!_isWorking)
                return new int[] { 0, 0 };

            double start = _businessStartHour + (_businessStartMinutes / 60);
            double end = _businessEndHour + (_businessEndMinutes / 60);
            double timeWorking = end - start;

            array[0] = int.Parse(timeWorking.ToString().Split('.')[0]);
            array[1] = timeWorking.ToString().Split('.').Length > 1 ? (int.Parse(timeWorking.ToString().Split('.')[1]) * 60) / 100 : 0;

            return array;
        }

        public static WorkingDay[] StaticWorkingDays(double startHour, double endHour)
        {
            WorkingDay[] workingDays = new WorkingDay[7];

            for (int i = 0; i < 7; i++)
            {
                // DayOfWeek Enum, 0 = Sunday, 6 = Saturday.
                WorkingDay temp = new WorkingDay(
                    (DayOfWeek)i,
                    startHour,
                    endHour,
                    (i == 0 || i == 6) ? false : true
                );

                workingDays[i] = temp;
            }

            return workingDays;
        }
    }
}
