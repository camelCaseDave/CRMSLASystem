using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CRMSLASystem
{
    /// <summary>
    /// CRM Business Closure SLA System.
    /// Authors: Richard Anderson and David Clark.
    /// Version: 1.0.0.0
    /// Changelog:
    /// </summary>
    public class BusinessTime
    {
        private int _days;
        private int _hours;
        private int _minutes;
        private DateTime _startDate;
        private IOrganizationService _service;
        private IWorkflowContext _context;

        private WorkingDay[] _workingDays;

        public BusinessTime(DateTime startDate, IOrganizationService service, IWorkflowContext context)
        {
            _startDate = startDate;
            _service = service;
            _context = context;
        }

        public DateTime AddBusinessTime(int days, int hours, int minutes)
        {
            // 1 = calculate working time - weekends
            // 2 = calculate CRM Business Closure time during start -> end date.
            // 3 = if above > 0, re-run AddBusinessTime()
            //     else return endDate
            _minutes = minutes;
            _hours = hours;
            _days = days;

            // Will be added in by CRM workflow typically.
            _workingDays = WorkingDay.StaticWorkingDays(9.0, 17.0);

            WorkingDay currentDay = GetStartDate(_startDate);
            DateTime processTime = _startDate;

            IDictionary<string, object> validStartDateObjects = GetNextValidWorkingDate(processTime, currentDay);
            processTime = (DateTime)validStartDateObjects["datetime"];
            currentDay = (WorkingDay)validStartDateObjects["currentday"];

            while (_days > 0)
            {
                int[] workingTime = currentDay.GetWorkingTime();

                if (workingTime[0] == 0 && workingTime[1] == 0)
                {
                    currentDay = GetNextDay(currentDay);
                }
                else
                {
                    _minutes += workingTime[1];

                    int hoursCounter = workingTime[0];

                    while (hoursCounter > 0)
                    {
                        processTime = processTime.AddHours(1);

                        int leftoverMinutes = currentDay.RemainingMinutesInHour(processTime);

                        _minutes += leftoverMinutes;
                        hoursCounter--;

                        if (leftoverMinutes > 0)
                        {
                            validStartDateObjects = GetNextValidWorkingDate(processTime, currentDay);
                            processTime = (DateTime)validStartDateObjects["datetime"];
                            currentDay = (WorkingDay)validStartDateObjects["currentday"];
                        }
                    }

                    _days--;
                }
            }

            do
            {
                _hours += _minutes / 60;
                _minutes = _minutes % 60;

                while (_hours > 0)
                {
                    processTime = processTime.AddHours(1);

                    int leftoverMinutes = currentDay.RemainingMinutesInHour(processTime);

                    _minutes += leftoverMinutes;
                    _hours--;

                    if (leftoverMinutes > 0)
                    {
                        validStartDateObjects = GetNextValidWorkingDate(processTime, currentDay);
                        processTime = (DateTime)validStartDateObjects["datetime"];
                        currentDay = (WorkingDay)validStartDateObjects["currentday"];
                    }
                }
            }
            while (_minutes > 60);

            while (_minutes > 0)
            {
                processTime = processTime.AddHours(1);

                int remainingMinutes = currentDay.RemainingMinutesInHour(processTime);
                int workedMinutes = 60 - remainingMinutes;

                if (workedMinutes >= _minutes)
                {
                    processTime = processTime.AddMinutes((60 - _minutes) * -1);
                    _minutes = 0;
                }
                else
                {
                    validStartDateObjects = GetNextValidWorkingDate(processTime, currentDay);
                    processTime = (DateTime)validStartDateObjects["datetime"];
                    currentDay = (WorkingDay)validStartDateObjects["currentday"];

                    _minutes = _minutes - workedMinutes;
                }
            }

            // Get CRM Business Closures.
            CRMBusinessClosure closureModel = new CRMBusinessClosure(_service, _context);
            IList<Entity> businessClosures = closureModel.GetBusinessClosureCalendarRules(_startDate, processTime);

            int[] totalTime = new int[3] { 0, 0, 0 };

            foreach (Entity businessClosure in businessClosures)
            {
                int[] i = TimeWorkedInBusinessClosure(businessClosure, _startDate, processTime);
                totalTime[0] += i[0];
                totalTime[1] += i[1];
                totalTime[2] += i[2];
            }

            if (totalTime[0] == 0 && totalTime[1] == 0 && totalTime[2] == 0)
                return processTime;

            _startDate = processTime;

            return AddBusinessTime(totalTime[0], totalTime[1], totalTime[2]);

        }

        public IDictionary<string, object> GetNextValidWorkingDate(DateTime dateTime, WorkingDay currentDay)
        {
            DateTime? startDate = null;

            // Get the start date and ensure it's valid.
            while (startDate == null)
            {
                startDate = currentDay.GetValidStartDate(dateTime);

                if (startDate == null)
                {
                    dateTime = dateTime.AddDays(1).Date;
                    currentDay = GetNextDay(currentDay);
                }
            }

            IDictionary<string, object> nextValidDate = new Dictionary<string, object>();
            nextValidDate.Add("datetime", (DateTime)startDate);
            nextValidDate.Add("currentday", currentDay);

            return nextValidDate;
        }

        public WorkingDay GetNextDay(WorkingDay workingDay)
        {
            int currentDay = (int)workingDay.dayOfWeek;
            int nextDay = currentDay == 6 ? 0 : currentDay + 1;

            var nextWorkingDay = (from item in _workingDays
                                  where item.dayOfWeek == (DayOfWeek)nextDay
                                  select item).FirstOrDefault<WorkingDay>();

            return (WorkingDay)nextWorkingDay;
        }

        public WorkingDay GetStartDate(DateTime dateTime)
        {
            DayOfWeek dayOfWeek = dateTime.DayOfWeek;

            var workingDay = (from item in _workingDays
                              where item.dayOfWeek == dayOfWeek
                              select item).FirstOrDefault<WorkingDay>();

            return (WorkingDay)workingDay;

        }

        public int[] TimeWorkedInBusinessClosure(Entity businessClosure, DateTime startTime, DateTime endTime)
        {
            int[] timeWorked = new int[3];
            DateTime businessClosureStart = (DateTime)businessClosure["starttime"];
            DateTime businessClosureEnd = ((DateTime)businessClosure["starttime"]).AddMinutes((int)businessClosure["duration"]);
            WorkingDay currentDay = GetStartDate(businessClosureStart);

            if (businessClosureStart.Date == businessClosureEnd.Date)
            {
                DateTime businessStartDate = currentDay.GetStartDate(businessClosureStart);
                DateTime businessEndDate = currentDay.GetEndDate(businessClosureEnd);

                if (currentDay.GetValidStartDate(businessClosureStart) == null)
                    return new int[] { 0, 0, 0 };

                return CRMBusinessClosure.GetTimeWorked(businessStartDate, businessEndDate, businessClosureStart, businessClosureEnd);
            }

            else
            {
                DateTime counter = businessClosureStart;

                while (counter < businessClosureEnd)
                {
                    if (currentDay.GetValidStartDate(businessClosureStart) != null)
                    {
                        DateTime businessStartDate = currentDay.GetStartDate(businessClosureStart);
                        DateTime businessEndDate = currentDay.GetEndDate(businessClosureStart);

                        // We know that we're working this day, we need to find out how much we're working.
                        int[] i = CRMBusinessClosure.GetTimeWorked(businessStartDate, businessEndDate, businessClosureStart, businessClosureEnd);
                        timeWorked[0] += i[0];
                        timeWorked[1] += i[1];
                        timeWorked[2] += i[2];
                    }

                    // Add a day and setting to next working day.
                    counter = counter.Date.AddDays(1);
                    currentDay = GetStartDate(counter);
                }
            }

            return timeWorked;
        }
    }
}
