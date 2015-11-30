using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CRMSLASystem
{
    // CRM Business Closure
    public class CRMBusinessClosure
    {
        private IOrganizationService _service;
        private IWorkflowContext _context;

        public CRMBusinessClosure(IOrganizationService service, IWorkflowContext context)
        {
            _service = service;
            _context = context;
        }

        private IList<Entity> GetBusinessClosureCalendarRules()
        {
            // Retrieve Organisation Business Closure Calendar Id.
            Entity organisation = _service.Retrieve("organization", _context.OrganizationId, new ColumnSet("businessclosurecalendarid"));

            QueryExpression calendarQuery = new QueryExpression("calendar");
            calendarQuery.ColumnSet = new ColumnSet(true);
            calendarQuery.Criteria = new FilterExpression();
            calendarQuery.Criteria.AddCondition(new ConditionExpression("calendarid", ConditionOperator.Equal, organisation["businessclosurecalendarid"].ToString()));

            Entity businessClosureCalendar = _service.RetrieveMultiple(calendarQuery).Entities[0];
            if (businessClosureCalendar != null)
            {
                return businessClosureCalendar.GetAttributeValue<EntityCollection>("calendarrules").Entities.ToList<Entity>();
            }
            return null;
        }

        public IList<Entity> GetBusinessClosureCalendarRules(DateTime startDate, DateTime endDate)
        {
            IList<Entity> businessClosures = GetBusinessClosureCalendarRules();

            var validBusinessClosures = from a in businessClosures
                                        where ((DateTime)a["starttime"] >= startDate
                                        && (DateTime)a["starttime"] <= endDate)
                                        || ((((DateTime)a["starttime"]).AddMinutes((int)a["duration"])) >= startDate
                                        && (((DateTime)a["starttime"]).AddMinutes((int)a["duration"])) <= endDate)
                                        || ((((DateTime)a["starttime"]).AddMinutes((int)a["duration"])) > endDate
                                        && (DateTime)a["starttime"] < startDate)
                                        select a;

            IList<Entity> validClosures = new List<Entity>();

            foreach (var validBusinessClosure in validBusinessClosures)
            {
                validClosures.Add((Entity)validBusinessClosure);
            }

            return validClosures;
        }

        public static int[] GetTimeWorked(DateTime businessOpen, DateTime businessClose, DateTime closureStart, DateTime closureEnd)
        {
            if (businessOpen > closureEnd || businessClose < closureStart)
                return new int[] { 0, 0, 0 };

            if (businessOpen > closureStart && businessClose < closureEnd)
                return new int[] { 1, 0, 0 };

            DateTime currentTime = businessOpen > closureStart ? businessOpen : closureStart;
            DateTime currentEnd = businessClose < closureEnd ? businessClose : closureEnd;
            TimeSpan duration = currentEnd - currentTime;

            return new int[] { 0, duration.Hours, duration.Minutes };
        }
    }
}
