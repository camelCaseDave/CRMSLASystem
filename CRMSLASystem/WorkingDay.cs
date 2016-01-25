using System;

namespace CRMSLASystem
{
    /// <summary>
    /// A CRM Working Day i.e. is not a business closure and is not a weekend.
    /// Accounts for valid working time as defined by working hours and minutes e.g. 09:30am to 18:00pm.
    /// </summary>
    public class WorkingDay
    {
        private int _businessStartHour;
        private int _businessStartMinutes;
        private int _businessEndHour;
        private int _businessEndMinutes;

        private bool _isWorking;

        private DayOfWeek _dayOfWeek;
        public DayOfWeek dayOfWeek { get { return _dayOfWeek; } }

        /// <summary>
        /// Abstraction of a CRM working day i.e. not a weekend and not a business closure.
        /// </summary>
        /// <param name="dayOfWeek">The day of the week as a DayOfWeek.</param>
        /// <param name="businessStartHour">The time work commences for the WorkingDay as a double. 
        /// e.g. 09.0d = 09:00am.</param>
        /// <param name="businessEndHour">The time work finishes for the WorkingDay as a double.
        /// e.g. 17.0d = 17:00pm.</param>
        /// <param name="isWorking">Boolean defining if the day is a working day, true if it is, false if it is not.</param>
        public WorkingDay(DayOfWeek dayOfWeek, double businessStartHour, double businessEndHour, bool isWorking)
        {
            _businessStartHour = int.Parse(businessStartHour.ToString().Split('.')[0]);
            _businessStartMinutes = businessStartHour.ToString().Split('.').Length > 1 ? (int.Parse(businessStartHour.ToString().Split('.')[1]) * 60) / 100 : 0;

            _businessEndHour = int.Parse(businessEndHour.ToString().Split('.')[0]);
            _businessEndMinutes = businessEndHour.ToString().Split('.').Length > 1 ? (int.Parse(businessEndHour.ToString().Split('.')[1]) * 60) / 100 : 0;

            _dayOfWeek = dayOfWeek;

            _isWorking = isWorking;
        }

        /// <summary>
        /// Gets the remaining minutes in an hour accounting for business closures.
        /// </summary>
        /// <param name="dateTime">The DateTime to assess.</param>
        /// <returns>Returns the number of minutes remaining in the current hour of a DateTime as an integer.</returns>
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

        /// <summary>
        /// Gets a valid start date and time for a working day. A valid working date and time falls 
        /// within working hours and must be on a working day (_isWorking = true).
        /// </summary>
        /// <param name="dateTime">The DateTime to assess and set to a valid start date if not already valid.</param>
        /// <returns>Returns the next valid working start date as a nullable DateTime.</returns>
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

        /// <summary>
        /// Gets the amount of hours and minutes required to be work for a given WorkingDay.
        /// On weekends this is { 0, 0 } else e.g. for a day where start hour is 09:00am and finish
        /// hour is 17:30pm, returns 8 hours and 30 minutes as { 8, 30 }.
        /// </summary>
        /// <returns>Returns an int[2], where int[0] is hours to work in a WorkingDay and int[1] is minutes
        /// to work in a WorkingDay.</returns>
        public int[] GetWorkingTime()
        {
            int[] array = new int[2];

            // If this isn't a working day, return 0 hours and 0 minutes.
            if (!_isWorking)
                return new int[] { 0, 0 };

            double start = _businessStartHour + (_businessStartMinutes / 60);
            double end = _businessEndHour + (_businessEndMinutes / 60);
            double timeWorking = end - start;

            array[0] = int.Parse(timeWorking.ToString().Split('.')[0]);
            array[1] = timeWorking.ToString().Split('.').Length > 1 ? (int.Parse(timeWorking.ToString().Split('.')[1]) * 60) / 100 : 0;

            return array;
        }

        /// <summary>
        /// A static array of WorkingDays where Saturday and Sunday are not considered working days.
        /// </summary>
        /// <param name="startHour">The hour work commences for a given WorkingDay as a double.</param>
        /// <param name="endHour">The hour work finishes for a given WorkingDay as a double.</param>
        /// <returns>Returns a static array of WorkingDays.</returns>
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

        /// <summary>
        /// Gets the start date and time for a given DateTime by considering business open hours.
        /// </summary>
        /// <param name="dateTime">The DateTime to assess.</param>
        /// <returns>Returns the start date and time having applied business opening hours as a DateTime.</returns>
        public DateTime GetStartDate(DateTime dateTime)
        {
            return dateTime.Date.AddHours(_businessStartHour).AddMinutes(_businessStartMinutes);
        }

        /// <summary>
        /// Gets the end date and time for a given DateTime by considering the time of day business ends.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns>Returns the end date and time having applied business close hours as a DateTime.</returns>
        public DateTime GetEndDate(DateTime dateTime)
        {
            return dateTime.Date.AddHours(_businessEndHour).AddMinutes(_businessEndMinutes);
        }
    }
}
